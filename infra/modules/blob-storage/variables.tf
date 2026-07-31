variable "name" {
  description = "Storage account name (lowercase alphanumeric only, 3-24 chars, globally unique)."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group to create the storage account in."
  type        = string
}

variable "location" {
  description = "Azure region."
  type        = string
}

variable "container_name" {
  description = "Blob container name for uploaded media."
  type        = string
  default     = "event-media"
}

variable "tags" {
  description = "Tags applied to the storage account."
  type        = map(string)
  default     = {}
}
