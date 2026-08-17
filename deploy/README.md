# deploy

GitOps manifests, reconciled onto the AKS cluster by Argo CD — never applied
by hand (see the root [CLAUDE.md](../CLAUDE.md)'s GitOps rule).

## Layout

- `base/<service>/` — Deployment + Service + kustomization per service
  (catalog, inventory, ordering, payments, ticketing, communication, media,
  identity, queue, gateway, frontend). Every service that owns a database also has a
  `migrate-job.yaml` (see below).
- `base/dapr-components/` and `base/observability/` — platform-wide rather
  than per-service: the Dapr components every sidecar resolves by name, and
  the OpenTelemetry Collector everything exports telemetry to.
- `overlays/dev/` — the only overlay today. Namespaces everything under
  `eventplatform-dev`, generates the shared non-secret config, sets
  per-service image placeholders (overwritten by CI on each push — see
  `.github/workflows/`), and wires Key Vault secrets in via
  `keyvault-secretproviderclass.yaml`.

## Dapr components

`base/dapr-components/` holds the `pubsub` and `statestore` components the
services resolve by name. They carry the **same names** as the local-dev files
in `platform/dapr/components/`, which is the point: service code never names a
broker or a state store, so nothing in the application differs between a laptop
and AKS — only the backing config does.

Both are Redis-backed here, against the Azure Cache for Redis instance
Terraform creates. Service Bus remains the production target for pub/sub; dev
diverges deliberately and that divergence is recorded in ADR-0017.

`statestore` sets `actorStateStore: "true"` because Dapr Workflow is built on
actors and Ordering's checkout saga is a Dapr Workflow. Drop it and checkout
breaks — not at deploy time, but at the first checkout.

The **control plane that makes any of this work is installed by Terraform**
(`infra/environments/dev/dapr.tf`), not by Argo CD. Without it nothing injects
a sidecar, every `dapr.io/*` annotation is inert, and the services come up
healthy while doing nothing. Terraform orders the Argo CD Application after the
Dapr install so these Component manifests are never applied to a cluster whose
API server does not yet know the kind.

Credentials come from `eventplatform-secrets` via `secretKeyRef` — note that
Dapr needs `redis-host` and `redis-password` as separate values, while
Inventory and Queue use `redis-connection-string` for their own direct
StackExchange.Redis connections. One credential, two representations, because
two different clients read it.

## Observability

`base/observability/` runs an OpenTelemetry Collector. Every backend service
reaches it through `OTEL_EXPORTER_OTLP_ENDPOINT` in the shared config map
(the gateway sets the same value directly, since it deliberately doesn't use
`envFrom`), and every Dapr sidecar through the `tracing` Dapr `Configuration`
in the same directory, referenced by each pod's `dapr.io/config` annotation.

The services have always exported OTLP; until ADR-0031 nothing in the cluster
listened, so traces went nowhere. Dropping the sidecar half would still give
you connected traces — Dapr propagates `traceparent` regardless — but every
pub/sub delivery would be an unexplained gap in the timeline.

The pipeline config is a `configMapGenerator`, not a hand-written ConfigMap, so
editing it changes the generated name and actually rolls the collector. A plain
ConfigMap would update in place and leave the old config running.

`base/observability/` is listed before the services in
`base/kustomization.yaml` for the same reason `dapr-components` is: things
resolve it by name at startup.

## Ingress and TLS

`overlays/dev/ingress.yaml` is the cluster's single HTTPS entry point. It
deliberately knows nothing about individual backend services — a per-service
path here would let a request reach a backend without passing the gateway's
own route allowlist, which is what keeps saga-internal routes (Inventory's
hold convert/release, every Payments endpoint) off the public internet.

The controller and cert-manager are installed by Terraform
(`infra/environments/dev/ingress.tf`), not Argo CD — same reasoning as the Dapr
control plane above. `REPLACE_ME_GATEWAY_HOST` is filled from `terraform output
gateway_hostname` by `scripts/finish-dev-bootstrap.sh`, the same way this
overlay's Key Vault values are; the hostname belongs to an environment, not
to the gateway, which is why the Ingress lives here rather than in
`base/gateway/`.

`base/gateway/service.yaml` is `ClusterIP` on purpose. Turning it back into a
`LoadBalancer` gives you a second public IP serving plain HTTP with no
certificate, bypassing everything above (ADR-0030).

The ingress splits on path: `/api` to the gateway (longer prefix wins),
everything else to the SPA, on one hostname. That is what makes the frontend's
API calls same-origin — no CORS, and one frontend image for every environment
rather than one per hostname (ADR-0033). Note the gateway's own `/health/*`
and `/scalar/v1` are consequently not reachable from outside.

`gateway-cors-patch.yaml` allows a browser origin. The deployed gateway runs
as `Staging`, which skips the only appsettings file that populates
`Cors:AllowedOrigins`, so without it every *cross-origin* browser request
fails preflight. The deployed SPA does not need it (same origin); the real
caller is a local `npm run dev` pointed at the cluster. Removing the patch
would lock the deployed API to the deployed SPA — stricter, and probably where
this ends up.

## Authentication

`AddEventPlatformAuthentication` picks its branch on whether `Jwt:DevSigningKey`
is set. Locally it is (in `appsettings.Development.json`) and everything
validates HS256 against a shared secret. **In this cluster it deliberately is
not**: the overlay's config map sets `Jwt__Authority=http://identity` and
`Jwt__RequireHttpsMetadata=false`, so every service does OIDC discovery against
Identity and validates real RS256 signatures (ADR-0032).

`Identity__Jwt__Issuer` on the Identity Deployment must stay byte-identical to
that Authority — validation compares the token's `iss` to the issuer in the
discovery document, and a mismatch rejects every token with an error naming
neither URL.

Consequences worth knowing: **the gateway's dev-login endpoint is not mapped
here** (it is gated on the dev key), so a script hitting a deployed environment
needs a real Identity token. `jwt-dev-signing-key` is still in Key Vault and
still synced, deliberately unused — it is the escape hatch, so re-enabling the
dev path is adding an env var rather than a Terraform round trip.

## Schema migrations

Services never migrate themselves. Each database-owning service ships a
`base/<service>/migrate-job.yaml` — the *same image* as its Deployment, run
with `args: ["--migrate"]`, annotated `argocd.argoproj.io/hook: Sync` at
`sync-wave: "-5"` so Argo CD applies the schema and waits for the job to
succeed before rolling a single new pod. A failed migration fails the sync with
inspectable logs instead of crash-looping a replica (ADR-0029).

**Not `PreSync`, and this is load-bearing.** PreSync runs before *every* normal
resource in the Application — including `overlays/dev/keyvault-secretproviderclass.yaml`,
which these jobs mount to get their connection string. That deadlocks on a
fresh namespace: the hook waits forever for a volume whose SecretProviderClass
does not exist yet, and the main sync that would create it never starts because
the hook never finishes. The symptom is every migrate pod sitting in
`ContainerCreating` with `FailedMount ... SecretProviderClass "eventplatform-keyvault"
not found`, and an Application stuck `OutOfSync`/`Missing`. Staying in the Sync
phase and ordering with waves gives the same guarantee without the cycle:
SecretProviderClass at `-10`, migrate jobs at `-5`, everything else at the
default `0`.

Two things about these jobs that look like omissions but are not:

- **No `dapr.io/*` annotations.** A Dapr sidecar never exits on its own, so
  the pod would stay `Running` and the Job would never complete. Migrations
  need nothing from Dapr.
- **The image name matches the Deployment's placeholder exactly.** That is
  what makes CI's `kustomize edit set image` rewrite both to the same tag, so
  the schema is never applied by a different build than the one about to
  serve traffic.

`media`, `gateway` and `frontend` have no job — none owns a database.

## Secrets

Every service reads its Postgres connection string (and Redis, Stripe, and the
rest) from a Kubernetes `Secret` named `eventplatform-secrets`, populated at
runtime by the [Secrets Store CSI Driver's Azure Key Vault
provider](https://learn.microsoft.com/azure/aks/csi-secrets-store-driver) —
not committed anywhere, not templated by Kustomize. The actual secret
*values* live in Key Vault, created by `infra/environments/dev`.

`overlays/dev/keyvault-secretproviderclass.yaml` has three placeholders that
must be filled in once per environment, after `terraform apply`. Run
[`scripts/finish-dev-bootstrap.sh`](../scripts/finish-dev-bootstrap.sh)
rather than copying values by hand — it reads them from `terraform output`,
fills in and commits this file, and offers to set the matching GitHub
Actions secrets too. These values change only if the AKS cluster or Key
Vault is destroyed and recreated — not on every deploy.

This is a script, not a Terraform resource, on purpose: this file lives in
`deploy/`, which is GitOps-owned so a future service's secrets can be added
here without ever touching Terraform (see `docs/onboarding-new-service.md`).
Having Terraform write into `deploy/` would blur that boundary — only its
three bootstrap values (identity/vault/tenant) come from `terraform output`;
everything else in this file is maintained by hand as services are added.

**Why every Deployment mounts a CSI volume it never reads directly:** the
driver only materializes the synced `eventplatform-secrets` Kubernetes
`Secret` when some pod actually mounts the `SecretProviderClass` as a volume
(`volumes: secrets-store` in every `base/*/deployment.yaml`). Every service
reads the resulting `Secret` the normal way, via `secretKeyRef` — the mount
itself is otherwise unused, but removing it stops the sync.

## What CI checks

`kustomize build deploy/overlays/dev` runs in CI's `infrastructure` job, which
exercises the base, every patch, and every generator — a patch that targets
nothing, or a resource missing from a `kustomization.yaml`, fails there rather
than at Argo CD sync time. It does not talk to a cluster, so it says the
manifests are well-formed, not that they do what you meant.

One gap worth knowing: the workflow excludes `deploy/**` from push triggers to
break the CD tag-bump loop (ADR-0004), so a push touching only this directory
is checked at pull-request time instead.

## Do not

- Do not `kubectl apply` these manifests by hand — change them here and let
  Argo CD reconcile (once it's bootstrapped — see `platform/argocd/`).
- Do not add a logs pipeline to the collector — the services write logs to
  stdout and Container Insights already ships them to the same workspace, so
  a second path bills the same lines twice against one daily cap.
- Do not commit real secret values — only Key Vault object *names* appear
  here, in `keyvault-secretproviderclass.yaml`.
