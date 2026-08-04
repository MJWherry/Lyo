# Lyo Portfolio (React / Next.js)

Showcase site for the Lyo monorepo: feature explainers, full `lyo.bench/v1` viewer (all suites including k6), package cards by taxonomy, and live demos (Query where-clause builder).

## Nav

Home · Features · Benchmarks · Demos · About

## Architecture

- **Browser** → Next.js (public) only
- **Next.js Route Handlers** → `Lyo.Portfolio.Api` over `LYO_API_BASE_URL` (private)
- Auth: Lyo JWT + Google OIDC (configure Google Cloud OAuth; canonical site URL is lyo)
- TypeScript clients: `lyo-api-client` + `lyo-person-api-client` (async / fetch)
- Theme: purple / gray / white with light default + dark invert (`lyo-theme` in localStorage)

.NET backend host: `Lyo.Net/Apps/Portfolio/Lyo.Portfolio.Api` (separate from kitchen-sink `Lyo.TestApi`). A portfolio Gateway (Test Gateway-style) is planned later.

## Featured home benchmarks

Docs select *which* measurement/scenario to feature; the portfolio resolves
metric/detail from `docs/benchmarks/data/{suite}.json` at render time. Do **not**
hardcode timings or allocation in `docs.json`.

```json
{
  "suite": "csv",
  "items": [
    {
      "label": "CSV UTF-8 export",
      "href": "/benchmarks/csv",
      "featured": true,
      "method": "Utf8_Export",
      "params": { "RowCount": "100000" },
      "primary": "mean"
    }
  ]
}
```

Load (k6) suites use `scenario` instead of `method`/`params`:

```json
{
  "suite": "query-api",
  "items": [
    {
      "label": "QueryConcrete load",
      "href": "/benchmarks/query-api",
      "featured": true,
      "scenario": "query_load",
      "primary": "p95"
    }
  ]
}
```

Run `npm run sync-docs` after editing docs. Schema: `docs/catalog/schema/package.schema.json`.

## Local development

```bash
# Terminal 1 — build TS packages (once), in order:
cd packages/typescript/lyo-api-client && npm i && npm run build
cd ../lyo-query && npm i && npm run build
cd ../lyo-query-components && npm i && npm run build
cd ../lyo-person-api-client && npm i && npm run build

# Terminal 2 — Portfolio.Api + Postgres (or use deploy/portfolio/api compose)

# Terminal 3 — this app
cd apps/portfolio
cp .env.example .env.local
npm install
npm run sync-benchmarks   # optional: copy docs/benchmarks/history if present
npm run dev
```

Open http://localhost:3100

### Benchmark history

```bash
npm run sync-benchmarks    # → public/benchmarks/history (gitignored)
```

## Deploy

See `deploy/portfolio/` and `infra/aws/portfolio/`. CI (manual): **Docker - Build Portfolio** + **Deploy - Portfolio**.
