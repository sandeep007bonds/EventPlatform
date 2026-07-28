variable "name" {
  description = "Key Vault name (must start with a letter, 3-24 chars, globally unique)."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group to create the vault in."
  type        = string
}

variable "location" {
  description = "Azure region."
  type        = string
}

variable "tenant_id" {
  description = "Azure AD tenant ID for the vault."
  type        = string
}

variable "tags" {
  description = "Tags applied to the vault."
  type        = map(string)
  default     = {}
}
