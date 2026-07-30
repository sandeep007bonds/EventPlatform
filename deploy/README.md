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
must be filled in once per environment, after `terraform apply`:

```bash
cd infra/environments/dev
terraform output aks_key_vault_secrets_provider_client_id
terraform output key_vault_name
terraform output aks_tenant_id
```

Paste those into `userAssignedIdentityID`, `keyvaultName`, and `tenantId`
respectively, then commit. These change only if the AKS cluster or Key Vault
is destroyed and recreated — not on every deploy.

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
