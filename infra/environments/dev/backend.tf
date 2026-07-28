# Partial backend config on purpose — the real values (resource_group_name,
# storage_account_name, container_name from infra/bootstrap's outputs) are
# globally unique per subscription and supplied via -backend-config flags at
# `terraform init` time, so they never need to be committed here. See this
# environment's README for the exact command.
terraform {
  backend "azurerm" {
    key = "dev.terraform.tfstate"
  }
}
