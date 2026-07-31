# deploy

GitOps manifests, reconciled onto the AKS cluster by Argo CD — never applied
by hand (see the root [CLAUDE.md](../CLAUDE.md)'s GitOps rule).

## Layout

- `base/<service>/` — Deployment + Service + kustomization per service
  (catalog, inventory, ordering, payments, ticketing, communication, gateway).
- `overlays/dev/` — the only overlay today. Namespaces everything under
  `eventplatform-dev`, generates the shared non-secret config, sets
  per-service image placeholders (overwritten by CI on each push — see
  `.github/workflows/`), and wires Key Vault secrets in via
  `keyvault-secretproviderclass.yaml`.

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
