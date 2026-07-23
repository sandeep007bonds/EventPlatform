# 03 — Technology Stack

> This is a **recommendation with alternatives**, not a locked decision. The
> single biggest open decision is cloud + primary language — see the end of
> this doc and [09 — Roadmap: Open Decisions](09-delivery-roadmap.md#open-decisions).

## Recommended stack (Azure-leaning, given the AZ-204 context)

| Layer | Recommendation | Alternatives |
|-------|----------------|--------------|
| **Frontend** | React + Next.js (PWA), TypeScript | Angular, Vue/Nuxt |
| **Mobile (later)** | React Native or native Swift/Kotlin | Flutter |
| **Backend services** | **.NET 8 (C#)** for core services; Go for the ultra-hot inventory/queue path | Java/Spring Boot, Node.js/NestJS |
| **API Gateway** | Azure API Management / YARP; Envoy for internal | Kong, NGINX, AWS API GW |
| **Waiting room** | Dedicated service in Go/.NET on Redis | Queue-it / Akamai (buy vs build) |
| **Event bus** | **Apache Kafka** (or Azure Event Hubs, Kafka-compatible) | Pulsar, AWS Kinesis |
| **Hot inventory / holds / cache** | **Redis** (Azure Cache for Redis, clustered) | Hazelcast, KeyDB |
| **Transactional DBs** | **PostgreSQL** (Azure DB for PostgreSQL) | SQL Server, CockroachDB (for horizontal scale) |
| **Search** | Elasticsearch / OpenSearch | Azure AI Search, Algolia |
| **Analytics warehouse** | ClickHouse or Snowflake | Azure Synapse, BigQuery |
| **Object storage** | Azure Blob Storage | S3, GCS |
| **Container orchestration** | Kubernetes (AKS) | ECS/EKS, Cloud Run |
| **CDN + WAF + bot mgmt** | Azure Front Door + WAF / Cloudflare | CloudFront + AWS WAF, Akamai |
| **Payments** | Stripe (primary), Adyen; Razorpay for India | Braintree, local PSPs |
| **Messaging (email/SMS)** | SendGrid + Twilio | Azure Communication Services, SES/SNS |
| **Wallet passes** | Apple PassKit + Google Wallet API | — |
| **Auth** | OpenID Connect / OAuth2 (Azure AD B2C or Auth0/Keycloak) | Cognito, self-hosted |
| **IaC** | Terraform (or Bicep for Azure) | Pulumi, ARM |
| **CI/CD** | GitHub Actions | Azure DevOps, GitLab CI |
| **Observability** | OpenTelemetry → Prometheus/Grafana + Loki + Tempo; App Insights | Datadog, New Relic, ELK |
| **Feature flags** | LaunchDarkly / OpenFeature | Unleash |

## Why these choices

- **Redis for hot inventory.** Single-threaded atomic ops (`DECR`, Lua scripts)
  give us correct, extremely fast decrement-and-check for availability without
  lock contention. It's the workhorse of the on-sale. PostgreSQL remains the
  durable **system of record**; Redis is the fast front, reconciled to Postgres.
- **Kafka for the backbone.** Durable, replayable, high-throughput log. Perfect
  for the event-sourced audit trail and for fanning out to reporting, search,
  notifications, and wallet delivery without coupling.
- **PostgreSQL for money & orders.** ACID, mature, `SELECT ... FOR UPDATE`,
  serializable isolation when we need it. Partition/shard by event for hot
  inventory; consider CockroachDB if we need horizontal write scaling later.
- **CQRS split stores.** Selling stays fast on the write model; browsing and
  reporting scale independently on read models fed by Kafka.
- **A separate hot-path language (Go) is optional.** .NET 8 is fast enough for
  most of this; Go is worth it only if profiling shows the inventory/queue path
  needs it. Don't prematurely fragment the stack.

## Polyglot persistence summary

| Data | Store | Why |
|------|-------|-----|
| Events, venues, seat maps, pricing | PostgreSQL | Relational, strong consistency |
| Hot availability + holds (TTL) | Redis (clustered) | Atomic, fast, TTL-native |
| Durable inventory ledger | PostgreSQL (partitioned by event) | System of record, ACID |
| Orders & payments | PostgreSQL | ACID, auditable |
| Tickets | PostgreSQL + Blob (assets) | Relational + files |
| Search catalog | Elasticsearch/OpenSearch | Full-text, faceted |
| Event log / audit | Kafka (retained) | Replayable source of truth |
| Analytics | ClickHouse/Snowflake | OLAP, fast aggregation |
| Sessions / rate limits | Redis | Ephemeral, fast |

## Buy vs. build callouts

- **Waiting room:** mature SaaS exists (Queue-it, Cloudflare Waiting Room,
  Akamai). For v1, **buying** de-risks the hardest part. Build in-house later
  if economics/control demand it. This doc documents the *build* design too.
- **Anti-bot:** buy (Cloudflare Bot Management, hCaptcha Enterprise, DataDome).
- **Payments:** always buy (never build a PSP; stay PCI SAQ-A).
- **Wallets, email/SMS:** buy.
