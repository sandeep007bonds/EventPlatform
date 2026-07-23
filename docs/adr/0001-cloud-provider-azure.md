# ADR-0001 — Cloud provider: Azure, single-cloud SaaS

- **Status:** Accepted
- **Date:** 2026-07-23

## Context

EventPlatform is a centrally-hosted, multi-tenant SaaS. Some clients run their
own infrastructure on AWS, which raised the question of whether the platform
must be cloud-agnostic / multi-cloud. The team's expertise is .NET and Azure
(AZ-204). Enterprise ticketing clients frequently require strong B2B
identity/SSO.

## Decision

Build on **Azure as a single cloud**. Because clients only *consume* the platform
(web/API), their own cloud is irrelevant to where we run. We therefore **drop the
cloud-agnostic requirement** and use Azure managed services freely.

## Consequences

- Faster to build, cheaper to operate, and a smaller security surface than a
  multi-cloud build.
- We may use Azure-native services (e.g., Azure Service Bus, Entra ID) without
  portability contortions.
- We still prefer OSS-standard engines (PostgreSQL, Redis, Kafka API) to avoid
  *needless* lock-in — a pragmatic hedge, not a portability mandate.
- If the model ever changes to deploying into clients' own clouds, this ADR must
  be revisited. The portable substrate (Kubernetes + Dapr) keeps that option
  relatively open.

## Alternatives considered

- **AWS** — the safe default in a vacuum (largest market, deepest talent pool),
  but our team is Azure/.NET-skilled; running Azure well beats running AWS while
  learning it.
- **Multi-cloud / cloud-agnostic** — rejected: real cost (portable-only engines,
  per-cloud IaC) with no payoff for a central SaaS.
- **GCP** — strong Kubernetes/analytics but smaller enterprise footprint; not
  compelling here.
