# platform/argocd

One-time Argo CD install for the AKS cluster created by
`infra/environments/dev`, plus the `Application` that tells it to reconcile
`deploy/overlays/dev`.

## Why this is here, not in `infra/` or `deploy/`

- Not `infra/`: Argo CD is a cluster workload (Helm chart / raw manifests),
  not an Azure resource - Terraform provisions the cluster, it doesn't
  install things onto it.
- Not `deploy/`: `deploy/` is reconciled *by* Argo CD. Argo CD's own install
  can't be reconciled by itself - it has to exist first. That's the one
  documented exception to the root [CLAUDE.md](../../CLAUDE.md)'s "no
  kubectl/helm by hand" rule.

## Bootstrap (once per cluster)

Prerequisites: `kubectl` pointed at the cluster (`az aks get-credentials`,
per `infra/environments/dev/README.md`), and the cluster actually up.

```bash
./platform/argocd/bootstrap.sh
```

This installs Argo CD into the `argocd` namespace, waits for it to be ready,
and applies `applications/dev.yaml` (the `Application` pointing at
`deploy/overlays/dev`). It prints the initial admin password at the end -
rotate it (or wire up SSO) before this is anything but a personal dev
sandbox.

### Private repo access

If `sandeep007bonds/EventPlatform` is a private GitHub repo, Argo CD needs
credentials to clone it - the bootstrap script's `Application` apply will
succeed, but syncing will fail with an auth error until this is done:

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
the default branch name and re-apply:

```bash
kubectl apply -f platform/argocd/applications/dev.yaml
```

## Onboarding a new service - what does NOT need to change here

Adding a new service to the platform does not need a new Argo CD
`Application` or any change under `platform/argocd/`. The existing
`eventplatform-dev` Application watches all of `deploy/overlays/dev`, which
in turn pulls in all of `deploy/base` via its `kustomization.yaml` - so a new
service just needs a `deploy/base/<service>/` directory added to
`deploy/base/kustomization.yaml`'s `resources` list, and Argo CD picks it up
on its next automatic sync (self-heal polls every few minutes by default; to
sync immediately, use the UI's Sync button or `argocd app sync
eventplatform-dev` if you have the Argo CD CLI installed).

See `docs/onboarding-new-service.md` for the full checklist.

## Do not

- Do not `kubectl apply`/`helm upgrade` anything under `deploy/` by hand -
  edit the manifests and let Argo CD reconcile. This directory
  (`platform/argocd/`) is the sole exception, and only for Argo CD's own
  install.
- Do not commit `repo-credentials.yaml` - only the `.example.yaml`.
