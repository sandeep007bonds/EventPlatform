variable "name" {
  description = "Postgres Flexible Server name."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group to create the server in."
  type        = string
}

variable "location" {
  description = "Azure region."
  type        = string
}

variable "administrator_login" {
  description = "Server admin username."
  type        = string
  default     = "eventplatform_admin"
}

variable "administrator_password" {
  description = "Server admin password."
  type        = string
  sensitive   = true
}

variable "sku_name" {
  description = "Flexible Server compute SKU."
  type        = string
  default     = "B_Standard_B1ms"
}

variable "storage_mb" {
  description = "Allocated storage, in MB."
  type        = number
  default     = 32768
}

variable "postgres_version" {
  description = "PostgreSQL major version. Changing this later forces server replacement (destroys all databases) — pick deliberately."
  type        = string
  default     = "16"
}

variable "database_names" {
  description = "Names of the per-service databases to create on this single shared server."
  type        = set(string)
}

variable "dev_ip_cidr" {
  description = "Optional developer IP address allowed through the server firewall, in addition to Azure services. Azure firewall rules take a single start/end IP, not real CIDR notation, so pass a bare IP (e.g. 1.2.3.4), not a /32. No VNet integration in this dev topology."
  type        = string
  default     = null
}

variable "tags" {
  description = "Tags applied to the server."
  type        = map(string)
  default     = {}
}
