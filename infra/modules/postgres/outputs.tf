output "id" {
  description = "Postgres Flexible Server resource ID."
  value       = azurerm_postgresql_flexible_server.this.id
}

output "fqdn" {
  description = "Postgres Flexible Server FQDN."
  value       = azurerm_postgresql_flexible_server.this.fqdn
}

output "administrator_login" {
  description = "Server admin username, for building per-database connection strings."
  value       = azurerm_postgresql_flexible_server.this.administrator_login
}

output "database_names" {
  description = "Names of the databases created on the shared server."
  value       = [for db in azurerm_postgresql_flexible_server_database.this : db.name]
}
