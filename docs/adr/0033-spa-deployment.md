# ADR-0033 — Deploying the SPA: static nginx image, same-origin, one image per build

- **Status:** Accepted
- **Date:** 2026-08-15

## Context

Everything up to ADR-0032 gave the cluster a working API behind an HTTPS
hostname. The SPA in `frontend/` was not deployed at all — no entry in
`deploy/base/kustomization.yaml`, no image, no CD step. It ran only on a
developer's laptop via `npm run dev`, which is why the dev overlay allows
`http://localhost:5173` as a browser origin.

So the URL served an API and nothing a person could open.

## Decision

### A static nginx image, not a Node server

`npm run build` emits static files; nothing in this app needs server-side
rendering (ADR-0015 already rejected Next.js on those grounds). A two-stage
build compiles with `node:22-alpine` and ships only `dist/` on
`nginx:1.27-alpine`, listening on 8080 to match every other pod. The runtime
image carries no Node, no `node_modules`, and no application source.

### Same-origin, which removes two problems at once

The ingress routes `/api` to the gateway and everything else to the SPA, on
one hostname. `VITE_GATEWAY_BASE_URL` is therefore left **unset** at build
time, which makes axios issue relative requests against its own origin.

Two consequences fall out of that single choice:

- **No CORS for the deployed app.** Same origin, so the mechanism never
  engages.
- **One image for every environment.** Vite inlines `VITE_*` at build time, so
  a baked-in hostname would mean one build per environment — the thing that
  makes "promote the tested artifact" impossible. An image that asks its own
  origin is portable by construction.

This also decides the ingress path split in the only safe direction: `/api`
is matched by longer prefix and wins, and the ingress still knows nothing
about individual services, so the gateway's route allowlist remains the only
way in (ADR-0030).

### The one build-time value that survives

`VITE_STRIPE_PUBLISHABLE_KEY`. A publishable key identifies the account and
can only create tokens — it is designed to be public and ships to the browser
regardless, so baking it into the image leaks nothing. Passed from a GitHub
Actions **variable**, not a secret, to say that plainly. An unset key still
builds; checkout falls back to a canned test payment method.

### CD branches on `project_path` being empty

Rather than adding an is-it-the-frontend flag to every associative array, the
frontend's `project_path`/`assembly_name` are empty, and the build step reads
that as "this one brings its own Dockerfile and context". One conditional
instead of a parallel set of arrays.

### nginx config carries two non-obvious rules

- `try_files $uri $uri/ /index.html` — the router is client-side, so
  `/events/<id>/seats` is not a file. Without the fallback the app works only
  if you enter at `/` and navigate; refreshing any inner page 404s.
- `index.html` is served `no-cache` while `/assets/` is `immutable` for a
  year. Vite fingerprints asset filenames, so they are safe to cache forever —
  but `index.html` is the only file naming the current hashes. Cache it, and a
  returning visitor asks for asset URLs the new deploy already deleted, and
  sees a blank page until a hard refresh.

## Consequences

- The HTTPS hostname now serves the actual product. A link to it works for
  anyone.
- The gateway's `/health/*` and `/scalar/v1` are no longer reachable from
  outside, since `/` now goes to the SPA. Probes hit pods directly, and API
  docs are not something to publish unintentionally — but if you were using
  the deployed Scalar page, it is gone.
- The dev overlay's CORS patch stays, now serving only a local `npm run dev`
  against the cluster. Removing it would lock the deployed API to the deployed
  SPA — stricter, and probably where this should end up.
- One more image in ACR and one more pod (10m CPU, 32Mi) on a single-node
  cluster.
- The frontend has no `secrets-store` CSI mount, unlike every other pod. It
  needs no secrets, and anything it could read would reach the browser anyway.
  The synced Secret is unaffected — ten other pods already mount the class.
- **Partly verified.** The build stage was run for real (Node 22): it produces
  `dist/index.html` plus fingerprinted `dist/assets/index-<hash>.{js,css}`,
  which is what the nginx cache rules assume, and the bundle contains
  `baseURL: void 0` with no hostname anywhere — the same-origin claim is
  observed, not assumed. The **image** has not been built: the authoring
  sandbox has the Docker CLI but no daemon. So the nginx stage, the `COPY`
  paths and the base image tags are still unproven; first real proof is a CD
  run.

## Alternatives considered

- **Runtime configuration** (a `config.js` written at container start, or a
  `/config` endpoint) instead of build-time inlining. The standard answer to
  Vite's build-time env problem, and unnecessary once the app targets its own
  origin — it would add a moving part to solve a problem this design does not
  have.
- **Serving the SPA from the gateway** as static files. One fewer image and
  pod, but it makes a stateless proxy also a web server, couples frontend
  releases to gateway releases, and gives up nginx's caching behaviour.
- **Azure Static Web Apps / Blob static hosting + CDN.** Cheaper and faster
  for the assets, at the cost of a second public origin, real CORS, and a
  hosting model that diverges from every other component here. Worth
  revisiting when asset delivery cost or latency is an actual problem.
- **A separate hostname for the API** (`api.<host>`). Conventional, but
  reintroduces CORS and a per-environment build for no benefit at this size.
