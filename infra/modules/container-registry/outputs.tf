output "id" {
  description = "ACR resource ID."
  value       = azurerm_container_registry.this.id
}

output "login_server" {
  description = "ACR login server hostname."
  value       = azurerm_container_registry.this.login_server
}
