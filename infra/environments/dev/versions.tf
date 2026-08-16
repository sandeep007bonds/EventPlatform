terraform {
  required_version = ">= 1.9.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 3.0"
    }
    helm = {
      source  = "hashicorp/helm"
      version = "~> 2.16"
    }
    time = {
      # Only for the cert-manager webhook settle in ingress.tf — see the comment there for why a
      # helm_release's `wait` isn't sufficient on its own.
      source  = "hashicorp/time"
      version = "~> 0.12"
    }
    kubectl = {
      # NOT hashicorp/kubernetes' kubernetes_manifest resource: that one
      # validates a manifest's CRD schema against the live cluster at plan
      # time, which fails here since Argo CD's own CRDs (installed by the
      # helm_release below) don't exist yet on a first-ever plan. This
      # provider's kubectl_manifest applies raw YAML without that check,
      # which is what makes installing Argo CD AND registering an Application
      # CR for it possible in the same apply.
      source  = "gavinbunney/kubectl"
      version = "~> 1.14"
    }
  }
}
