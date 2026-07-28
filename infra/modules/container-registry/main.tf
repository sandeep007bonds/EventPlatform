# Basic SKU, admin account disabled — AKS pulls images via its kubelet
# managed identity (AcrPull role, assigned at the root, not here) rather
# than the registry's admin credentials.

resource "azurerm_container_registry" "this" {
  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = "Basic"
  admin_enabled       = false
  tags                = var.tags
}
