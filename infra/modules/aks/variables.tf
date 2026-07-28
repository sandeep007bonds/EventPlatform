variable "name" {
  description = "AKS cluster name."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group to create the cluster in."
  type        = string
}

variable "location" {
  description = "Azure region."
  type        = string
}

variable "dns_prefix" {
  description = "DNS prefix for the cluster's API server."
  type        = string
}

variable "subnet_id" {
  description = "Subnet ID for the default node pool."
  type        = string
}

variable "node_count" {
  description = "Node count for the single default node pool. Defaults to 1 to keep dev cost minimal; scale up via this variable if pods don't fit."
  type        = number
  default     = 1
}

variable "node_vm_size" {
  description = "VM size for the default node pool."
  type        = string
  default     = "Standard_B2ms"
}

variable "kubernetes_version" {
  description = "Kubernetes version. Null lets Azure pick the current default."
  type        = string
  default     = null
}

variable "tags" {
  description = "Tags applied to the cluster."
  type        = map(string)
  default     = {}
}
