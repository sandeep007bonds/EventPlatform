# CLAUDE.md — infra (Terraform)

Inherits the [root CLAUDE.md](../CLAUDE.md).

## Responsibility

Azure infrastructure as code, per [ADR-0005](../docs/adr/0005-iac-terraform.md).
Provisions the underlying cloud resources (AKS, Postgres, Redis, ACR, Key
Vault, networking, remote-state storage, GitHub OIDC identity) **and**
installs Argo CD onto the cluster it creates (`environments/dev/argocd.tf`,
via the `helm`/`kubectl` providers — the one platform workload this layer
installs, since Argo CD can't deploy its own install). It does not deploy
the *application* onto the cluster — that's `deploy/` + Argo CD (GitOps).

## Owns

- `bootstrap/` — one-time, local-state config creating the remote-state
  storage account. Not an environment; apply once, never destroy.
- `environments/dev/` — the only environment that exists today. A
  minimal-cost topology that deliberately diverges from the production ADRs
  — see [ADR-0017](../docs/adr/0017-dev-environment-cost-topology.md) for
  every divergence and why. A future `staging`/`production` environment
  would follow the full ADR-0002/0005 topology instead.
- `modules/` — leaf modules (`resource-group`, `networking`,
  `container-registry`, `aks`, `postgres`, `redis`, `key-vault`,
  `blob-storage`), reused across environments. Leaf modules never contain
  `provider`/`backend`/`required_version` blocks — those are root-module-only, set in each
  `environments/*` config.

## Design notes

- **Root modules own the provider/backend/version blocks; leaf modules
  don't.** Only `bootstrap/` and `environments/dev/` declare `provider`,
  `backend`, and `required_providers`/`required_version`.
- **Cross-module role assignments live at the root**, not inside the leaf
  modules they connect — e.g. AKS-to-ACR `AcrPull` and the applying
  principal's Key Vault Secrets Officer role are both wired in
  `environments/dev/main.tf`. Putting a role assignment inside `modules/aks`
  or `modules/key-vault` would create a circular module dependency.
- **AKS pulls images via the kubelet identity**
  (`kubelet_identity[0].object_id`), never the cluster's own `identity`
  block principal — assigning `AcrPull` to the wrong one is a common,
  silent cause of `ImagePullBackOff`.
- **Key Vault is RBAC-mode** (`enable_rbac_authorization = true`), not
  legacy access policies. Subscription Contributor is not sufficient for
  vault data-plane operations under RBAC — an explicit role assignment is
  required even for the applying principal.
- **Postgres databases have `prevent_destroy = true`.** Removing a name
  from an environment's `db_names` local does not silently drop that
  database — the lifecycle block must be deliberately removed first.
- **Redis `maxmemory_policy` must stay `noeviction`** wherever an instance
  backs a Dapr Workflow actor state store (the checkout saga) — the
  default eviction policy silently drops in-flight saga state under memory
  pressure instead of failing loudly.

## Do not

- Never commit a real `terraform.tfvars` — only `*.tfvars.example`.
- Never hand-edit Terraform state.
- Never run `terraform destroy` against `bootstrap/` once any environment
  has state stored in its storage account.
- Always review `terraform plan` output before `apply` — infrastructure
  changes here are real, billable, and not always reversible.
- Never `kubectl apply`/`helm install` **by hand** onto a cluster this
  creates — that's `deploy/` + Argo CD's job, per the root CLAUDE.md's
  GitOps rule. The `helm_release`/`kubectl_manifest` resources in
  `environments/dev/{argocd,dapr,ingress}.tf` are the sanctioned exception:
  still Terraform-tracked IaC (reviewed in `terraform plan`, applied by
  `terraform apply`), not ad-hoc commands — and limited to cluster-wide
  platform components that have to exist before `deploy/` can work, never
  anything under `deploy/` itself.

## Cluster bootstrap lives in Terraform, deliberately

`argocd.tf`, `dapr.tf` and `ingress.tf` install Argo CD, the Dapr control
plane, and the ingress controller + cert-manager onto the cluster the same
apply creates. None can be installed by Argo CD: one *is* Argo CD, one is the
control plane whose sidecar injector has to be running before any annotated
pod starts, and the last is the entry point traffic arrives through. All are
tracked IaC rather than ad-hoc commands, which is what the root CLAUDE.md's
"no kubectl/helm by hand" rule is actually about.

Ingress is where TLS lives (ADR-0030): the gateway's Service is deliberately
`ClusterIP`, not `LoadBalancer` — making it a `LoadBalancer` again would
create a second public IP that bypasses the ingress and its certificate
entirely. The hostname is Azure's free `<label>.<region>.cloudapp.azure.com`,
which Let's Encrypt will issue for because `cloudapp.azure.com` is on the
Public Suffix List. `externalTrafficPolicy: Local` on the controller is load
bearing, not cosmetic: Queue's join rate limiter buckets by client address,
and the default policy SNATs every caller to one node address.

Dapr is the one whose absence is silent — the services start, report healthy,
and simply never exchange an event. If pub/sub, service invocation or the
checkout saga are dead in a deployed environment, check that `helm_release.dapr`
actually applied before looking anywhere else.
