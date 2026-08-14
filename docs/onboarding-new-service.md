# SOP — Onboarding a new service

Checklist for adding a new bounded-context service to EventPlatform, from
code scaffold through to a running pod in `eventplatform-dev`. Follow it in
order — later steps assume earlier ones are done. Replace `<name>` with the
service's lowercase name (e.g. `waitlist`) and `<Name>` with its PascalCase
form (e.g. `Waitlist`) throughout.

## 0. Before writing any code

- [ ] Open a tracking issue describing the service's bounded context. No PR
      without a linked issue (root [CLAUDE.md](../CLAUDE.md)).
- [ ] Branch: `feat/<issue-number>-<name>-service`. Never commit to `main`.
- [ ] If the service owns money or inventory, re-read
      [ADR-0010](adr/0010-messaging-and-sagas.md) (sagas/outbox) and
      [ADR-0012](adr/0012-payments.md) before designing its API.

## 1. Scaffold the code

- [ ] Create `services/<name>/` with the four layers **directly under it, no
      `src/` subfolder**:
      `<Name>.Domain`, `<Name>.Application`, `<Name>.Infrastructure`,
      `<Name>.Api` (+ `<Name>.Workflow` only if this service orchestrates a
      saga — see Ordering for a reference).
- [ ] Copy [`templates/service-template/CLAUDE.md`](../templates/service-template/CLAUDE.md)
      to `services/<name>/CLAUDE.md` and fill in every placeholder
      (Responsibility, Owns, events published/consumed).
- [ ] Every project: file-scoped namespace, a `GlobalUsings.cs` (no inline
      `using` directives in any other file), XML docs on all
      public/protected members. `Directory.Build.props` enforces all of this
      as build errors — see [engineering guidelines](engineering-guidelines.md).
- [ ] Add every project to `EventPlatform.slnx` — there's no auto-discovery
      glob, each `.csproj` needs an explicit `<Project Path="...">` entry.
- [ ] Any new NuGet package: pin its version in `Directory.Packages.props`,
      never in the `.csproj`. Check its licence first (MediatR, AutoMapper,
      and MassTransit are commercial now).
- [ ] `services/<name>/tests/` — unit + integration (Testcontainers), per
      root CLAUDE.md golden rule #8. Health/readiness endpoints and
      OpenTelemetry are the same rule — every service ships them.

Verify: `dotnet build EventPlatform.slnx` and `dotnet test EventPlatform.slnx`
both clean before moving on.

## 2. Database (skip if this service has no database)

- [ ] Add `<name>` to `db_names` in
      [`infra/environments/dev/locals.tf`](../infra/environments/dev/locals.tf).
      This alone makes `terraform apply` create the database on the shared
      Postgres server **and** a `<name>-connection-string` Key Vault secret
      (via the `for_each` in `environments/dev/main.tf` — no other Terraform
      change needed).
- [ ] `terraform plan`/`apply` (see `infra/environments/dev/README.md`).
      Postgres databases have `prevent_destroy = true` once created — that's
      a one-way door by design, not a bug.
- [ ] Add the new secret to **both** arrays in
      [`deploy/overlays/dev/keyvault-secretproviderclass.yaml`](../deploy/overlays/dev/keyvault-secretproviderclass.yaml)
      (the `objects` parameter *and* `secretObjects.data`) — this file is
      hand-maintained, Terraform doesn't reach into it:
      ```yaml
      # in parameters.objects's array:
        - |
          objectName: <name>-connection-string
          objectType: secret
      # in secretObjects[0].data:
        - objectName: <name>-connection-string
          key: <name>-connection-string
      ```

## 3. Container image

No new Dockerfile — every service shares the root
[`Dockerfile`](../Dockerfile) via build args. Note your project's
`PROJECT_PATH`/`ASSEMBLY_NAME` pair (add it to the Dockerfile's header
comment for the next person):

```
services/<name>/<Name>.Api/<Name>.Api.csproj -> <Name>.Api
```

