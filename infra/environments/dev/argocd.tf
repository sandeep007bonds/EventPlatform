# Installs Argo CD onto the AKS cluster this same apply creates, and
# registers the Application that reconciles deploy/overlays/dev — folding
# what used to be a separate by-hand `platform/argocd/bootstrap.sh` step
# into `terraform apply` itself. This is still IaC, not a hand-run
# kubectl/helm command, so it doesn't violate the "no kubectl/helm by hand"
# rule in the root CLAUDE.md — that rule is about ad-hoc, untracked changes,
# and Argo CD's own install is the one thing that can't be deployed by Argo
# CD (see infra/CLAUDE.md and platform/argocd/README.md for the full
# reasoning, including why this doesn't extend to deploy/ itself).
#
# Check https://github.com/argoproj/argo-helm/releases for a newer chart
# version before bumping — Argo CD ships regularly and this pin may be
# behind by the time you read this.
resource "helm_release" "argocd" {
  name             = "argocd"
  repository       = "https://argoproj.github.io/argo-helm"
  chart            = "argo-cd"
  version          = "7.7.11"
  namespace        = "argocd"
  create_namespace = true

  # Defaults are otherwise fine for a personal dev cluster - a Free-tier
  # single-node-pool AKS cluster has no headroom to justify HA replicas.
  values = [
    yamlencode({
      redis-ha = { enabled = false }
      controller = {
        replicas = 1
      }
      server = {
        replicas = 1
      }
      repoServer = {
        replicas = 1
      }
      applicationSet = {
        replicaCount = 1
      }
    })
  ]
}

# The single source of truth for this Application's spec is
# platform/argocd/applications/dev.yaml (also usable standalone with
# `kubectl apply -f`, e.g. for a cluster not managed by this Terraform
# config) - read here rather than duplicated, so there's exactly one place
# to update repoURL/targetRevision.
resource "kubectl_manifest" "argocd_dev_application" {
  yaml_body = file("${path.module}/../../../platform/argocd/applications/dev.yaml")

  # helm_release.dapr as well as argocd: this Application reconciles deploy/, which now contains
  # Dapr Component manifests. Registering it before the Dapr CRDs exist would have Argo CD sync a
  # resource kind the cluster does not yet recognise.
  depends_on = [helm_release.argocd, helm_release.dapr]
}
