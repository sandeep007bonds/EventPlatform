# The destination for AKS container logs and cluster metrics (Container Insights, wired up in the
# aks module's oms_agent block).
resource "azurerm_log_analytics_workspace" "this" {
  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location

  # PerGB2018 is the only generally available SKU; the old Free tier is retired. Cost is controlled
  # by the daily cap below rather than by the SKU.
  sku               = "PerGB2018"
  retention_in_days = var.retention_in_days
  daily_quota_gb    = var.daily_quota_gb

  tags = var.tags
}

# Workspace-based Application Insights: the destination for the OpenTelemetry traces and metrics
# the services already emit. Backing it with the workspace above rather than letting it create its
# own means one ingestion pipeline, one retention setting, and — the part that matters on a
# personal subscription — one daily cap covering container logs and traces together.
#
# The trade-off that comes with sharing a cap: a chatty trace load can starve container logging for
# the rest of the UTC day, and vice versa. Deliberate. Two independent caps would double the
# ceiling on a surprise bill, which is the thing actually worth protecting against here.
resource "azurerm_application_insights" "this" {
  name                = "appi-${var.name}"
  resource_group_name = var.resource_group_name
  location            = var.location
  workspace_id        = azurerm_log_analytics_workspace.this.id
  application_type    = "web"

  tags = var.tags
}
