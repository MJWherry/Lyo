# k6 Framework: Person Query API

Reusable k6 framework for `TestApi` person querying with multiple workload profiles and query shapes based on your `QueryRequest` model.

Shared reusable API code lives outside k6 in:
- `packages/lyo-api-client`
- `packages/lyo-person-api-client`

`k6/framework-person` contains test orchestration only.

## Production Matrix

The primary production path is now a **symmetric matrix**:

- Endpoints: `/person/query`, `/person/QueryProject`
- Profiles: `load`, `stress`, `spike`, `soak`
- Dedicated suites: 8 scenario files (one per endpoint x profile)

Core matrix orchestration is data-driven via:

- `lib/config.js` (matrix/env schema)
- `lib/cases.js` (case registry)
- `lib/k6Transport.js` (k6 adapter over shared TS clients)
- `lib/matrixRunner.js` (shared per-iteration execution)

## Benchmarks & “modern standards”

Archived k6 outputs live under `k6/framework-person/results/<timestamp>/` (JSON summaries + logs). The **authoritative write-up** — per-scenario metrics, environment, grades, and **comparison to common stacks** (Hasura/PostgREST, typical EF/Django/Rails/Spring, GraphQL) — is:

- [`Lyo.Net/Integration/Api/Lyo.Api/K6_BENCHMARK_ANALYSIS.md`](../../Lyo.Net/Integration/Api/Lyo.Api/K6_BENCHMARK_ANALYSIS.md)

**Latest full suite analyzed there (June 2026, symmetric matrix)**: `k6/results/prod-matrix-20260623-163003/` (earlier April 2026 5-scenario archives are retained in the analysis doc for historical comparison).

At a high level (local laptop, API + Postgres + k6 colocated — pessimistic vs split infra). **`Lyo.TestApi`** uses **`CacheOptions:QueryCacheTagGranularity`** = **`Broad`** (default) for these numbers:

| Bucket | What to expect in the wild | This stack (see analysis) |
|--------|----------------------------|---------------------------|
| Small/medium JSON reads, filters, sorts, projections | Public APIs often target **p95 ~100–300 ms**; internal microservices often **~50–150 ms** | Mixed query **~29 ms p95**; subquery load **~18 ms p95** on the archived run |
| Thin DB→JSON gateways (PostgREST, Hasura) | Often **~5–30 ms** p95 for simple reads on small data | Competitive **order of magnitude** for comparable shapes and sizes |
| GraphQL (Hasura vs hand-written resolvers) | Hasura: similar to thin gateways; Apollo/Hot Chocolate/gqlgen: **~40 ms–seconds** depending on N+1 and DataLoader | Analysis separates **gateway GraphQL** from **resolver-heavy GraphQL**; see industry tables |
| ORM-heavy APIs (typical EF/Django/Rails) | Often **50–400 ms** for non-trivial reads | **Lower** than “typical ORM” bands in the analysis for the scenarios tested |
| Large payloads / deep graphs | Dominated by **bytes and join depth** — compare after pagination, not a single global SLO | Heavy-include stress **~93 ms avg** with **100%** within scenario SLA under 40 VUs |

Treat any table as **directional**: dataset size, indexes, cache keys, and hardware dominate absolute milliseconds.

**Dataset (current benchmark DB, approximate)**: `person` **~176k**, `address` **~1.1m**, `contact_address` **~1.1m**, `phone_number` **~631k**, `contact_phone_number` **~668k**,
`email_address` **~384k**, `contact_email_address` **~397k** — not a tiny seed database. Scenarios still use **bounded `Start`/`Amount`**; they do not load the full graph.

**TestApi host**: k6 runs against **`Lyo.TestApi`**, which wires **`ILyoMapper`** to **Mapster** (`MapsterLyoMapper`). Object mapping adds CPU and allocations compared to hand-written maps or another mapper; production APIs that skip or minimize mapping might see different end-to-end latency than these results.

## What this covers

- Workload profiles:
  - `load` (constant arrival rate)
  - `stress` (ramping VUs)
  - `spike` (ramping arrival rate burst)
  - `soak` (long-running leak watch)
- Query shapes:
  - baseline pagination
  - filter groups + multi-sort
  - select-field projection (`Select` via `/person/QueryProject`)
  - complex `QueryNode` tree
  - `QueryNode` + `SubQuery` (two-phase style)
  - heavy includes (cache-bypass or cache-hit mode)
  - QueryProject projection and computed fields (scenarios 06–07)

## Directory layout

- `lib/`
  - `env.js` env parsing helpers
  - `client.js` HTTP execution + validation checks/metrics
  - `metrics.js` custom k6 metrics
  - `profiles.js` workload profile option builders
  - `config.js` matrix config schema and selectors
  - `cases.js` endpoint-aware query case registry
  - `k6Transport.js` transport adapter for shared TS API client
  - `matrixRunner.js` shared execution flow for endpoint/profile suites
  - `personModels.js` shared field names and source-type constants
  - `queryFactory.js` `QueryReq` body builders (`/person/query`)
  - `projectionQueries.js` `ProjectionQueryReq` body builders (`/person/QueryProject`)
