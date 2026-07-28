# One-time bootstrap: creates the storage account that every other Terraform
# config in this repo uses as its remote state backend. This config itself
# uses LOCAL state on purpose — a backend must exist before Terraform can be
# pointed at it as one, so this can't bootstrap itself.

resource "random_string" "state" {
  length  = 6
  special = false
  upper   = false
}

resource "azurerm_resource_group" "tfstate" {
  name     = "rg-${var.prefix}-tfstate"
  location = var.location
}

resource "azurerm_storage_account" "tfstate" {
  # Storage account names must be 3-24 chars, lowercase letters+digits only —
  # truncate var.prefix defensively so a longer prefix can never overflow it.
  name                = "st${substr(replace(lower(var.prefix), "-", ""), 0, 9)}tf${random_string.state.result}"
  resource_group_name = azurerm_resource_group.tfstate.name
  location            = azurerm_resource_group.tfstate.location

  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  min_tls_version                 = "TLS1_2"
  allow_nested_items_to_be_public = false
}

resource "azurerm_storage_container" "tfstate" {
  name                  = "tfstate"
  storage_account_id    = azurerm_storage_account.tfstate.id
  container_access_type = "private"
}
