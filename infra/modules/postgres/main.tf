# Single Flexible Server shared by all 5 services' databases — the dev-topology
# divergence from one-server-per-service (see ADR-0017). Burstable B1ms is the
# cheapest usable tier and may fall under a new subscription's free allowance.

resource "azurerm_postgresql_flexible_server" "this" {
  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location

  administrator_login    = var.administrator_login
  administrator_password = var.administrator_password

  sku_name   = var.sku_name
  storage_mb = var.storage_mb
  version    = var.postgres_version

  backup_retention_days        = 7
  geo_redundant_backup_enabled = false

  tags = var.tags

  lifecycle {
    ignore_changes = [zone]
  }
}

# One database per bounded context, all on the shared server above.
# prevent_destroy: removing a name from database_names must never silently
# drop that service's database on the next apply.
resource "azurerm_postgresql_flexible_server_database" "this" {
  for_each = var.database_names

  name      = each.value
  server_id = azurerm_postgresql_flexible_server.this.id
  collation = "en_US.utf8"
  charset   = "utf8"

  lifecycle {
    prevent_destroy = true
  }
}

# No VNet integration in this dev topology — public access, locked down by
# firewall rules only. "Allow Azure services" lets AKS pods reach the server.
resource "azurerm_postgresql_flexible_server_firewall_rule" "allow_azure_services" {
  name             = "allow-azure-services"
  server_id        = azurerm_postgresql_flexible_server.this.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

resource "azurerm_postgresql_flexible_server_firewall_rule" "dev_ip" {
  count = var.dev_ip_cidr == null ? 0 : 1

  name             = "allow-dev-ip"
  server_id        = azurerm_postgresql_flexible_server.this.id
  start_ip_address = var.dev_ip_cidr
  end_ip_address   = var.dev_ip_cidr
}
