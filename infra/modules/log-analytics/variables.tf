variable "name" {
  description = "Log Analytics workspace name."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group to create the workspace in."
  type        = string
}

variable "location" {
  description = "Azure region."
  type        = string
}

variable "retention_in_days" {
  description = "How long to keep ingested data. 30 is the minimum Azure charges nothing extra for; longer retention is billed per GB-month."
  type        = number
  default     = 30
}

variable "daily_quota_gb" {
  description = <<-EOT
    Hard ceiling on daily ingestion, in GB. This is a real cap, not a warning: once it is hit,
    Azure STOPS ingesting for the rest of the UTC day and that data is lost, not queued. That is
    the right trade for a personal subscription — a chatty log loop should cost you visibility for
    a few hours, not produce a surprise bill. The default keeps a month's ingestion inside Azure
    Monitor's 5 GB free grant; raise it deliberately if you would rather pay than lose data.
  EOT
  type        = number
  default     = 0.15
}

variable "tags" {
  description = "Tags applied to the workspace."
  type        = map(string)
  default     = {}
}
