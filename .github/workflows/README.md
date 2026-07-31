# .github/workflows

- **`ci.yml`** — build, test, lint on every push and PR. `paths-ignore:
  deploy/**` so `cd.yml`'s tag-bump commits don't retrigger it (see the loop
  note in both files, and [ADR-0004](../../docs/adr/0004-cicd-github-actions-argocd.md)).
- **`cd.yml`** — runs after `ci.yml` succeeds on `main` or the current
  working branch. `detect-changes` diffs the triggering commit against its
  parent and builds/pushes only the services whose code (or `building-blocks/`
  or another shared path) actually changed, then commits just those new tags
  into `deploy/overlays/dev/kustomization.yaml`. Ambiguous cases (no parent
  commit to diff, or a shared-code change) fall back to rebuilding
  everything — it errs toward wasted builds, never a missed one. Never
  touches the cluster directly — Argo CD reconciles that commit (see
  `platform/argocd/`).

## Required repository secrets

Set these once, after `terraform apply` in `infra/environments/dev` (see that
directory's README for the exact `terraform output` commands):

| Secret                    | Source                                            |
| ------------------------- | -------------------------------------------------- |
| `AZURE_CLIENT_ID`         | `terraform output -raw github_actions_client_id`   |
| `AZURE_TENANT_ID`         | `terraform output -raw aks_tenant_id`              |
| `AZURE_SUBSCRIPTION_ID`   | whatever `subscription_id` is in your tfvars       |
| `ACR_LOGIN_SERVER`        | `terraform output -raw acr_login_server`           |

No client secret or password is stored anywhere — `AZURE_CLIENT_ID` +
`AZURE_TENANT_ID` + `AZURE_SUBSCRIPTION_ID` are used by `azure/login`'s OIDC
flow, which exchanges a short-lived GitHub-minted token for an Azure one at
run time (see `infra/environments/dev/github-oidc.tf`).

These only need updating if the AKS/Key Vault/ACR resources are destroyed
and recreated (new client ID, possibly new login server) — not on every
deploy.
