module "resource_group" {
  source = "../../modules/resource-group"

  name     = "rg-${local.name_prefix}"
  location = var.location
  tags     = local.tags
}

module "networking" {
  source = "../../modules/networking"

  name_prefix         = local.name_prefix
  resource_group_name = module.resource_group.name
  location            = var.aks_location
  tags                = local.tags
}

module "container_registry" {
  source = "../../modules/container-registry"

  name                = "acr${replace(local.name_prefix, "-", "")}${local.suffix}"
  resource_group_name = module.resource_group.name
  location            = var.location
  tags                = local.tags
}

module "key_vault" {
  source = "../../modules/key-vault"

  # Key Vault names must be 3-24 chars — local.name_prefix alone ("eventplatform-dev")
  # is already 18 chars, so this can't reuse it in full like the other resources do.
  name                = "kv-epdev-${local.suffix}"
  resource_group_name = module.resource_group.name
  location            = var.location
  tenant_id           = data.azurerm_client_config.current.tenant_id
  tags                = local.tags
}

module "postgres" {
  source = "../../modules/postgres"

  name                   = "psql-${local.name_prefix}"
  resource_group_name    = module.resource_group.name
  location               = var.postgres_location
  administrator_password = var.postgres_administrator_password
  database_names         = local.db_names
  dev_ip_cidr            = var.dev_ip_cidr
  tags                   = local.tags
}

module "redis" {
  source = "../../modules/redis"

  name                = "redis-${local.name_prefix}"
  resource_group_name = module.resource_group.name
  location            = var.location
  tags                = local.tags
}

module "aks" {
  source = "../../modules/aks"

  name                = "aks-${local.name_prefix}"
  resource_group_name = module.resource_group.name
  location            = var.aks_location
  dns_prefix          = local.name_prefix
  subnet_id           = module.networking.aks_subnet_id
  node_count          = var.node_count
  node_vm_size        = var.node_vm_size
  tags                = local.tags
}

# --- Cross-module role assignments -----------------------------------------
# These live at the root, not inside modules/aks or modules/key-vault: a role
# assignment inside a leaf module would create a circular module dependency
# (aks needs acr's ID; acr would need aks's kubelet identity).

# AKS pulls images via its KUBELET identity, not the cluster's own identity
# block principal — assigning AcrPull to the wrong identity is the most
# common cause of a silent ImagePullBackOff that looks like a working role
# assignment.
resource "azurerm_role_assignment" "aks_acr_pull" {
  scope                = module.container_registry.id
  role_definition_name = "AcrPull"
  principal_id         = module.aks.kubelet_identity_object_id
}

# Key Vault RBAC mode requires an explicit data-plane role even for the
# applying principal — subscription Contributor is not sufficient. Omitting
# this produces a 403 on secret creation that looks like a permissions bug
# but is actually a missing role assignment.
resource "azurerm_role_assignment" "applier_key_vault_secrets_officer" {
  scope                = module.key_vault.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = data.azurerm_client_config.current.object_id
}

# The Key Vault Secrets Provider add-on's identity is what pods actually read
# secrets with at runtime (via SecretProviderClass) — a read-only role, never
# the Officer role granted to the applying principal above.
resource "azurerm_role_assignment" "aks_key_vault_secrets_user" {
  scope                = module.key_vault.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = module.aks.key_vault_secrets_provider_object_id
}

# Postgres admin password, stored immediately after the server is created.
resource "azurerm_key_vault_secret" "postgres_admin_password" {
  name         = "postgres-admin-password"
  value        = var.postgres_administrator_password
  key_vault_id = module.key_vault.id

  depends_on = [azurerm_role_assignment.applier_key_vault_secrets_officer]
}

# Redis primary access key, stored the same way.
resource "azurerm_key_vault_secret" "redis_primary_access_key" {
  name         = "redis-primary-access-key"
  value        = module.redis.primary_access_key
  key_vault_id = module.key_vault.id

  depends_on = [azurerm_role_assignment.applier_key_vault_secrets_officer]
}

# Dev-only JWT signing key, shared across every service (Jwt__DevSigningKey) —
# generated once here rather than per-service, since dev-mode auth issues and
# validates tokens with the same symmetric key across the whole platform.
# Excludes quote/backslash characters so it round-trips cleanly through a K8s
# Secret and a .NET env var without escaping.
resource "random_password" "jwt_dev_signing_key" {
  length           = 64
  override_special = "!#$%&*()-_=+[]{}<>:?"
}

resource "azurerm_key_vault_secret" "jwt_dev_signing_key" {
  name         = "jwt-dev-signing-key"
  value        = random_password.jwt_dev_signing_key.result
  key_vault_id = module.key_vault.id

  depends_on = [azurerm_role_assignment.applier_key_vault_secrets_officer]
}

# StackExchange.Redis connection string — only Inventory's hold hot path
# connects to Redis directly (bypassing Dapr); see deploy/base/inventory.
resource "azurerm_key_vault_secret" "redis_connection_string" {
  name         = "redis-connection-string"
  value        = "${module.redis.hostname}:${module.redis.ssl_port},password=${module.redis.primary_access_key},ssl=True,abortConnect=False"
  key_vault_id = module.key_vault.id

  depends_on = [azurerm_role_assignment.applier_key_vault_secrets_officer]
}

# One Npgsql connection string per service database, all pointed at the
# single shared Postgres Flexible Server (see ADR-0017).
resource "azurerm_key_vault_secret" "service_connection_strings" {
  for_each = toset(local.db_names)

  name         = "${each.value}-connection-string"
  value        = "Host=${module.postgres.fqdn};Port=5432;Database=${each.value};Username=${module.postgres.administrator_login};Password=${var.postgres_administrator_password};Ssl Mode=Require;Trust Server Certificate=true"
  key_vault_id = module.key_vault.id

  depends_on = [azurerm_role_assignment.applier_key_vault_secrets_officer]
}
