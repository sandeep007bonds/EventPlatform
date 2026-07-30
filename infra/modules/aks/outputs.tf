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
