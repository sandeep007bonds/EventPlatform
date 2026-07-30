# GitHub Actions authenticates to Azure via OIDC federation, not a stored
# client secret - nothing here is a credential that can leak or expire.
# Azure trusts a short-lived token GitHub mints for each workflow run,
# scoped to the exact repo+branch in each federated credential below.

resource "azuread_application" "github_actions" {
  display_name = "github-actions-${local.name_prefix}"
}

resource "azuread_service_principal" "github_actions" {
  client_id = azuread_application.github_actions.client_id
}

# One federated credential per trusted branch - GitHub's OIDC token's
# "sub" claim must match one of these exactly for Azure to accept it.
resource "azuread_application_federated_identity_credential" "github_actions" {
  for_each = toset(var.github_oidc_branches)

  application_id = azuread_application.github_actions.id
  display_name   = "github-${replace(each.value, "/", "-")}"
  audiences      = ["api://AzureADTokenExchange"]
  issuer         = "https://token.actions.githubusercontent.com"
  subject        = "repo:${var.github_repository}:ref:refs/heads/${each.value}"
}

# Scoped to ONLY push images - CI never touches the cluster directly
# (that's Argo CD's job, reconciling from deploy/ after CI commits a new
# image tag), so no AKS or Key Vault role is granted here.
resource "azurerm_role_assignment" "github_actions_acr_push" {
  scope                = module.container_registry.id
  role_definition_name = "AcrPush"
  principal_id         = azuread_service_principal.github_actions.object_id
}
