output "resource_group_name" {
  description = "Name of the resource group holding the Terraform state storage account."
  value       = azurerm_resource_group.tfstate.name
}

output "storage_account_name" {
  description = "Name of the storage account used as the azurerm backend for every other Terraform config."
  value       = azurerm_storage_account.tfstate.name
}

output "container_name" {
  description = "Blob container name used for state files."
  value       = azurerm_storage_container.tfstate.name
}
