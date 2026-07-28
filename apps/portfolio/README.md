# Lyo Portfolio (React / Next.js)

Showcase site for the Lyo monorepo: feature explainers, full `lyo.bench/v1` viewer (all suites including k6), package cards by taxonomy, and live demos (Query where-clause builder).

## Nav

Home · Features · Benchmarks · Demos

## Architecture

- **Browser** → Next.js (public) only
- **Next.js Route Handlers** → `Lyo.TestApi` over `LYO_API_BASE_URL` (private)
- TypeScript clients: `lyo-api-client` + `lyo-person-api-client` (async / fetch)
- Theme: purple / gray / white with light default + dark invert (`lyo-theme` in localStorage)

Blazor WASM Portfolio under `Lyo.Net/Apps/Portfolio` is unchanged.

## Local development

```bash
# Terminal 1 — build TS packages (once), in order:
cd packages/typescript/lyo-api-client && npm i && npm run build
cd ../lyo-query && npm i && npm run build
cd ../lyo-query-react && npm i && npm run build
cd ../lyo-person-api-client && npm i && npm run build

# Terminal 2 — TestApi + Postgres (or use deploy/portfolio/api compose)

# Terminal 3 — this app
cd apps/portfolio
cp .env.example .env.local
npm install
npm run sync-benchmarks   # optional: copy docs/benchmarks/history if present
npm run dev
```

Open http://localhost:3100

### Package sizes / benchmark history

```bash
npm run measure-sizes      # → content/package-sizes.json
npm run sync-benchmarks    # → public/benchmarks/history (gitignored)
```

## Deploy

See `deploy/portfolio/` and `infra/aws/portfolio/`. GitLab CI: root `.gitlab-ci.yml`.
