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