## 4. Deploy manifests

- [ ] Create `deploy/base/<name>/` with `deployment.yaml`, `service.yaml`,
      `kustomization.yaml` — copy an existing service (`catalog` is a good
      template if `<name>` has a Dapr sidecar + its own database; `gateway`
      if it doesn't). Keep the same shape: Dapr annotations (unless it's a
      plain proxy like gateway), `envFrom: eventplatform-config`,
      `secretKeyRef` env vars for `ConnectionStrings__<name>` and
      `Jwt__DevSigningKey`, readiness/liveness probes, resource
      requests/limits, and — **do not skip this** — the `secrets-store` CSI
      volume + volumeMount. Without that mount, the Key Vault Secrets
      Provider never syncs `eventplatform-secrets`, and every service in the
      cluster (not just yours) silently loses its secrets on the next pod
      restart, since the sync is triggered by *any* pod mounting the class.
- [ ] If `<name>` owns a database, add `migrate-job.yaml` too (copy
      `deploy/base/catalog/migrate-job.yaml`) and list it in that service's
      `kustomization.yaml` **before** `deployment.yaml`. It runs the same image
      with `args: ["--migrate"]` as an Argo CD PreSync hook, so the schema lands
      before any new pod rolls (ADR-0029). Two things to keep as they are: the
      pod template carries **no** `dapr.io/*` annotations (a sidecar never
      exits, so the Job would never complete), and its `env` mirrors the
      Deployment's — the host is fully built before the migration branch runs,
      so anything the Deployment needs at construction, the Job needs too.
- [ ] Add `<name>` to `deploy/base/kustomization.yaml`'s `resources` list.
- [ ] Add a `<name>-placeholder` entry to `deploy/overlays/dev/kustomization.yaml`'s
      `images` list (`newName: REPLACE_ME.azurecr.io/<name>`,
      `newTag: REPLACE_ME` — CI overwrites both on the next push).

## 5. CD workflow

- [ ] Add `<name>` to all three associative arrays (`service_path`,
      `project_path`, `assembly_name`) and to the `for svc in ...` loop in
      the `detect-changes` job of
      [`.github/workflows/cd.yml`](../.github/workflows/cd.yml). That job
      computes which services changed and builds a matrix from it — there's
      no static matrix to edit anymore, and no separate `update-manifests`
      change needed, since it derives its `kustomize edit set image`
      arguments from whatever `detect-changes` decided to build.

## 6. Argo CD

Nothing to do. `eventplatform-dev`'s `Application`
(`platform/argocd/applications/dev.yaml`) already watches all of
`deploy/overlays/dev`, which pulls in `deploy/base/kustomization.yaml`'s
full resource list — your new service directory is picked up on the next
sync once step 4 is committed. See `platform/argocd/README.md`.

## 7. Ship it

- [ ] `dotnet format --verify-no-changes` clean.
- [ ] Open the PR against your issue. CI must be green (build + test +
      frontend lint/typecheck, unaffected by your `deploy/` changes since
      those are the same `paths-ignore`'d path CI already excludes for the
      CD bot).
- [ ] After merge, CD builds and pushes the new image and commits the tag
      bump; Argo CD reconciles. Watch it land with:
      `kubectl -n eventplatform-dev get pods -w`.

## Common mistakes this checklist exists to prevent

- Forgetting the CSI volume mount (step 4) — the service starts, but
  `eventplatform-secrets` never gets created or updated, and *every* service
  in the namespace loses secrets on next restart, not just the new one.
- Adding the database to `locals.tf` but forgetting the two array entries in
  `keyvault-secretproviderclass.yaml` (step 2) — Terraform creates the
  secret in Key Vault, but nothing ever syncs it into the cluster, so the
  new service crashes on startup with a missing `ConnectionStrings__<name>`.
- Forgetting the CD workflow matrix entry (step 5) — the service deploys
  fine manually once, but every future push silently never rebuilds its
  image.
