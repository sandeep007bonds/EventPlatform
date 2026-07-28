variable "name_prefix" {
  description = "Prefix applied to the VNet/subnet names."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group to create the VNet in."
  type        = string
}

variable "location" {
  description = "Azure region."
  type        = string
}

variable "vnet_address_space" {
  description = "Address space for the VNet."
  type        = list(string)
  default     = ["10.10.0.0/16"]
}

variable "aks_subnet_address_prefix" {
  description = "Address prefix for the AKS node subnet. CNI Overlay only needs one IP per node, not per pod, so this can stay small."
  type        = list(string)
  default     = ["10.10.0.0/22"]
}

variable "tags" {
  description = "Tags applied to networking resources."
  type        = map(string)
  default     = {}
}
