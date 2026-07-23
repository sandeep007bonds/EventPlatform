# 06 — Security & Compliance

Ticketing is a high-value target: real money, scarce inventory, personal data,
and organized adversaries (scalper bots). Security is layered — edge to data.

## Threat model (what we're defending against)

| Threat | Example | Primary defense |
|--------|---------|-----------------|
| **Scalper bots** | Scripts grabbing inventory to resell | Waiting room + bot management + CAPTCHA + purchase limits + device/identity signals |
| **Queue jumping** | Sharing/forging admission tokens | Signed, short-lived, session-bound tokens |
| **Payment fraud** | Stolen cards, chargebacks | PSP fraud tools (Radar/Sift), 3-D Secure, velocity rules |
| **Inventory abuse** | Hoarding via mass holds | Per-user limits, hold caps, anomaly detection |
| **Account takeover** | Credential stuffing | MFA, breached-password checks, rate limits, anomaly detection |
| **Ticket fraud** | Screenshot/duplicate QR reuse | Rotating secure barcodes, single-use scan validation |
| **Data breach** | PII / card theft | PCI SAQ-A (no card data), encryption, least privilege |
| **DDoS** | Volumetric / app-layer floods | CDN/WAF, rate limiting, load shedding |
| **API abuse** | Partner key misuse | OAuth2 scopes, quotas, rate limits |

## Authentication & authorization

- **OIDC/OAuth2**; social + email login; **MFA** available (enforced for
  organizers/admins).
- **RBAC**: buyer / organizer / gate-staff / platform-admin, least privilege.
- Short-lived access tokens + refresh; server-side session controls.
- Service-to-service auth via mTLS / signed service tokens inside the mesh.

## Bot & scalper defense (defense in depth)

1. **Edge:** WAF + bot management (fingerprinting, reputation, rate).
2. **Waiting room:** challenge before a queue slot; one slot per identity;
   random-at-open option.
3. **Purchase limits:** per user + per payment instrument + per device, across
   sessions (atomic counters).
4. **Signals:** device fingerprint, velocity, behavioral anomalies feed a risk
   score used to challenge/step-up/block.
5. **Post-purchase:** flag & void bulk/fraudulent buys; cooperate with resale
   controls.

## Payment & PCI

- **PCI-DSS SAQ-A** via PSP hosted fields — no card data on our systems (see
  [Payments](feature-flows/06-payments.md)).
- 3-D Secure / SCA where required; PSP fraud screening; chargeback handling.

## Data protection & privacy

- **Encryption in transit** (TLS everywhere) and **at rest** (DB, storage,
  backups).
- **PII minimization**; field-level encryption/tokenization for sensitive data.
- **GDPR/CCPA**: consent, data-subject access/erasure, retention policies,
  data residency options (region pinning).
- **Secrets** in a managed vault (Key Vault), rotated, never in code/repo.
- Audit logs for access to PII and for all money/inventory-affecting actions.

## Application security

- **OWASP Top 10** discipline: input validation, output encoding,
  parameterized queries, authz checks on every endpoint, CSRF/XSS protections.
- **SAST/DAST + dependency scanning + secret scanning** in CI.
- **Signed, single-use-ish ticket tokens**; rotating QR to defeat screenshots.
- Rate limiting and idempotency on all state-changing endpoints.
- Least-privilege IAM; network segmentation; private subnets for data stores.

## Ticket integrity at the gate

- Tickets carry a **rotating secure barcode** (time-based token), not a static
  image, so screenshots/shared images fail validation.
- Scanning marks a ticket **used** exactly once; duplicates are rejected.
- **Offline-capable** gate app validates against a signed local manifest and
  reconciles when back online (prevents both double-entry and gate outages).

## Compliance checklist

- [ ] PCI-DSS SAQ-A attestation
- [ ] GDPR/CCPA data-processing records, DPA with vendors
- [ ] Accessibility WCAG 2.1 AA (storefront + queue)
- [ ] Tax compliance per jurisdiction
- [ ] Consumer-protection / ticketing laws (price transparency, refund rules,
      resale caps where mandated)
- [ ] SOC 2 (target as the platform matures)
