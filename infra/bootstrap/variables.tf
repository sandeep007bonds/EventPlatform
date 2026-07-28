variable "prefix" {
  description = "Short name prefix applied to every bootstrap resource."
  type        = string
  default     = "eventplatform"
}

variable "location" {
  description = "Azure region for the Terraform remote-state storage account."
  type        = string
  default     = "eastus"
}
