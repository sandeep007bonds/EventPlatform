# HTTPS entry point for the cluster: an NGINX ingress controller in front of the gateway, with
# cert-manager issuing real Let's Encrypt certificates for it.
#
# Before this, deploy/base/gateway/service.yaml was a bare `type: LoadBalancer` — a public IP,
# port 80, no hostname, no certificate. Reachable, but nothing a browser would let you send a
# bearer token to. Both charts install here rather than through Argo CD for the same reason
# argocd.tf and dapr.tf do: they are cluster-wide platform components that the application's own
# manifests depend on existing, not part of the application.
#
# VERSION PINS: both are unverified from this sandbox (no network to the chart repos). Check
# https://github.com/kubernetes/ingress-nginx/releases and https://github.com/cert-manager/cert-manager/releases
# before applying. The cert-manager values below use `crds.enabled`, which is the key from chart
# v1.15 onward — an older chart wants `installCRDs` instead and will silently install no CRDs,
# leaving every Certificate resource stuck Pending.

resource "helm_release" "ingress_nginx" {
  name             = "ingress-nginx"
  repository       = "https://kubernetes.github.io/ingress-nginx"
  chart            = "ingress-nginx"
  version          = "4.11.3"
  namespace        = "ingress-nginx"
  create_namespace = true

  # The ClusterIssuer below solves its ACME challenge through this controller, so it has to be
  # serving before that resource is applied.
  wait    = true
  timeout = 600

  values = [
    yamlencode({
      controller = {
        # Same call as Argo CD and Dapr: a Free-tier single-node-pool cluster has no headroom for
        # replicas whose only job is surviving a node loss it cannot survive anyway.
        replicaCount = 1

        service = {
          annotations = {
            # This is what makes real TLS possible without owning a domain. Azure attaches a free
            # FQDN — <label>.<region>.cloudapp.azure.com — to the load balancer's public IP, and
            # because cloudapp.azure.com is on the Public Suffix List, Let's Encrypt treats each
            # label as its own registrable domain and will issue for it.
            "service.beta.kubernetes.io/azure-dns-label-name" = local.ingress_dns_label
          }

          # Preserves the caller's real source IP instead of SNATing it to a node address. Queue's
          # join rate limiter buckets by client address, so with the default (Cluster) every buyer
          # would share one bucket and a handful of joins would close the waiting room for
          # everyone. Safe here: the LB only health-probes nodes actually running a controller pod.
          externalTrafficPolicy = "Local"
        }
      }
    })
  ]

  depends_on = [module.aks]
}

resource "helm_release" "cert_manager" {
  name             = "cert-manager"
  repository       = "https://charts.jetstack.io"
  chart            = "cert-manager"
  version          = "v1.16.2"
  namespace        = "cert-manager"
  create_namespace = true

  wait    = true
  timeout = 600

  values = [
    yamlencode({
      crds = {
        enabled = true
      }
      replicaCount = 1
      webhook = {
        replicaCount = 1
      }
      cainjector = {
        replicaCount = 1
      }
    })
  ]

  depends_on = [module.aks]
}

# helm_release's `wait` returns once cert-manager's pods are Ready, which is not the same as its
# validating webhook being reachable and its CA bundle injected. Applying a ClusterIssuer in that
# window fails with "no endpoints available for service cert-manager-webhook" — a real, repeatable
# first-apply failure, not a flake. This is the standard mitigation; the alternative is telling
# people to re-run apply.
resource "time_sleep" "cert_manager_webhook_ready" {
  depends_on = [helm_release.cert_manager]

  create_duration = "60s"
}

# Cluster-wide, so any namespace's Ingress can reference it by name — deploy/overlays/dev's
# Ingress does exactly that via the cert-manager.io/cluster-issuer annotation.
resource "kubectl_manifest" "letsencrypt_cluster_issuer" {
  yaml_body = yamlencode({
    apiVersion = "cert-manager.io/v1"
    kind       = "ClusterIssuer"
    metadata = {
      name = "letsencrypt"
    }
    spec = {
      acme = {
        server = var.letsencrypt_server
        email  = var.letsencrypt_email

        privateKeySecretRef = {
          name = "letsencrypt-account-key"
        }

        # HTTP-01, not DNS-01: HTTP-01 needs nothing but a reachable ingress, while DNS-01 would
        # need Azure DNS zone credentials for a zone nobody here owns. The trade is that HTTP-01
        # cannot issue wildcards — fine, there is one hostname.
        solvers = [
          {
            http01 = {
              ingress = {
                class = "nginx"
              }
            }
          }
        ]
      }
    }
  })

  depends_on = [
    time_sleep.cert_manager_webhook_ready,
    helm_release.ingress_nginx,
  ]
}
