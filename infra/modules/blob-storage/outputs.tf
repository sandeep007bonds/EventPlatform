output "id" {
  description = "Storage account resource ID."
  value       = azurerm_storage_account.this.id
}

output "primary_connection_string" {
  description = "Primary connection string for the storage account — goes into Key Vault, never a plain output consumers read directly."
  value       = azurerm_storage_account.this.primary_connection_string
  sensitive   = true
}

output "primary_blob_endpoint" {
  description = "Primary blob service endpoint."
  value       = azurerm_storage_account.this.primary_blob_endpoint
}

output "container_name" {
  description = "The blob container name uploaded media lives in."
  value       = azurerm_storage_container.media.name
}
