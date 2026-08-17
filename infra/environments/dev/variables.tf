variable "subscription_id" {
  description = "Azure subscription ID to deploy into."
  type        = string
}

variable "location" {
  description = "Azure region for every resource in this environment."
  type        = string
  default     = "eastus"
}

variable "postgres_location" {
  # Postgres Flexible Server has its own per-subscription region allow-list,
  # separate from every other resource type here — a region that works for
  # AKS/VNet/Key Vault/Redis can still be offer-restricted for Postgres.
  # Kept independent of `location` so a Postgres restriction doesn't force
  # moving (and replacing) every other resource in the environment.
  description = "Azure region for the Postgres Flexible Server. Defaults to `location` but can be overridden if Postgres Flexible Server is offer-restricted there for your subscription."
  type        = string
  default     = "eastus"
}

variable "aks_location" {
  # AKS cluster creation can fail with AKSCapacityHeavyUsage in a region that
  # otherwise works fine for every other resource here — a transient
  # Azure-side capacity throttle, not an offer restriction, so it can hit
  # (and clear) unpredictably. Kept independent of `location` so you can move
  # just AKS (and its VNet/subnet, which must share AKS's region) to a region
  # with available capacity without forcing Key Vault, Postgres, or Redis to
  # move too. The networking module also takes this value, since an AKS
  # cluster's VNet must be in the same region as the cluster itself.
  description = "Azure region for the AKS cluster and its VNet/subnet. Defaults to `location` but can be overridden if AKS cluster creation is capacity-restricted there."
  type        = string
  default     = "eastus"
}

variable "node_count" {
  description = "AKS default node pool count. Defaults to 1 to keep dev cost minimal."
  type        = number
  default     = 1
}

variable "node_vm_size" {
  # Standard_B2ms (burstable, cheapest) is unavailable on some subscriptions —
  # this one's allowed-SKU list has no B-series at all, only D/E-series v7.
  # Standard_D2s_v7 is the smallest general-purpose size on that list (2 vCPU,
  # 8 GiB). If B-series ever becomes available on your subscription, switch
  # back — it's meaningfully cheaper. Verify current pricing for whichever SKU
  # you use via the Azure Pricing Calculator; the cost table in infra/README.md
  # was written against B2ms and may not reflect D2s_v7's actual rate.
  description = "AKS default node pool VM size."
  type        = string
  default     = "Standard_D2s_v7"
}

variable "postgres_administrator_password" {
  description = "Postgres Flexible Server admin password."
  type        = string
  sensitive   = true
}

variable "dev_ip_cidr" {
  description = "Optional developer IP address allowed through the Postgres firewall (bare IP, not CIDR notation — see infra/modules/postgres). Leave null to skip."
  type        = string
  default     = null
}

variable "enable_github_oidc" {
  description = "Create the Entra ID app registration + service principal + federated credentials that let GitHub Actions push images to ACR. Requires Microsoft Entra ID *directory* permissions, which are separate from your Azure subscription role — subscription Owner is NOT sufficient. Set false if `terraform apply` 403s with Authorization_RequestDenied on the azuread_* resources; everything else in this environment applies fine without it, and infra/README.md documents how to give CD a push identity another way."
  type        = bool
  default     = true
}

variable "entra_tenant_id" {
  description = "Microsoft Entra ID tenant the azuread provider targets. Leave null to inherit the Azure CLI's home tenant, which is correct when the subscription lives in your own directory. Set it explicitly (to `az account show --query tenantId -o tsv`) when the subscription belongs to a different tenant than your sign-in home tenant — otherwise app registrations land in one directory while their service principals are attempted in another."
  type        = string
  default     = null
}

variable "github_repository" {
  description = "GitHub \"owner/repo\" that the CD workflow's OIDC federated identity trusts. Must match the repo that runs .github/workflows/cd.yaml exactly, or the federated credential's subject claim won't match and Azure login in CI will fail."
  type        = string
  default     = "sandeep007bonds/EventPlatform"
}

variable "github_oidc_branches" {
  # "main" is included pre-emptively for when this work merges - an unused
  # federated credential is harmless, unlike missing one when you need it.
  description = "Branches allowed to assume the GitHub Actions OIDC identity (each becomes one federated credential, subject repo:<github_repository>:ref:refs/heads/<branch>)."
  type        = list(string)
  default     = ["main", "claude/enterprise-ticket-platform-w3opb0"]
}

variable "log_analytics_daily_quota_gb" {
  description = "Hard daily ingestion cap for Log Analytics, in GB. The default keeps a month inside Azure Monitor's 5 GB free grant; past the cap, data is dropped for the rest of the UTC day rather than billed."
  type        = number
  default     = 0.15
}

variable "letsencrypt_email" {
  description = <<-EOT
    Contact address registered with Let's Encrypt. Used only for expiry and account notices — it is
    not published anywhere and does not appear in the issued certificate. Deliberately has no
    default: a wrong address here means you find out a renewal is failing when the site breaks.
  EOT
  type        = string
}

variable "letsencrypt_server" {
  description = <<-EOT
    ACME directory URL. The default is Let's Encrypt production, which issues browser-trusted
    certificates and is what you actually want. Switch to the staging directory
    (https://acme-staging-v02.api.letsencrypt.org/directory) while debugging the ingress or DNS
    label: staging certificates are untrusted (the browser will warn) but its rate limits are far
    looser, and production's limit of 5 identical certificates per week is easy to burn through
    while iterating. Changing this issues a fresh certificate from the new authority.
  EOT
  type        = string
  default     = "https://acme-v02.api.letsencrypt.org/directory"
}

variable "ingress_dns_label" {
  description = <<-EOT
    Azure DNS label for the ingress controller's public IP, giving the cluster a free hostname at
    <label>.<aks_location>.cloudapp.azure.com. Must be globally unique within the region. Null
    derives one from the environment's shared random suffix, which is already unique — set this
    only if you want a hostname you can recognise.
  EOT
  type        = string
  default     = null
}

variable "custom_domain" {
  description = <<-EOT
    Hostname to serve the gateway on instead of the free Azure FQDN. Terraform cannot create this
    for you: you must own the domain and point a CNAME at the Azure FQDN (or an A record at the
    ingress IP) yourself, before applying — cert-manager proves control of the name over HTTP, so
    issuance fails until that record resolves. Null uses the Azure FQDN, which needs nothing.
  EOT
  type        = string
  default     = null
}

variable "tags" {
  description = "Tags applied to every resource in this environment."
  type        = map(string)
  default = {
    environment = "dev"
    managed_by  = "terraform"
    project     = "eventplatform"
  }
}
