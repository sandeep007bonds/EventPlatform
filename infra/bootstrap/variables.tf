variable "subscription_id" {
  description = <<-EOT
    Azure subscription to create the remote-state storage in. Deliberately has no default, so it is
    a conscious choice rather than "whatever `az account show` happens to return" — on a machine
    signed into more than one account, the wrong default puts this environment's Terraform state in
    someone else's subscription, and nothing about the apply would say so. Should normally be the
    same subscription as infra/environments/dev's `subscription_id`.
  EOT
  type        = string
}

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
