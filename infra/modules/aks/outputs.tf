output "id" {
  description = "AKS cluster resource ID."
  value       = azurerm_kubernetes_cluster.this.id
}

output "name" {
  description = "AKS cluster name."
  value       = azurerm_kubernetes_cluster.this.name
}

output "kubelet_identity_object_id" {
  description = "Object ID of the cluster's kubelet managed identity — this, not the cluster identity, is what needs the AcrPull role for image pulls."
  value       = azurerm_kubernetes_cluster.this.kubelet_identity[0].object_id
}

output "oidc_issuer_url" {
  description = "OIDC issuer URL, needed for Workload Identity federation."
  value       = azurerm_kubernetes_cluster.this.oidc_issuer_url
}

output "key_vault_secrets_provider_object_id" {
  description = "Object ID of the Key Vault Secrets Provider add-on's managed identity — grant this Key Vault data-plane read access, not the cluster or kubelet identity."
  value       = azurerm_kubernetes_cluster.this.key_vault_secrets_provider[0].secret_identity[0].object_id
}

output "key_vault_secrets_provider_client_id" {
  description = "Client ID of the Key Vault Secrets Provider add-on's managed identity — referenced by userAssignedIdentityID in every SecretProviderClass in deploy/."
  value       = azurerm_kubernetes_cluster.this.key_vault_secrets_provider[0].secret_identity[0].client_id
}

output "kube_config_raw" {
  description = "Raw kubeconfig for the cluster."
  value       = azurerm_kubernetes_cluster.this.kube_config_raw
  sensitive   = true
}

# Structured (not raw-YAML) cluster-admin credentials, for configuring the
# helm/kubectl Terraform providers directly against this cluster in the same
# apply — see environments/dev/providers.tf and argocd.tf. Client-certificate
# auth, not a kubeconfig file on disk.
output "kube_config_host" {
  description = "AKS API server host, for the helm/kubectl provider blocks."
  value       = azurerm_kubernetes_cluster.this.kube_config[0].host
  sensitive   = true
}

output "kube_config_client_certificate" {
  description = "Cluster-admin client certificate (PEM, base64-decoded), for the helm/kubectl provider blocks."
  value       = base64decode(azurerm_kubernetes_cluster.this.kube_config[0].client_certificate)
  sensitive   = true
}

output "kube_config_client_key" {
  description = "Cluster-admin client key (PEM, base64-decoded), for the helm/kubectl provider blocks."
  value       = base64decode(azurerm_kubernetes_cluster.this.kube_config[0].client_key)
  sensitive   = true
}

output "kube_config_cluster_ca_certificate" {
  description = "Cluster CA certificate (PEM, base64-decoded), for the helm/kubectl provider blocks."
  value       = base64decode(azurerm_kubernetes_cluster.this.kube_config[0].cluster_ca_certificate)
  sensitive   = true
}
