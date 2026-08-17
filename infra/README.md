# infra — Terraform (Azure)

Azure infrastructure for EventPlatform, per [ADR-0005](../docs/adr/0005-iac-terraform.md).

## Layout

```
infra/
  bootstrap/           # one-time, local-state: creates the remote-state storage account
  environments/dev/    # minimal-cost dev environment (see ADR-0017)
  modules/             # leaf modules (resource-group, networking, container-registry,
                        # aks, postgres, redis, key-vault) — reused across environments
```

Currently only `dev` exists. A `staging`/`production` environment would be a
new `infra/environments/*` config that follows the full topology in
[ADR-0002](../docs/adr/0002-runtime-aks.md) and
[ADR-0005](../docs/adr/0005-iac-terraform.md) (dedicated hot-path node
pools, per-service Postgres servers, Service Bus) — `dev` deliberately does
not, see [ADR-0017](../docs/adr/0017-dev-environment-cost-topology.md).

## Apply flow

1. `infra/bootstrap` — apply once, ever (creates remote state storage).
2. `infra/environments/dev` — init with the bootstrap outputs as
   `-backend-config`, then `plan`/`apply`.

Full steps are in each directory's own README:
[infra/bootstrap/README.md](bootstrap/README.md),
[infra/environments/dev/README.md](environments/dev/README.md).

## Cost (dev environment, pay-as-you-go, rough — verify with the Azure Pricing Calculator before applying)

