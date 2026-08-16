# Installs the Dapr control plane onto the AKS cluster this same apply creates.
#
# Without this, every `dapr.io/*` annotation on the service Deployments is inert: nothing injects a
# sidecar, so pub/sub, service invocation and the checkout saga's workflow engine all silently do
# nothing. The pods start and report healthy — the platform simply stops working end to end. It is
# the one piece of the architecture that cannot be inferred from the manifests alone.
#
# Same reasoning as argocd.tf for why a helm_release here doesn't violate the root CLAUDE.md's "no
# kubectl/helm by hand" rule: this is tracked IaC, not an ad-hoc command, and the control plane
# that runs everyone's sidecars is not something Argo CD can install for itself.
#
# VERSION PIN: check https://github.com/dapr/dapr/releases (and the matching chart in
# https://dapr.github.io/helm-charts/index.yaml) before applying. This must stay compatible with
# the Dapr.Client/Dapr.Workflow package versions in Directory.Packages.props — a control plane
# older than the SDK is the more painful direction, since the workflow engine is version-sensitive.
resource "helm_release" "dapr" {
  name             = "dapr"
  repository       = "https://dapr.github.io/helm-charts/"
  chart            = "dapr"
  version          = "1.15.4"
  namespace        = "dapr-system"
  create_namespace = true

  # The sidecar injector mutates pods as they are created, so it must be running and its webhook
  # registered before any annotated pod starts. Waiting here is what makes the ordering below
  # meaningful — otherwise Argo CD could roll the services into a cluster that cannot inject.
  wait    = true
  timeout = 600

  values = [
    yamlencode({
      global = {
        # A Free-tier single-node-pool cluster has no headroom for HA control-plane replicas, and
        # this mirrors the same call already made for Argo CD.
        ha = {
          enabled = false
        }
      }
    })
  ]

  depends_on = [module.aks]
}
