locals {
  name_prefix = "eventplatform-dev"

  db_names = ["catalog", "inventory", "ordering", "payments", "ticketing", "communication", "identity", "queue"]

  tags = var.tags

  suffix = random_string.suffix.result
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
