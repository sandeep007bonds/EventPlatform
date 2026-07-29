# RBAC-mode authorization, not legacy access policies. Data-plane access
# (secret read/write) still needs an explicit role assignment on top of this
# even for the subscription owner/Contributor — that's wired at the root,
# not here, since it targets whichever principal is applying Terraform.

resource "azurerm_key_vault" "this" {
  name                       = var.name
  resource_group_name        = var.resource_group_name
  location                   = var.location
  tenant_id                  = var.tenant_id
  sku_name                   = "standard"
  rbac_authorization_enabled = true
  soft_delete_retention_days = 7
  tags                       = var.tags
}
