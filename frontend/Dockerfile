# Builds the SPA to static files and serves them with nginx. Separate from the repo-root
# Dockerfile, which is the generic .NET one — this shares nothing with it.
#
# Build context is frontend/, not the repo root: nothing outside this directory is needed.

FROM node:22-alpine AS build
WORKDIR /src

# Copied before the rest of the source so a change to application code doesn't invalidate the
# dependency layer. npm ci (not install) so the lockfile is authoritative — a deployed image should
# never resolve a different dependency tree than CI tested.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# Vite inlines VITE_* at build time, so anything environment-specific has to arrive here rather
# than at runtime. Deliberately only ONE such value: VITE_GATEWAY_BASE_URL is left unset, which
# makes axios issue same-origin relative requests (`/api/...`). The SPA and the gateway sit behind
# one ingress, so that resolves correctly, needs no CORS, and — the real win — makes this image
# identical for every environment rather than one build per hostname.
#
# A Stripe publishable key is designed to be public (it identifies the account and can only create
# tokens, never move money), so baking one in is safe. Unset builds still work: checkout falls back
# to a canned test payment method.
ARG VITE_STRIPE_PUBLISHABLE_KEY=""
ENV VITE_STRIPE_PUBLISHABLE_KEY=${VITE_STRIPE_PUBLISHABLE_KEY}

RUN npm run build

FROM nginx:1.27-alpine AS final

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist /usr/share/nginx/html

# 8080, matching every other pod in this platform, rather than nginx's default 80.
EXPOSE 8080
