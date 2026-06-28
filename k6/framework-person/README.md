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

Archived k6 outputs live under `k6/framework-person/results/<timestamp>/` (JSON summaries + logs). `build-manifests.py` normalizes the raw `*.summary.json` into the unified `lyo.bench/v1` schema (`type: "load"` — cases / scenarios / rollups / SLO / grades; see [`Lyo.Benchmark.Models`](../../Lyo.Net/Core/Benchmark/Lyo.Benchmark.Models/README.md)). It also attaches a per-case `cases` block (query structure: where clauses, filters, sort, includes, selection field count) from the `K6_CASE_META` map in `build-manifests.py` — keep that map in sync with [`lib/cases.js`](lib/cases.js) / [`lib/queryFactory.js`](lib/queryFactory.js) when cases change, so the dashboard explains what each scenario actually tested. The **authoritative review** is the HTML dashboard:

- **[Benchmark dashboard](../../docs/benchmarks/index.html)** — open after `build-manifests.py`, then click the **Query API (k6)** report
- Stub / refresh notes: [`K6_BENCHMARK_ANALYSIS.md`](../../Lyo.Net/Integration/Api/Lyo.Api/K6_BENCHMARK_ANALYSIS.md)

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

# Run specific scenarios by keyword (substring match)
./k6/framework-person/run_all.sh query_spike queryproject_load

# Run profile groups across both endpoints (matrix-style)
./k6/framework-person/run_all.sh spike
./k6/framework-person/run_all.sh load
./k6/framework-person/run_all.sh stress

# Combine groups/keywords
./k6/framework-person/run_all.sh query spike
./k6/framework-person/run_all.sh nonsoak

# Equivalent via env var (comma-separated)
TEST_FILTER="query_spike,queryproject_load" ./k6/framework-person/run_all.sh
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
  - `MATRIX_CASES` (`all` or comma-separated case ids; applies to matrix suites). Default is `baseline,filter_sort,complex_querynode,query_with_subquery,realistic_include` for `query` load, otherwise `all`.
  - `MATRIX_AMOUNT_MIN`, `MATRIX_AMOUNT_MAX`, `MATRIX_START_MAX` (global matrix overrides)
  - Fairness default: both `/person/query` and `/person/QueryProject` use the same matrix pagination range unless you explicitly override endpoint-specific values.
  - `MATRIX_SLEEP_SECONDS` (global matrix iteration sleep override)
  - `RANDOMIZE_CASE_SELECTION` (`true|false`, default `true`) weighted random case selection instead of strict round-robin
  - `RANDOM_SEED` (integer, default `20260623`) deterministic replay seed for case/shape/sort generation
  - `CASE_WEIGHT_<CASE_ID>` (e.g. `CASE_WEIGHT_PROJECTION_NESTED=2.0`) set case weights
  - `CASE_WEIGHT_<ENDPOINT>_<PROFILE>_<CASE_ID>` (most specific override; e.g. `CASE_WEIGHT_QUERYPROJECT_SPIKE_PROJECTION_UNIFIED=3.0`)
- Query behavior:
  - `TOTAL_COUNT_MODE` (`None`, `HasMore`, `Exact`) — default `None`. `HasMore` is the in-between: fetches one extra row to detect more pages (no `COUNT`). `Exact` runs an extra `COUNT(*)` and can roughly double query time.
  - `INCLUDE_FILTER_MODE` (`Full`, `MatchedOnly`)
  - `INCLUDES` (comma separated include paths)
  - `SELECT_FIELDS` (comma separated projection field paths for `QueryProject`; use `SourceEntityType`, not `Source`)
  - `SOURCE_FILTER_VALUES` (comma-separated `SourceEntityType` values for filter scenarios; default both Endato PS + CE entity type names)
  - `AMOUNT`, `START`
  - `RANDOMIZE_INCLUDES` / `QUERY_RANDOMIZE_INCLUDES` (`true|false`, defaults `true`) for query include branch randomization
  - `QUERY_INCLUDE_ADDRESS_RATE`, `QUERY_INCLUDE_PHONE_RATE`, `QUERY_INCLUDE_EMAIL_RATE` (branch probabilities; defaults `0.75/0.35/0.30`)
  - `RANDOMIZE_PROJECTION_FIELDS` / `QUERYPROJECT_RANDOMIZE_PROJECTION_FIELDS` (`true|false`, defaults `true`) for per-request `Select` generation
  - `PROJECTION_FIELD_MIN`, `PROJECTION_FIELD_MAX` (default `2..6` projected fields)
  - `PROJECTION_ROOT_POOL`, `PROJECTION_ADDRESS_POOL`, `PROJECTION_PHONE_POOL`, `PROJECTION_EMAIL_POOL` (CSV field pools)
  - `QUERYPROJECT_ADDRESS_RATE`, `QUERYPROJECT_PHONE_RATE`, `QUERYPROJECT_EMAIL_RATE` (projection nav-branch probabilities; defaults align with query include rates)
  - `RANDOMIZE_SORTS`, `QUERY_RANDOMIZE_SORTS`, `QUERYPROJECT_RANDOMIZE_SORTS` (`true|false`, defaults `true`)
  - `QUERY_SORT_FIELDS`, `QUERYPROJECT_SORT_FIELDS` (CSV sort candidate fields)
  - `SORT_KEYCOUNT_WEIGHTS` or endpoint-specific variants (`QUERY_SORT_KEYCOUNT_WEIGHTS`, `QUERYPROJECT_SORT_KEYCOUNT_WEIGHTS`) using `count:weight` CSV, e.g. `0:0.1,1:0.4,2:0.4,3:0.1`
  - `SORT_DESC_RATE` / endpoint-specific variants (default `0.45`)
  - `SORT_MIXED_DIRECTION_RATE` / endpoint-specific variants (default `0.35`)
- Profile tuning:
  - Load: `LOAD_RATE`, `LOAD_DURATION`, `LOAD_PREALLOCATED_VUS`, `LOAD_MAX_VUS` (defaults: `7`, `3m`, `6`, `12`)
  - Stress: `STRESS_START_VUS`, `STRESS_TARGET1`, `STRESS_TARGET2`, stage durations
  - Spike: `SPIKE_START_RATE`, `SPIKE_TARGET_RATE`, `SPIKE_MAX_VUS`
  - Soak: `SOAK_VUS`, `SOAK_DURATION`, `SOAK_HEAVY_EVERY`
- Heavy include:
  - `BYPASS_CACHE=true|false`
  - `HEAVY_AMOUNT`, `HEAVY_MIN_AMOUNT`, `HEAVY_MAX_AMOUNT` (defaults: `200`, `150`, `300`)

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
- `QueryProject` keeps `Include` empty by design in these workloads; navigation loading is derived from `Select` (and where-clause collection paths) by the API.
- Query and QueryProject generators now share seeded randomization and probability controls so cross-endpoint comparisons can use matched shape distributions (nav branches, sort count, sort direction mix).
- Matrix suites emit per-case tagged success metrics: `status_success_rate`, `latency_success_rate`, `shape_success_rate`, and per-case `query_duration`.
- Scenarios that send `Select` post to `/person/QueryProject` via shared person client routing.
- Legacy mixed scenarios still exist for migration/backward compatibility, but production execution should use dedicated matrix suites.