- `scenarios/`
  - `query_load.js`
  - `query_stress.js`
  - `query_spike.js`
  - `query_soak.js`
  - `queryproject_load.js`
  - `queryproject_stress.js`
  - `queryproject_spike.js`
  - `queryproject_soak.js`
  - (legacy scenarios retained for migration compatibility)
- `run_all.sh` strict matrix runner with package-build preflight

## Quick start

Build shared TypeScript packages first:

```bash
cd packages/lyo-api-client && npm install && npm run build
cd ../lyo-person-api-client && npm install && npm run build
cd ../../
```

Then run a scenario:

```bash
k6 run -e BASE_URL="http://localhost:5251" -e ENDPOINT_PATH="/person/query" \
  k6/framework-person/scenarios/01_load_mixed_queries.js
```

Run everything:

```bash
./k6/framework-person/run_all.sh
```

Smoke matrix mode:

```bash
MODE=smoke ./k6/framework-person/run_all.sh
```

## Useful env vars

- Core:
  - `BASE_URL` (default `http://localhost:5251`)
  - `ENDPOINT_PATH` (default `/person/query`) — full entity queries only
  - `QUERY_PROJECT_PATH` (default `/person/QueryProject`) — projection scenarios (01 case 3, 03, 04 case 2, 06, 07)
  - `TOKEN` (optional bearer token)
  - `SLEEP_SECONDS`
- Matrix control:
  - `MODE` (`full` or `smoke`) in `run_all.sh`
  - `MATRIX_CASES` (`all` or comma-separated case ids; applies to matrix suites)
  - `MATRIX_AMOUNT_MIN`, `MATRIX_AMOUNT_MAX`, `MATRIX_START_MAX` (global matrix overrides)
  - Fairness default: both `/person/query` and `/person/QueryProject` use the same matrix pagination range unless you explicitly override endpoint-specific values.
  - `MATRIX_SLEEP_SECONDS` (global matrix iteration sleep override)
- Query behavior:
  - `TOTAL_COUNT_MODE` (`None`, `HasMore`, `Exact`) — default `None`. `HasMore` is the in-between: fetches one extra row to detect more pages (no `COUNT`). `Exact` runs an extra `COUNT(*)` and can roughly double query time.
  - `INCLUDE_FILTER_MODE` (`Full`, `MatchedOnly`)
  - `INCLUDES` (comma separated include paths)
  - `SELECT_FIELDS` (comma separated projection field paths for `QueryProject`; use `SourceEntityType`, not `Source`)
  - `SOURCE_FILTER_VALUES` (comma-separated `SourceEntityType` values for filter scenarios; default both Endato PS + CE entity type names)
  - `AMOUNT`, `START`
- Profile tuning:
  - Load: `LOAD_RATE`, `LOAD_DURATION`, `LOAD_PREALLOCATED_VUS`, `LOAD_MAX_VUS`
  - Stress: `STRESS_START_VUS`, `STRESS_TARGET1`, `STRESS_TARGET2`, stage durations
  - Spike: `SPIKE_START_RATE`, `SPIKE_TARGET_RATE`, `SPIKE_MAX_VUS`
  - Soak: `SOAK_VUS`, `SOAK_DURATION`, `SOAK_HEAVY_EVERY`
- Heavy include:
  - `BYPASS_CACHE=true|false`
  - `HEAVY_AMOUNT`, `HEAVY_MIN_AMOUNT`, `HEAVY_MAX_AMOUNT`

## Example runs

Heavy include stress, cache bypass:

```bash
k6 run \
  -e BASE_URL="http://localhost:5251" \
  -e ENDPOINT_PATH="/person/query" \
  -e BYPASS_CACHE=true \
  -e STRESS_TARGET1=20 \
  -e STRESS_TARGET2=40 \
  k6/framework-person/scenarios/02_stress_heavy_includes.js
```

Spike test with projection only:

```bash
k6 run \
  -e BASE_URL="http://localhost:5251" \
  -e SELECT_FIELDS="Id,FirstName,LastName,SourceEntityType" \
  -e SPIKE_TARGET_RATE=100 \
  k6/framework-person/scenarios/03_spike_select_fields.js
```

Soak (leak watch) for 1 hour:

```bash
k6 run \
  -e BASE_URL="http://localhost:5251" \
  -e ENDPOINT_PATH="/person/query" \
  -e SOAK_DURATION="1h" \
  -e SOAK_VUS=12 \
  k6/framework-person/scenarios/04_soak_mixed_leak_watch.js
```

## Notes

- Query paths use entity property names (e.g. `SourceEntityType`). Values are full `EntityRef` type names such as `Lyo.Endato.Postgres.Database.EndatoPsPersonEntity` (Person Search) and `Lyo.Endato.Postgres.Database.EndatoCePersonEntity` (Contact Enrichment). JSON responses from `/person/query` expose the mapped field as `source` on `PersonRes`.
- Matrix suites emit per-case tagged success metrics: `status_success_rate`, `latency_success_rate`, `shape_success_rate`, and per-case `query_duration`.
- Scenarios that send `Select` post to `/person/QueryProject` via shared person client routing.
- Legacy mixed scenarios still exist for migration/backward compatibility, but production execution should use dedicated matrix suites.
