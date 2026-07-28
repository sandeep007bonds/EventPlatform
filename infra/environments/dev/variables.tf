variable "subscription_id" {
  description = "Azure subscription ID to deploy into."
  type        = string
}

variable "location" {
  description = "Azure region for every resource in this environment."
  type        = string
  default     = "eastus"
}

variable "node_count" {
  description = "AKS default node pool count. Defaults to 1 to keep dev cost minimal."
  type        = number
  default     = 1
}

variable "node_vm_size" {
  description = "AKS default node pool VM size."
  type        = string
  default     = "Standard_B2ms"
}

variable "postgres_administrator_password" {
  description = "Postgres Flexible Server admin password."
  type        = string
  sensitive   = true
}

variable "dev_ip_cidr" {
  description = "Optional developer IP address allowed through the Postgres firewall (bare IP, not CIDR notation — see infra/modules/postgres). Leave null to skip."
  type        = string
  default     = null
}

variable "tags" {
  description = "Tags applied to every resource in this environment."
  type        = map(string)
  default = {
    environment = "dev"
    managed_by  = "terraform"
    project     = "eventplatform"
  }
}
