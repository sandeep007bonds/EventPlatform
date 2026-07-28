variable "name" {
  description = "Azure Cache for Redis instance name."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group to create the cache in."
  type        = string
}

variable "location" {
  description = "Azure region."
  type        = string
}

variable "tags" {
  description = "Tags applied to the cache."
  type        = map(string)
  default     = {}
}