| Item | Est. monthly |
|---|---|
| AKS control plane (Free tier) | $0 |
| Node pool (1× `Standard_D2s_v7`, default; `node_count`/`node_vm_size` are tfvars) | **verify — see note below** |
| Postgres Flexible B1ms + storage | ~$15-20 (possibly $0 under a new subscription's free allowance) |
| Redis Basic C0 | ~$16 |
| ACR Basic | ~$5 |
| AKS default Standard Load Balancer + egress IP (provisioned even with zero `LoadBalancer` Services) | ~$15-20 |
| Key Vault + tfstate Storage Account | <$2 |
| ingress-nginx + cert-manager | $0 — both run on nodes already paid for, and the controller's load balancer replaces the gateway's rather than adding one. Let's Encrypt certificates are free. |
| Log Analytics (Container Insights) | $0 at the default 0.15 GB/day cap — that keeps a month inside Azure Monitor's 5 GB free grant. Raise `log_analytics_daily_quota_gb` and you start paying ~$2.30/GB. |
| **Total (1 node, default)** | **~$55-65/mo + node pool (verify)** |

**Note on node pool cost:** `Standard_B2ms` (burstable, ~$60/mo) was the
original recommendation, but some subscriptions' allowed-SKU lists don't
include B-series at all — `terraform apply` fails with a 400 naming the
allowed sizes for yours. `Standard_D2s_v7` (this repo's current default) is
the smallest general-purpose size on such lists, but general-purpose pricing
is typically higher than burstable for the same vCPU/RAM — check the Azure
Pricing Calculator for your subscription's actual rate before relying on the
total below. If B-series becomes available on your subscription, switch back
via the `node_vm_size` tfvar.

Two nodes (headroom for Dapr sidecars across 6 services/gateway) roughly
adds the node-pool line again (~$175-185/mo total). `az aks stop`
deallocates node VMs (preserves cluster config) for nights/idle time with no
Terraform change.

## What observability you actually get

Two things, landing in one Log Analytics workspace:

- **Container Insights** — container stdout/stderr and node/pod metrics,
  queryable with KQL. This is what makes a deployed problem diagnosable at all,
  instead of racing `kubectl logs` against a pod that may already have been
  replaced.
- **Distributed traces and app metrics**, through an OpenTelemetry Collector
  (`deploy/base/observability/`) into workspace-based Application Insights.
  The services and their Dapr sidecars have always emitted OTLP; before
  ADR-0031 nothing in the cluster listened, so it went nowhere. Traces now
  follow a request across service and pub/sub boundaries — which is the only
  practical way to see what the checkout saga did.

Both share **one daily ingestion cap**, and that is a real trade rather than
an oversight: a chatty trace load can starve container logging for the rest of
the UTC day, and the reverse. Two caps would have doubled the ceiling on a
surprise bill, which is the risk worth managing on a personal subscription.

The cap is a hard stop, not a warning: past it, ingestion stops for the rest of
the UTC day and that data is gone. Nothing here samples, because dev traffic is
usually one person clicking through one checkout and a sampler would discard the
trace you were trying to read. **Before any load test**, either raise
`log_analytics_daily_quota_gb` or add a `probabilistic_sampler` to the
collector's pipeline — a k6 run at full fidelity will spend a day's budget in
minutes.

## Provisioning

```bash
./scripts/provision-azure.sh          # or: ./scripts/provision-azure.sh dev
```

Logs you in, lists your subscriptions and makes you pick one, bootstraps the
remote-state storage if it does not exist, writes a `terraform.tfvars` (asking
for the Let's Encrypt address, generating the Postgres password), runs `plan`,
and applies **only** after you read it and type `y`. Then it offers to run
`finish-dev-bootstrap.sh` for the GitOps wiring.

Re-runnable: existing state, tfvars and init are detected and reused.

The subscription prompt is not a convenience. Terraform's provider will
otherwise use whatever `az account show` returns, and on a machine signed into
more than one tenant that silently creates the whole environment — and every
environment's state — in the wrong subscription, with nothing in the output to
say so.

The step-by-step equivalent, if you would rather drive it yourself, is in
[`environments/dev/README.md`](environments/dev/README.md).

## GitHub OIDC needs directory permissions, and often you won't have them

Everything in this environment is an **Azure Resource Manager** resource,
authorized by your role on the subscription — except `github-oidc.tf`. That
file creates an app registration, a service principal and federated credentials
in **Microsoft Entra ID**. Those are *directory* objects, governed by an
entirely separate permission system. **Subscription Owner grants you none of
it.** You will hit this if the tenant has "Users can register applications" set
to No, or if you are a guest in the directory rather than a member — both are
normal for a subscription you were given rather than one you created.

The failure looks like:

```
Error: Could not create service principal
  unexpected status 403 ... Authorization_RequestDenied
Error: Adding federated identity credential for Application
  unexpected status 403 ... Insufficient privileges to complete the operation
```

Check which case you are in:

```bash
az account show --query '{signedIn:user.name, subTenant:tenantId, homeTenant:homeTenantId}'
az rest --method GET --url 'https://graph.microsoft.com/v1.0/me' \
  --query '{upn:userPrincipalName, type:userType}'
az rest --method GET \
  --url 'https://graph.microsoft.com/v1.0/policies/authorizationPolicy' \
  --query 'defaultUserRolePermissions.allowedToCreateApps'
```

- `subTenant` ≠ `homeTenant` → set `entra_tenant_id = "<subTenant>"`. The
  `azuread` provider follows your *home* tenant by default, so it was creating
  the application in one directory and attempting its service principal in
  another — which is what the confusingly-worded "the backing application ...
  must be in the local tenant" error actually means.
- `type: Guest`, or `allowedToCreateApps: false` → you genuinely lack the
  permission. Either ask a Global Administrator / Application Administrator to
  grant **Application Developer** on your account, or skip it (below).

### Skipping it

```hcl
enable_github_oidc = false
```

The cluster, databases, Key Vault, ACR, ingress and Argo CD all apply exactly
as before. The *only* thing you lose is the identity GitHub Actions logs in
with to push images, so CD can't push until you replace it. Two ways:

1. **Have an admin create the app registration once**, by hand or with their
   own credentials, then add the federated credential for
   `repo:<owner>/<repo>:ref:refs/heads/<branch>` and assign it `AcrPush` on the
   ACR. Set `AZURE_CLIENT_ID` to it. This is the same end state as the
   Terraform, just created by someone who has the rights.
2. **Use ACR's admin credentials** — enable the admin user on the registry and
   give CD `ACR_USERNAME`/`ACR_PASSWORD` instead of an OIDC login. This is a
   long-lived shared password rather than a short-lived per-run token, and it
   is a real downgrade in posture: prefer option 1, and treat this as the
   "unblock me today" path, not the destination.

Until one of those is in place, use **`./scripts/push-images.sh`** — the local
equivalent of CD's build-and-push plus manifest bump. It reads the ACR name from
`terraform output`, builds every image (or just the ones you name), pushes them
tagged with your `HEAD`, rewrites `deploy/overlays/dev/kustomization.yaml` to
point at them, and commits. Argo CD reconciles whatever tag `deploy/` points at,
regardless of who pushed it.

It is a bootstrap and debug path, not a replacement for CD: it tags with your
*working tree's* HEAD, so an image can land in ACR that no commit on the remote
ever produced. The script warns on a dirty tree and on unpushed commits for
exactly that reason — Argo CD syncs the remote, never your working copy.

### Turning it back on later — import, don't recreate

If you get directory permissions afterwards and an app registration from a
failed earlier apply is still sitting in the directory, **do not just flip
`enable_github_oidc` back to true**. Entra ID does not enforce unique display
names, so Terraform will happily create a *second*
`github-actions-eventplatform-dev` and you will have two apps with the same
name and no way to tell from the Portal which one CI actually uses. Adopt the
existing one instead:

```bash
# Object ID (not client/app ID) — that is what the import address wants.
az ad app list --display-name github-actions-eventplatform-dev \
  --query '[].{objectId:id, clientId:appId}' -o table

terraform import 'azuread_application.github_actions[0]' /applications/<objectId>
terraform plan   # should show the service principal + federated credentials
                 # to add, and NO new application to create
```

If the old app is genuinely gone (an admin deleted it, or you never got one),
skip the import — flipping the flag is enough.

### If you already got a partial apply

The application may exist while its service principal does not. Flipping
`enable_github_oidc` to false will plan to destroy it, which needs the same
directory permission you just found you lack. If that destroy fails, drop it
from state instead and delete it in the Portal later (or leave it — an app
registration with no service principal is inert and free):

```bash
terraform state rm azuread_application.github_actions
```

## How you reach the cluster

`infra/environments/dev/ingress.tf` installs an NGINX ingress controller and
cert-manager, and the gateway is served over HTTPS on a hostname Azure gives
you for free: `<label>.<region>.cloudapp.azure.com`, from a DNS label on the
controller's public IP. `cloudapp.azure.com` is on the Public Suffix List, so
Let's Encrypt issues a normal browser-trusted certificate for it — no domain
purchase, no DNS provider, nothing to renew by hand.

`terraform output gateway_hostname` is the URL; `scripts/finish-dev-bootstrap.sh`
writes it into `deploy/overlays/dev/ingress.yaml` for you.

Set `custom_domain` if you own one. Terraform cannot create the DNS record —
point a CNAME at `terraform output azure_ingress_fqdn` **before** Argo CD
syncs, or cert-manager's HTTP-01 challenge fails until it resolves.

On the first sync the certificate takes a minute or two to issue, and until it
does the host serves the controller's self-signed default and the browser
warns. That is issuance in progress, not a misconfiguration:

```bash
kubectl describe certificate -n eventplatform-dev gateway-tls
```

Note what this does **not** include: the SPA is not deployed to the cluster
(`deploy/base/kustomization.yaml` has no frontend entry). The HTTPS endpoint
serves the API. The dev overlay allows `http://localhost:5173` as a browser
origin so a local `npm run dev` can call it.
