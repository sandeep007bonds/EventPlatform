# deploy

GitOps manifests, reconciled onto the AKS cluster by Argo CD — never applied
by hand (see the root [CLAUDE.md](../CLAUDE.md)'s GitOps rule).

## Layout

- `base/<service>/` — Deployment + Service + kustomization per service
  (catalog, inventory, ordering, payments, ticketing, communication, media,
  identity, queue, gateway). Every service that owns a database also has a
  `migrate-job.yaml` (see below).
- `overlays/dev/` — the only overlay today. Namespaces everything under
  `eventplatform-dev`, generates the shared non-secret config, sets
  per-service image placeholders (overwritten by CI on each push — see
  `.github/workflows/`), and wires Key Vault secrets in via
  `keyvault-secretproviderclass.yaml`.

## Schema migrations

Services never migrate themselves. Each database-owning service ships a
`base/<service>/migrate-job.yaml` — the *same image* as its Deployment, run
with `args: ["--migrate"]`, annotated `argocd.argoproj.io/hook: PreSync` so
Argo CD applies the schema and waits for the job to succeed before rolling a
single new pod. A failed migration fails the sync with inspectable logs
instead of crash-looping a replica (ADR-0029).

Two things about these jobs that look like omissions but are not:

- **No `dapr.io/*` annotations.** A Dapr sidecar never exits on its own, so
  the pod would stay `Running` and the Job would never complete. Migrations
  need nothing from Dapr.
- **The image name matches the Deployment's placeholder exactly.** That is
  what makes CI's `kustomize edit set image` rewrite both to the same tag, so
  the schema is never applied by a different build than the one about to
  serve traffic.

`media` and `gateway` have no job — neither owns a database.

## Secrets

Every service reads Postgres connection strings and the dev JWT signing key
from a Kubernetes `Secret` named `eventplatform-secrets`, populated at
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

## Do not

- Do not `kubectl apply` these manifests by hand — change them here and let
  Argo CD reconcile (once it's bootstrapped — see `platform/argocd/`).
- Do not commit real secret values — only Key Vault object *names* appear
  here, in `keyvault-secretproviderclass.yaml`.
