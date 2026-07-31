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
  GitOps rule. `environments/dev/argocd.tf`'s `helm_release`/
  `kubectl_manifest` resources are the sanctioned exception: still
  Terraform-tracked IaC (reviewed in `terraform plan`, applied by
  `terraform apply`), not an ad-hoc command — and limited to installing
  Argo CD itself, never anything under `deploy/`.
