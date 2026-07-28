variable "name" {
  description = "ACR name (alphanumeric only, 5-50 chars, globally unique)."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group to create the registry in."
  type        = string
}

variable "location" {
  description = "Azure region."
  type        = string
}

variable "tags" {
  description = "Tags applied to the registry."
  type        = map(string)
  default     = {}
}
