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

## Format

Each ADR uses: **Status**, **Context**, **Decision**, **Consequences**,
**Alternatives considered**.
