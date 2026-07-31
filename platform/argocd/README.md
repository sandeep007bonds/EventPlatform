# platform/argocd

Argo CD's `Application` spec for `deploy/overlays/dev` — the definition
Terraform installs by reference, and the definition you'd reapply by hand if
you were ever pointing Argo CD at a cluster this repo's Terraform doesn't
manage.

## Why this is here, not in `infra/` or `deploy/`

- Not `infra/`: this directory holds the *content* (the `Application` YAML);
  `infra/environments/dev/argocd.tf` is what actually installs Argo CD and
  applies it, via Terraform's `helm` and `kubectl` providers pointed at the
  cluster that same apply creates.
- Not `deploy/`: `deploy/` is reconciled *by* Argo CD. Argo CD's own install
  can't be reconciled by itself — it has to exist first.

## Bootstrap (once per cluster) — now part of `terraform apply`

`infra/environments/dev/argocd.tf` installs the Argo CD Helm chart and
registers `applications/dev.yaml` as part of the same `terraform apply` that
creates the AKS cluster — no separate script to run. See that file's
comments for why this doesn't violate the root [CLAUDE.md](../../CLAUDE.md)'s
"no kubectl/helm by hand" rule: it's still Terraform-tracked IaC, not an
ad-hoc command.

After `apply`, get the initial admin password (not stored in Terraform
state or output — pulled live from the cluster):

```bash
az aks get-credentials --resource-group "$(terraform output -raw resource_group_name)" \
  --name "$(terraform output -raw aks_cluster_name)"
kubectl -n argocd get secret argocd-initial-admin-secret -o jsonpath='{.data.password}' | base64 -d
```

Rotate it (or wire up SSO) before this is anything but a personal dev
sandbox. Reach the UI with:
`kubectl -n argocd port-forward svc/argocd-server 8080:443`.

If you ever need to install Argo CD onto a cluster this Terraform config
*doesn't* manage, `applications/dev.yaml` in this directory is still valid
standalone: `kubectl apply -f platform/argocd/applications/dev.yaml` after
installing Argo CD by whatever means fits that cluster.

### Private repo access

If `sandeep007bonds/EventPlatform` is a private GitHub repo, Argo CD needs
credentials to clone it — the `Application` will be registered by
`terraform apply`, but syncing will fail with an auth error until this is
done (Terraform doesn't manage this, since it'd mean putting a GitHub PAT in
Terraform state):

```bash
cp platform/argocd/repo-credentials.example.yaml platform/argocd/repo-credentials.yaml
# fill in a GitHub username + a PAT scoped to repo read
kubectl apply -f platform/argocd/repo-credentials.yaml
```

Skip this entirely if the repo is public.

## Once merged to the default branch

`applications/dev.yaml`'s `targetRevision` currently points at
`claude/enterprise-ticket-platform-w3opb0` because `deploy/` doesn't exist on
the default branch yet. Once this work merges, update `targetRevision` to
the default branch name and run `terraform apply` again — `kubectl_manifest`
picks up the change like any other Terraform-managed resource.

## Onboarding a new service — what does NOT need to change here

Adding a new service to the platform does not need a new Argo CD
`Application`, a new Terraform resource, or any change under
`platform/argocd/`. The existing `eventplatform-dev` Application watches all
of `deploy/overlays/dev`, which in turn pulls in all of `deploy/base` via its
`kustomization.yaml` — so a new service just needs a `deploy/base/<service>/`
directory added to `deploy/base/kustomization.yaml`'s `resources` list, and
Argo CD picks it up on its next automatic sync (self-heal polls every few
minutes by default; to sync immediately, use the UI's Sync button or
`argocd app sync eventplatform-dev` if you have the Argo CD CLI installed).

See `docs/onboarding-new-service.md` for the full checklist.

## Do not

- Do not `kubectl apply`/`helm upgrade` anything under `deploy/` by hand —
  edit the manifests and let Argo CD reconcile.
- Do not edit `applications/dev.yaml` and expect it to take effect on its
  own — it's read into Terraform state via `file()`
  (`infra/environments/dev/argocd.tf`), so a change here needs a
  `terraform apply` to actually reach the cluster, same as any other
  Terraform-managed resource.
- Do not commit `repo-credentials.yaml` — only the `.example.yaml`.
