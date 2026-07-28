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

output "kube_config_raw" {
  description = "Raw kubeconfig for the cluster."
  value       = azurerm_kubernetes_cluster.this.kube_config_raw
  sensitive   = true
}
