# Public-read container for organizer-supplied event media (banner images) — the browser fetches
# an uploaded image's URL directly from storage, no proxy endpoint in front of it (see the Media
# service and ADR-0018). This is the first publicly-readable resource anywhere in infra/ - every
# other storage (Postgres, Redis, Key Vault) is private. Deliberate for this use case, but worth a
# second look in review, not something that should slip through unnoticed.

resource "azurerm_storage_account" "this" {
  name                     = var.name
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  # Required for a public-read container below - without this at the account level, no container
  # in it can allow anonymous blob access no matter its own container_access_type.
  allow_nested_items_to_be_public = true

  tags = var.tags
}

resource "azurerm_storage_container" "media" {
  name                  = var.container_name
  storage_account_id    = azurerm_storage_account.this.id
  container_access_type = "blob" # anonymous read for blobs by direct URL, not container listing
}
