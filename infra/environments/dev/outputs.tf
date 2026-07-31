output "subscription_id" {
  description = "The Azure subscription this environment was applied into — same value as var.subscription_id, exposed here so scripts/finish-dev-bootstrap.sh doesn't need to parse tfvars separately."
  value       = var.subscription_id
}

output "resource_group_name" {
  description = "Resource group holding every dev resource."
  value       = module.resource_group.name
}

output "aks_cluster_name" {
  description = "AKS cluster name — use with `az aks get-credentials`."
  value       = module.aks.name
}

output "acr_login_server" {
  description = "ACR login server — use with `az acr login`."
  value       = module.container_registry.login_server
}

output "postgres_fqdn" {
  description = "Postgres Flexible Server FQDN."
  value       = module.postgres.fqdn
}

output "postgres_database_names" {
  description = "Databases created on the shared Postgres server."
  value       = module.postgres.database_names
}

output "redis_hostname" {
  description = "Redis cache hostname."
  value       = module.redis.hostname
}

output "key_vault_uri" {
  description = "Key Vault URI — holds the Postgres admin password and Redis access key."
  value       = module.key_vault.uri
}

output "key_vault_name" {
  description = "Key Vault name — needed by every SecretProviderClass in deploy/."
  value       = module.key_vault.name
}

output "aks_key_vault_secrets_provider_client_id" {
  description = "Client ID of the AKS Key Vault Secrets Provider add-on identity — belongs in userAssignedIdentityID in deploy/overlays/dev/keyvault-secretproviderclass.yaml. Run scripts/finish-dev-bootstrap.sh instead of copying this by hand."
  value       = module.aks.key_vault_secrets_provider_client_id
}

output "aks_tenant_id" {
  description = "Azure AD tenant ID — needed by the tenantId field in every SecretProviderClass in deploy/, and by the AZURE_TENANT_ID GitHub Actions secret."
  value       = data.azurerm_client_config.current.tenant_id
}

output "github_actions_client_id" {
  description = "Client ID of the GitHub Actions OIDC application — set this as the AZURE_CLIENT_ID GitHub Actions secret (or repo variable) for the CD workflow's azure/login step."
  value       = azuread_application.github_actions.client_id
}
