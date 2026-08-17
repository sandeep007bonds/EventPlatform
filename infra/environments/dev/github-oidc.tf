# GitHub Actions authenticates to Azure via OIDC federation, not a stored
# client secret - nothing here is a credential that can leak or expire.
# Azure trusts a short-lived token GitHub mints for each workflow run,
# scoped to the exact repo+branch in each federated credential below.
#
# THIS FILE IS THE ONE PART OF THIS ENVIRONMENT THAT IS NOT SUBSCRIPTION-SCOPED.
# Everything else here is Azure Resource Manager, authorized by your role on the
# subscription (Owner/Contributor). App registrations and service principals are
# *directory* objects in Microsoft Entra ID, authorized by an entirely separate
# permission system - subscription Owner grants you none of it. A tenant where
# "Users can register applications" is off, or where you are a guest rather than
# a member, will 403 here (Authorization_RequestDenied) while every other
# resource in this config applies perfectly well.
#
# That is why this is gated: CI's push identity should never be able to block
# provisioning the cluster the app actually runs on. Set
# enable_github_oidc = false to apply everything else, and see the README for
# the two ways to get CD pushing images without directory rights.

resource "azuread_application" "github_actions" {
  count = var.enable_github_oidc ? 1 : 0

  display_name = "github-actions-${local.name_prefix}"
}

resource "azuread_service_principal" "github_actions" {
  count = var.enable_github_oidc ? 1 : 0

  client_id = azuread_application.github_actions[0].client_id
}

# One federated credential per trusted branch - GitHub's OIDC token's
# "sub" claim must match one of these exactly for Azure to accept it.
resource "azuread_application_federated_identity_credential" "github_actions" {
  for_each = var.enable_github_oidc ? toset(var.github_oidc_branches) : toset([])

  application_id = azuread_application.github_actions[0].id
  display_name   = "github-${replace(each.value, "/", "-")}"
  audiences      = ["api://AzureADTokenExchange"]
  issuer         = "https://token.actions.githubusercontent.com"
  subject        = "repo:${var.github_repository}:ref:refs/heads/${each.value}"
}

# Scoped to ONLY push images - CI never touches the cluster directly
# (that's Argo CD's job, reconciling from deploy/ after CI commits a new
# image tag), so no AKS or Key Vault role is granted here.
#
# This one IS a subscription-scoped ARM resource and would succeed on its own;
# it is gated only because it has nothing to point at without the principal.
resource "azurerm_role_assignment" "github_actions_acr_push" {
  count = var.enable_github_oidc ? 1 : 0

  scope                = module.container_registry.id
  role_definition_name = "AcrPush"
  principal_id         = azuread_service_principal.github_actions[0].object_id
}
