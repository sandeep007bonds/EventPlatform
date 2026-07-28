# Single system node pool (no dedicated hot-path pools — the explicit
# divergence from ADR-0002's production topology), Free-tier control plane
# (no SLA, $0), Azure CNI Overlay (not legacy kubenet), OIDC issuer +
# Workload Identity enabled at creation so wiring Key Vault CSI later
# doesn't force a disruptive cluster recreation. See ADR-0017.

resource "azurerm_kubernetes_cluster" "this" {
  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location
  dns_prefix          = var.dns_prefix
  kubernetes_version  = var.kubernetes_version

  sku_tier = "Free"

  oidc_issuer_enabled       = true
  workload_identity_enabled = true

  default_node_pool {
    name           = "system"
    vm_size        = var.node_vm_size
    node_count     = var.node_count
    vnet_subnet_id = var.subnet_id
  }

  identity {
    type = "SystemAssigned"
  }

  # No network_policy: Azure Network Policy Manager isn't supported in CNI
  # Overlay mode (only Calico/Cilium are), and this dev topology has no
  # policy-enforcement requirement to justify adding one.
  network_profile {
    network_plugin      = "azure"
    network_plugin_mode = "overlay"
    load_balancer_sku   = "standard"
  }

  tags = var.tags
}
