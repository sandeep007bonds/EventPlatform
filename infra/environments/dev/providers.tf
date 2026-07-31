provider "azurerm" {
  subscription_id = var.subscription_id

  features {
    key_vault {
      purge_soft_delete_on_destroy    = false
      recover_soft_deleted_key_vaults = true
    }
  }
}

provider "azuread" {}

# Configured against the AKS cluster this same apply creates (module.aks's
# structured kube_config_* outputs, not a kubeconfig file on disk) — this is
# what lets `terraform apply` install Argo CD in the same run instead of a
# separate by-hand bootstrap step. See argocd.tf.
#
# Known rough edge: because these depend on a resource instead of being
# static config, Terraform can't fully plan them until module.aks exists.
# That's fine for every normal apply (create-then-configure, in one pass),
# but if the AKS cluster's client certificate ever needs to be regenerated
# independently of a full cluster replacement, a follow-up `apply` may be
# needed for the provider blocks to pick up the new credentials.
provider "helm" {
  kubernetes {
    host                   = module.aks.kube_config_host
    client_certificate     = module.aks.kube_config_client_certificate
    client_key             = module.aks.kube_config_client_key
    cluster_ca_certificate = module.aks.kube_config_cluster_ca_certificate
  }
}

provider "kubectl" {
  host                   = module.aks.kube_config_host
  client_certificate     = module.aks.kube_config_client_certificate
  client_key             = module.aks.kube_config_client_key
  cluster_ca_certificate = module.aks.kube_config_cluster_ca_certificate
  load_config_file       = false
}

data "azurerm_client_config" "current" {}
