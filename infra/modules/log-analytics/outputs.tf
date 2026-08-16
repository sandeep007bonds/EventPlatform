output "id" {
  description = "Log Analytics workspace resource ID."
  value       = azurerm_log_analytics_workspace.this.id
}

output "name" {
  description = "Log Analytics workspace name."
  value       = azurerm_log_analytics_workspace.this.name
}

output "application_insights_connection_string" {
  description = "Application Insights connection string — what the OpenTelemetry Collector authenticates and routes with. Carries an instrumentation key, so it is secret."
  value       = azurerm_application_insights.this.connection_string
  sensitive   = true
}
