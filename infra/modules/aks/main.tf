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

  # Managed add-on: installs the Secrets Store CSI Driver + Azure Key Vault
  # provider and provisions a dedicated identity for pods to authenticate to
  # Key Vault with (via a SecretProviderClass), distinct from both the
  # cluster's own identity and the kubelet identity used for ACR pulls.
  # secret_rotation_enabled polls Key Vault periodically so a rotated secret
  # reaches mounted volumes without a pod restart.
  key_vault_secrets_provider {
    secret_rotation_enabled  = true
    secret_rotation_interval = "2m"
  }

  # Container Insights: ships container stdout/stderr and node/pod metrics to Log Analytics. This
  # is what makes a deployed problem diagnosable at all — without it, the only window into a
  # misbehaving service is `kubectl logs` against a pod that may already have been replaced.
  #
  # It collects logs and metrics, NOT distributed traces. The services export OTLP traces via
  # OTEL_EXPORTER_OTLP_ENDPOINT, and nothing in the cluster listens on that yet — see the
  # observability note in infra/README.md.
  dynamic "oms_agent" {
    for_each = var.log_analytics_workspace_id == null ? [] : [1]

    content {
      log_analytics_workspace_id = var.log_analytics_workspace_id
    }
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
