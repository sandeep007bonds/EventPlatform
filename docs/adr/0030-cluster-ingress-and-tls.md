# ADR-0030 — Cluster ingress and TLS: ingress-nginx + cert-manager on a free Azure hostname

- **Status:** Accepted
- **Date:** 2026-08-15

## Context

The dev cluster had no HTTPS entry point. `deploy/base/gateway/service.yaml`
was a bare `type: LoadBalancer` on port 80: a raw public IP, no hostname, no
certificate. That is reachable, but it is not usable — the frontend carries a
bearer token on every request, and sending one over plaintext HTTP to an IP
address is not something to build a habit around, quite apart from browsers
increasingly refusing to.

ADR-0017 lists Front Door + WAF as the production edge and explicitly not
built for dev. That remains true; Front Door's cost and configuration surface
are out of proportion to a personal-subscription dev environment. But "no
Front Door" was being read as "no ingress at all," which is a different and
worse thing.

The obvious blocker for real TLS is that certificates need a domain name, and
nobody here owns one.

## Decision

### ingress-nginx as the single entry point; the gateway drops its load balancer

`infra/environments/dev/ingress.tf` installs the ingress-nginx controller,
and `deploy/base/gateway/service.yaml` becomes `ClusterIP`. Public IP count
is unchanged — the controller's load balancer replaces the gateway's, rather
than adding to it. Leaving the gateway a `LoadBalancer` would have left a
second, certificate-free public IP bypassing the ingress entirely, billed for
the privilege.

The Ingress routes `/` to the gateway and knows nothing about individual
services. Adding per-service paths would let a request reach a backend
without passing the gateway's route allowlist — which is the only thing
keeping saga-internal routes (Inventory's hold convert/release, every
Payments endpoint) off the public internet.

### The free Azure FQDN is the default hostname — no domain purchase required

The controller's Service carries
`service.beta.kubernetes.io/azure-dns-label-name`, so Azure attaches
`<label>.<region>.cloudapp.azure.com` to its public IP at no cost. Because
`cloudapp.azure.com` is on the Public Suffix List, Let's Encrypt treats each
label as its own registrable domain and will issue a normal, browser-trusted
certificate for it. This is the decision that makes the whole thing free and
immediate rather than blocked on a purchase.

The label defaults to the environment's existing shared random suffix, which
is already globally unique, so no one has to invent a name that might
collide. `var.custom_domain` overrides the hostname for anyone who does own a
domain — with the honest caveat that Terraform cannot create that DNS record,
and issuance fails until the owner points a CNAME at the Azure FQDN.

### cert-manager with HTTP-01, and an explicit wait for its webhook

`helm_release`'s `wait` returns when cert-manager's pods are Ready, which is
not when its validating webhook is reachable with its CA bundle injected.
Applying a `ClusterIssuer` in that window fails with "no endpoints available
for service cert-manager-webhook" — a repeatable first-apply failure, not a
flake. A `time_sleep` covers it. The alternative was telling people to re-run
`apply`, which is how a known defect becomes folklore.

HTTP-01 over DNS-01 because HTTP-01 needs nothing but a reachable ingress,
while DNS-01 would need Azure DNS zone credentials for a zone nobody owns.
The cost is no wildcard certificates, which is fine for one hostname.

### `externalTrafficPolicy: Local`

The controller preserves the caller's source IP rather than SNATing it to a
node address. This is not a general-purpose preference: Queue's join rate
limiter (ADR-0026) buckets by client address, and under the default `Cluster`
policy every buyer would share a single bucket, so a handful of joins would
close the waiting room for everyone.

### CORS, which ingress alone does not fix

The deployed gateway runs as `Staging`, skipping `appsettings.Development.json`
— the only file that populates `Cors:AllowedOrigins`. Base `appsettings.json`
leaves it empty, so before this change every browser request to the cluster
failed preflight. An HTTPS entry point no browser can call is not an entry
point, so the dev overlay patches in an allowed origin.

That origin is `http://localhost:5173`, because the SPA is **not deployed to
the cluster** — `deploy/base/kustomization.yaml` has no frontend entry, and
the gateway serves the API only. The realistic caller is a local `npm run
dev` pointed at the cluster.

## Consequences

- The cluster has a real HTTPS URL, on a hostname, with a trusted
  certificate, at no additional cost over what it was already spending.
- Two more cluster-wide platform components install from Terraform
  (ingress-nginx, cert-manager), joining Argo CD and Dapr under the same
  reasoning: they are the substrate the application's manifests assume, and
  Argo CD cannot install what it needs in order to run.
- `letsencrypt_email` is a new **required** variable with no default. Existing
  `terraform apply` runs will prompt for it. Deliberate: a wrong address means
  finding out about a failing renewal when the site breaks.
- On first sync the host serves the controller's self-signed default
  certificate for a minute or two while ACME completes, and browsers warn
  during that window. That is issuance in progress, not a failure.
- **Still not deployed: the frontend.** This gives the API an HTTPS front
  door; it does not put the SPA behind it. Serving the built SPA from the
  cluster is separate, unbuilt work.
- Neither chart version pin could be verified from the sandbox this was
  written in (no network to the chart repositories), and CI has no Terraform
  job. Check both before applying — in particular, the cert-manager values use
  `crds.enabled`, correct from chart v1.15 onward; an older chart wants
  `installCRDs` and will silently install no CRDs, leaving every Certificate
  stuck Pending.

## Alternatives considered

- **Azure API Management.** Its proxy layer duplicates what the YARP gateway
  already does — route allowlisting, prefix stripping, auth pass-through —
  for roughly $50/month at the Developer tier (no SLA) or ~$150 at Basic v2,
  which would roughly double this environment's bill. Its real
  differentiators (subscription keys, per-consumer quotas, a developer
  portal) are for exposing APIs to third-party consumers, which nothing here
  does. Revisit as its own ADR if that changes.
- **Front Door + WAF**, the production target in ADR-0017. Not superseded —
  still the production edge. Out of proportion to dev, where the requirement
  is "a URL a browser accepts," not global anycast and managed WAF rules.
- **Azure Application Gateway Ingress Controller (AGIC).** More
  Azure-integrated, but Application Gateway itself costs ~$20+/month on top
  of the cluster and is markedly slower to reconcile. ingress-nginx runs on
  nodes already paid for.
- **A self-signed or manually-managed certificate.** Free and immediate, but
  it trains everyone to click through browser warnings, and manual renewal is
  a future outage with a date on it.
- **Let's Encrypt staging as the default issuer.** Looser rate limits, but
  issues untrusted certificates, so the default would be a setup that visibly
  does not work. Kept as a documented switch for anyone iterating on the
  ingress itself, where production's limit of 5 identical certificates per
  week is easy to burn through.
