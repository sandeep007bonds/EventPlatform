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
  # Standard_B2ms (burstable, cheapest) is unavailable on some subscriptions —
  # this one's allowed-SKU list has no B-series at all, only D/E-series v7.
  # Standard_D2s_v7 is the smallest general-purpose size on that list (2 vCPU,
  # 8 GiB). If B-series ever becomes available on your subscription, switch
  # back — it's meaningfully cheaper. Verify current pricing for whichever SKU
  # you use via the Azure Pricing Calculator; the cost table in infra/README.md
  # was written against B2ms and may not reflect D2s_v7's actual rate.
  description = "AKS default node pool VM size."
  type        = string
  default     = "Standard_D2s_v7"
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
