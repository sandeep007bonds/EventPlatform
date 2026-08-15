# ADR-0031 — OpenTelemetry Collector, with Application Insights as the trace backend

- **Status:** Accepted
- **Date:** 2026-08-15

## Context

Every service has emitted OpenTelemetry traces and metrics since the first
one was written — `AddDefaultObservability` wires OTLP export into all of
them. In the cluster those exports went nowhere: `OTEL_EXPORTER_OTLP_ENDPOINT`
was unset in every manifest under `deploy/`, so the exporter had no
destination. ADR-0030's pass documented this rather than fixing it.

Locally the same services export to Jaeger (`docker-compose.yml`), and
`platform/dapr/config.yaml` points the Dapr sidecars at the same endpoint. So
the deployed environment was the only place where a saga could misbehave and
leave no trace of it — which is the environment where that is hardest to
reproduce by other means.

Container Insights (ADR-0017's later pass) covers container logs and
node/pod metrics. It does not do distributed tracing, and no amount of
configuring it will.

## Decision

### A collector, not a direct exporter in each service

`deploy/base/observability/` runs an OpenTelemetry Collector; every service
points `OTEL_EXPORTER_OTLP_ENDPOINT` at it.

The alternative — the Azure Monitor OpenTelemetry distro referenced directly
from `EventPlatform.Hosting` — would have been one fewer pod. It would also
have put a vendor SDK in the one building block every service depends on, and
required a code change plus a config gate to keep local development working.
The collector needed **zero C# changes**: the services already speak OTLP to
whatever the environment variable names, and this pass just gave that variable
a value. Changing trace backends later is one file in `deploy/`, not a
rebuild of ten services.

### Application Insights, backed by the existing Log Analytics workspace

Workspace-based, so traces and container logs land in one workspace under one
retention setting and — the part that matters on a personal subscription —
**one daily ingestion cap**.

The cost of sharing that cap, stated plainly because it will eventually
surprise someone: a chatty trace load can starve container logging for the
rest of the UTC day, and the reverse. Two separate caps would have avoided
that at the price of doubling the ceiling on an unexpected bill, which is the
risk actually worth managing here.

### No sampling, and the daily cap as the backstop

Dev traffic is normally one person clicking through one checkout. A sampler
that drops 90% of that leaves you debugging the trace it threw away. Full
fidelity is the right default at this traffic level; `probabilistic_sampler`
is the documented knob to add before any load test, because a k6 run at full
fidelity will spend a day's budget in minutes.

### Traces and metrics pipelines; deliberately no logs pipeline

`AddDefaultObservability` configures the OpenTelemetry logging provider
**without** an OTLP exporter, so the services never send logs this way — they
write to stdout, which Container Insights already ships to the same
workspace. A logs pipeline here would bill the same lines twice against one
cap.

### Dapr sidecar tracing too

A Dapr `Configuration` named `tracing`, referenced by each service's
`dapr.io/config` annotation. Without it, sidecars emit no spans and a trace
goes quiet at every pub/sub hop — exactly where the checkout saga's
interesting behaviour lives, and the part you cannot recover by reading a
stack trace. The services' own spans would still connect (Dapr propagates
W3C `traceparent` regardless), but the delivery itself would be an
unexplained gap in the timeline.

### The pipeline config is a generated ConfigMap

`configMapGenerator` in `deploy/base/observability/kustomization.yaml`, so
kustomize hashes the content into the name and editing the pipeline actually
rolls the collector. A hand-written ConfigMap updates in place and leaves the
old config running until someone notices and restarts the pod.

## Consequences

- Traces from all ten services and their Dapr sidecars are queryable end to
  end in Application Insights, including across pub/sub boundaries.
- One more pod on a single-node Free-tier cluster (128Mi requested, 512Mi
  limit). `memory_limiter` is first in every pipeline: an OOMKilled collector
  takes its buffered telemetry with it, so shedding under pressure loses less
  than dying does.
- Traces and container logs now compete for one 0.15 GB/day budget. Raise
  `log_analytics_daily_quota_gb` or add sampling before load testing —
  whichever you would rather pay for.
- A new Key Vault secret (`appinsights-connection-string`) in both arrays of
  `keyvault-secretproviderclass.yaml`. Missing it from either would fail the
  CSI mount for every pod in the namespace, not just the collector.
- The collector image must be the **contrib** distribution; the core image
  fails to load this config with "unknown type: azuremonitor", which reads
  like a config typo rather than a wrong image.
- Local development is untouched — still Jaeger, still `localhost:4317`.
  `platform/dapr/config.yaml` and the new in-cluster Dapr `Configuration` are
  now parallel files that must be kept in step by hand.
- Same verification limits as the rest of this infra work: `terraform fmt`
  and YAML parsing pass, `terraform validate` could not run (provider registry
  unreachable), and CI has no Terraform or kustomize job. The collector image
  pin is unverified.

## Alternatives considered

- **Azure Monitor OpenTelemetry distro in `EventPlatform.Hosting`.** Fewer
  moving parts, but couples every service to one vendor at the deepest shared
  layer and needs a code change plus a local-dev gate. Rejected above.
- **Jaeger in-cluster.** Free and already familiar from local dev, but its
  all-in-one image stores traces in memory, so a pod restart loses history —
  and it would need its own ingress and auth to be reachable, which is more
  work than the exporter it replaced.
- **A separate Log Analytics workspace for traces.** Isolates the two budgets
  so traces cannot starve logs, at the cost of two caps to manage and a
  higher combined ceiling. Revisit if trace volume actually starts crowding
  out logging.
- **Sampling on by default.** Correct at production volume, wrong here: it
  optimizes ingestion cost against the one thing this environment exists to
  do, which is let you follow a single request all the way through.
- **DaemonSet rather than Deployment for the collector.** Standard at scale
  for node-local buffering; pointless on a single-node cluster, and it would
  scale with nodes rather than with telemetry volume.
