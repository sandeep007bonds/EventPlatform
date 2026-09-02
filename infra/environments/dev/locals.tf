locals {
  name_prefix = "eventplatform-dev"

  db_names = ["catalog", "inventory", "ordering", "payments", "ticketing", "communication", "identity", "queue", "venue"]

  tags = var.tags

  suffix = random_string.suffix.result

  # Globally unique within the region without asking anyone to invent a name, since the suffix
  # already is. Azure turns this into <label>.<region>.cloudapp.azure.com on the ingress
  # controller's public IP.
  ingress_dns_label = coalesce(var.ingress_dns_label, "${local.name_prefix}-${local.suffix}")

  azure_ingress_fqdn = "${local.ingress_dns_label}.${var.aks_location}.cloudapp.azure.com"

  # What the certificate is issued for and what the Ingress in deploy/overlays/dev serves on. A
  # custom domain only works if its DNS already points here — see var.custom_domain.
  gateway_hostname = coalesce(var.custom_domain, local.azure_ingress_fqdn)
}

# One shared suffix keeps globally-unique resource names correlated across a
# single apply, while still respecting each resource type's own naming
# constraints (storage: 3-24 lowercase+digits no hyphens; ACR: 5-50
# alphanumeric only; Key Vault: 3-24, must start with a letter).
resource "random_string" "suffix" {
  length  = 6
  special = false
  upper   = false
}
