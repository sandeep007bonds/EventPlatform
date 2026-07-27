# Frontend

The buyer + organizer web app — a single React SPA with two Ant
Design–themed sections, talking only to the [gateway](../gateways/EventPlatform.Gateway).

## Stack

Vite + React + TypeScript · Ant Design (theming, components) ·
react-router-dom · axios · react-i18next.

## Run

```bash
cp .env.example .env.development.local   # once — set VITE_GATEWAY_BASE_URL
npm install
npm run dev
```

Open http://localhost:5173. Needs the gateway running to log in or fetch
anything — see [docs/local-e2e-walkthrough.md](../docs/local-e2e-walkthrough.md).

## Scripts

| Script                            | What it does                                |
| --------------------------------- | ------------------------------------------- |
| `npm run dev`                     | Vite dev server with HMR                    |
| `npm run build`                   | Type-check (`tsc -b`) then production build |
| `npm run lint`                    | ESLint                                      |
| `npm run format` / `format:check` | Prettier write / check                      |
| `npm run typecheck`               | `tsc -b`, no emit                           |
| `npm run preview`                 | Serve the production build locally          |

See [CLAUDE.md](CLAUDE.md) for structure and design notes, and
[ADR-0015](../docs/adr/0015-frontend-react-vite-antd-and-bff-gateway.md) for
why this stack was chosen.
