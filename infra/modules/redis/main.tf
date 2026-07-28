# Basic C0 serves BOTH Dapr pub/sub and the Dapr Workflow actor state store
# in this dev topology (replacing the production plan's Service Bus for
# pub/sub — see ADR-0017). maxmemory_policy MUST be noeviction: Redis'
# default (volatile-lru) would silently evict in-flight checkout-saga state
# under memory pressure instead of failing loudly.

resource "azurerm_redis_cache" "this" {
  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location

  capacity = 0
  family   = "C"
  sku_name = "Basic"

  minimum_tls_version = "1.2"

  redis_configuration {
    maxmemory_policy = "noeviction"
  }

  tags = var.tags
}
