# Architecture Decision Records (ADRs)

This directory records the significant architecture decisions for EventPlatform.
Each record captures the **context**, the **decision**, its **consequences**, and
the **alternatives** we rejected and why — so the reasoning survives beyond the
conversation it came from.

ADRs are immutable once **Accepted**. To change a decision, add a new ADR that
supersedes the old one (mark the old one `Superseded by ADR-XXXX`).

## Index

| ADR | Title | Status |
|-----|-------|--------|
| [0001](0001-cloud-provider-azure.md) | Cloud provider: Azure, single-cloud SaaS | Accepted |
| [0002](0002-runtime-aks.md) | Runtime: AKS (Kubernetes) from day one | Accepted |
| [0003](0003-dotnet-10.md) | Language/runtime: .NET 10 (LTS) | Accepted |
| [0004](0004-cicd-github-actions-argocd.md) | CI/CD: GitHub Actions + Argo CD (GitOps) | Accepted |
| [0005](0005-iac-terraform.md) | Infrastructure as Code: Terraform | Accepted |
| [0006](0006-dapr.md) | Infrastructure abstraction: Dapr | Accepted |
| [0007](0007-monorepo.md) | Repository layout: Monorepo | Accepted |
| [0008](0008-microservices-ddd.md) | Decomposition: DDD bounded contexts, database-per-service | Accepted |
| [0009](0009-service-internal-pattern.md) | Per-service pattern: Clean Architecture + Vertical Slices + CQRS | Accepted |
| [0010](0010-messaging-and-sagas.md) | Messaging: event-driven + orchestrated saga + outbox | Accepted |
| [0011](0011-tenancy-hybrid.md) | Multi-tenancy: hybrid (pooled + cell isolation) | Accepted |
| [0012](0012-payments.md) | Payments: saga + idempotency + PCI SAQ-A | Accepted |
| [0013](0013-phase1-seated.md) | Phase 1 scope: seated events first | Accepted |
| [0014](0014-mediator-mediatr-v12.md) | In-process mediator: MediatR pinned to v12.5.0 | Accepted |
| [0015](0015-frontend-react-vite-antd-and-bff-gateway.md) | Frontend: React + Vite + Ant Design, fronted by a YARP BFF gateway | Accepted |
| [0016](0016-buyer-identity-and-notifications.md) | Buyer identity & notifications: Communication service (notifications scope); Identity deferred | Accepted |
| [0017](0017-dev-environment-cost-topology.md) | Dev environment: minimal-cost topology, diverging from production | Accepted |
| [0018](0018-media-service-and-blob-storage.md) | Media service and Azure Blob Storage for event media | Accepted |
| [0019](0019-event-tours-and-inline-location.md) | Event tours (`EventGroup`) and inline event location, replacing Venue | Accepted |
| [0020](0020-tour-dates-contact-social-and-ga-tickets.md) | Tour dates, enforced booking cutoff, contact/social, and Reserved-vs-General-Admission tickets | Accepted |
| [0021](0021-ticket-delivery-checkin-and-buyer-limits.md) | Ticket delivery email, check-in/scan, and per-buyer ticket limits | Accepted |
| [0022](0022-buyer-tenant-derivation.md) | Buyer-facing checkout: derive tenant from the resource, not the caller's claim | Accepted |
| [0023](0023-organizer-auth-in-house-identity.md) | Organizer auth: in-house email+password via Identity, superseding Entra External ID | Accepted |
| [0024](0024-scan-hardening-tour-dates-entry-gates.md) | Scan hardening, tour/leg date invariants, and entry gates | Accepted |
| [0025](0025-scan-cache-and-qr-scanning.md) | Warm-once local scan cache, real QR codes, and camera scanning | Accepted |
| [0026](0026-virtual-waiting-room-queue-service.md) | Virtual waiting-room Queue service | Accepted |
| [0027](0027-manual-sales-pause-resume.md) | Manual sales pause/resume for a published event | Accepted |
| [0028](0028-async-payment-authentication-and-hold-extension.md) | Async payment authentication (Stripe Payment Element) and hold extension | Accepted |
| [0029](0029-ef-core-migrations-and-the-database-provider-seam.md) | EF Core migrations, PostgreSQL-targeted, with the database-provider seam in one place | Accepted |
| [0030](0030-cluster-ingress-and-tls.md) | Cluster ingress and TLS: ingress-nginx + cert-manager on a free Azure hostname | Accepted |
| [0031](0031-otel-collector-and-trace-backend.md) | OpenTelemetry Collector, with Application Insights as the trace backend | Accepted |
| [0032](0032-real-token-validation-per-environment.md) | Real token validation in deployed environments; the dev signing key stays local | Accepted |

## Format

Each ADR uses: **Status**, **Context**, **Decision**, **Consequences**,
**Alternatives considered**.
