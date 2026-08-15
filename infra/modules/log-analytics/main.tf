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
