# k6 Framework: Person Query API

Reusable k6 framework for `TestApi` person querying with multiple workload profiles and query shapes based on your `QueryRequest` model.

Shared reusable API code lives outside k6 in:
- `packages/typescript/lyo-api-client`
- `packages/typescript/lyo-person-api-client`

`k6/framework-person` contains test orchestration only.

## Production Matrix

Four axes (cartesian product = **90 cells** by default):

| Axis | Values |
|------|--------|
| **Endpoint** | `query` (`/person/QueryConcrete`), `queryproject` (`/person/QueryProject`), `queryroot` (`POST /Query`) |
| **Profile** | `load`, `stress`, `spike`, `soak`, `ceiling` |
| **Intensity** | `low`, `med`, `high` |
| **Cache** | `uncached` (varied shapes), `cached` (`CACHE_HIT_MODE` pins shapes) |

Every cell pins **`RANDOM_SEED=20260623`**. Results are named `{endpoint}_{profile}_{intensity}_{cached|uncached}.summary.json`.

Still **15 scenario stubs** (endpoint × profile only); intensity + cache are env-driven. `run_all.py` expands the product via `MatrixPlanner`.

### Intensity presets (SoT: `lib/intensityPresets.js`)

| Profile | low | med | high |
|---------|-----|-----|------|
| **load** | 7/s, 3m, maxVU 12 | 25/s, 5m, maxVU 40 | 40/s, 5m, maxVU 60 |
| **stress** | 5→15→25 VU | 10→30→50 VU | 15→45→75 VU |
| **spike** | 5→40→10/s, maxVU 60 | 15→100→25/s, maxVU 120 | 25→150→40/s, maxVU 180 |
| **soak** | 5 VU / 30m | 15 VU / 1h | 25 VU / 2h |
| **ceiling** | 10…100 | 25…300 | 25…1000 (full ladder) |

### Flow table (× 3 endpoints = 90)

For each of `query`, `queryproject`, `queryroot`:

| Profile | Intensity | uncached | cached |
|---------|-----------|----------|--------|
| load / stress / spike / soak / ceiling | low \| med \| high | cell | cell |

Day-run tip: `python3 k6/framework-person/run_all.py nonsoak med uncached` (12 cells). Full product is long — soak dominates.

### OOP layout (reusable composition)

```
scenario stub → ScenarioFactory.create(MatrixCell)
                  ├─ IntensityPresets → ProfileOptionsBuilder
                  ├─ CacheModePolicy → paging / shape RNG
                  ├─ CaseCatalog (cases.js)
                  └─ CaseRunner (client.js)

run_all.py → MatrixPlanner → K6ProcessRunner(cell.to_env())
```

JS: `lib/matrixCell.js`, `intensityPresets.js`, `cacheModePolicy.js`, `scenarioFactory.js`, `profiles.js` (`ProfileOptionsBuilder`).  
Python: `matrix/axes.py`, `cell.py`, `planner.py`, `runner.py`.

## Benchmarks & “modern standards”

Archived k6 outputs live under `k6/framework-person/results/<timestamp>/` (JSON summaries + logs). `build_manifests.py` normalizes the raw `*.summary.json` into the unified `lyo.bench/v1` schema (`type: "load"` — cases / scenarios / rollups / SLO / grades; see [`Lyo.Benchmark.Models`](../../Lyo.Net/Core/Benchmark/Lyo.Benchmark.Models/README.md)). It also attaches a per-case `cases` block (query structure: where clauses, filters, sort, includes, selection field count) from the `K6_CASE_META` map in `build_manifests.py` — keep that map in sync with [`lib/cases.js`](lib/cases.js) / [`lib/queryFactory.js`](lib/queryFactory.js) when cases change, so the dashboard explains what each scenario actually tested. The **authoritative review** is the HTML dashboard:

- **[Benchmark dashboard](../../docs/benchmarks/index.html)** — open after `build_manifests.py`, then click the **Query API (k6)** report
- Stub / refresh notes: [`K6_BENCHMARK_ANALYSIS.md`](../../Lyo.Net/Integration/Api/Lyo.Api/K6_BENCHMARK_ANALYSIS.md)

**Latest full suite analyzed there (July 2026, 12-suite matrix incl. root `/Query`)**: `k6/framework-person/results/20260726-235847/` (the June 2026 `prod-like-20260624-234715` 8-suite archive and earlier April 2026 5-scenario archives are retained for historical comparison).

At a high level (local laptop, API + Postgres + k6 colocated — pessimistic vs split infra). **`Lyo.TestApi`** uses **`CacheOptions:QueryCacheTagGranularity`** = **`Broad`** (default) for these numbers:

| Bucket | What to expect in the wild | This stack (see analysis) |
|--------|----------------------------|---------------------------|
| Small/medium JSON reads, filters, sorts, projections | Public APIs often target **p95 ~100–300 ms**; internal microservices often **~50–150 ms** | Baseline/filter/subquery cases **~14–22 ms p95** under load; projections **~6–51 ms p95**; root joins **~5–46 ms p95** on the July 2026 run |
| Thin DB→JSON gateways (PostgREST, Hasura) | Often **~5–30 ms** p95 for simple reads on small data | Competitive **order of magnitude** for comparable shapes and sizes (root flat select **~5 ms p95**, scalar computed projection **~6 ms p95**) |
| GraphQL (Hasura vs hand-written resolvers) | Hasura: similar to thin gateways; Apollo/Hot Chocolate/gqlgen: **~40 ms–seconds** depending on N+1 and DataLoader | Analysis separates **gateway GraphQL** from **resolver-heavy GraphQL**; see industry tables |
| ORM-heavy APIs (typical EF/Django/Rails) | Often **50–400 ms** for non-trivial reads | **Lower** than “typical ORM” bands in the analysis for the scenarios tested |
| Large payloads / deep graphs | Dominated by **bytes and join depth** — compare after pagination, not a single global SLO | Heavy-include **~96–119 ms avg** at steady state (spike/soak); **~659 ms avg** under 40-VU stress with **99.98%** checks |

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
  - `ceiling` (saturation: staggered constant-arrival-rate steps that climb until the server can no longer keep up; zero iteration sleep, no pass/fail thresholds — each step is its own k6 scenario so the summary carries per-step p95/dropped-iterations and the dashboard reports the highest dropped-free rate as the measured ceiling)
- Query shapes:
  - baseline pagination
  - filter groups + multi-sort
  - select-field projection (`Select` via `/person/QueryProject`)
  - complex `QueryNode` tree
  - `QueryNode` + `SubQuery` (two-phase style)
  - heavy includes (cache-bypass or cache-hit mode)
  - QueryProject projection and computed fields (scenarios 06–07)
  - root `POST /Query` From/Joins shapes (flat, left join, chained joins, chained + exact count)

## Directory layout

- `lib/`
  - `matrixAxes.js` / `matrixCell.js` — cell identity (endpoint × profile × intensity × cache × seed)
  - `intensityPresets.js` — low/med/high numeric tables (SoT)
  - `cacheModePolicy.js` — cached vs uncached paging / RNG policy
  - `scenarioFactory.js` — composes cell → runnable k6 scenario
  - `profiles.js` — `ProfileOptionsBuilder` (presets → k6 options)
  - `env.js` env parsing helpers
  - `client.js` HTTP execution + validation checks/metrics
  - `metrics.js` custom k6 metrics
  - `config.js` matrix config schema and selectors
  - `cases.js` endpoint-aware query case registry
  - `k6Transport.js` transport adapter for shared TS API client
  - `matrixRunner.js` thin legacy wrapper → `ScenarioFactory`
  - `workloadShape.js` seeded RNG / sort helpers
  - `personModels.js` shared field names and source-type constants
  - `queryFactory.js` `QueryConcreteReq` body builders (`/person/QueryConcrete`)
  - `projectionQueries.js` `ProjectionQueryReq` body builders (`/person/QueryProject`)
  - `rootQueries.js` root query body builders (`POST /Query` — From/Joins sparse projection)
- `matrix/` (Python)
  - `axes.py` / `cell.py` / `planner.py` / `runner.py` — cartesian expansion + k6 process invoke
- `scenarios/`
  - `query_load.js`
  - `query_stress.js`
  - `query_spike.js`
  - `query_soak.js`
  - `queryproject_load.js`
  - `queryproject_stress.js`
  - `queryproject_spike.js`
  - `queryproject_soak.js`
  - `queryroot_load.js`
  - `queryroot_stress.js`
  - `queryroot_spike.js`
  - `queryroot_soak.js`
  - `query_ceiling.js`
  - `queryproject_ceiling.js`
  - `queryroot_ceiling.js`
  - (legacy scenarios retained for migration compatibility)
- `run_all.py` strict matrix runner with package-build preflight (see also [TOOLING.md](TOOLING.md))

## Quick start

Build shared TypeScript packages first:

```bash
cd packages/typescript/lyo-query && npm install && npm run build
cd ../lyo-api-client && npm install && npm run build
cd ../lyo-person-api-client && npm install && npm run build
cd ../../
# k6 cannot resolve bare "lyo-query" imports — run_all.py rewrites them after build.
```

Then run a scenario:

```bash
k6 run -e BASE_URL="http://localhost:5251" -e ENDPOINT_PATH="/person/QueryConcrete" \
  k6/framework-person/scenarios/01_load_mixed_queries.js
```

Run the matrix (default: all endpoints × profiles × intensities × both cache modes = 90 cells):

```bash
python3 k6/framework-person/run_all.py

# Axis filters (AND across axes; OR within an axis)
python3 k6/framework-person/run_all.py query load med          # 2 cells (cached + uncached)
python3 k6/framework-person/run_all.py spike high uncached     # 3 endpoints × spike × high × uncached
python3 k6/framework-person/run_all.py nonsoak med uncached    # day-run slice (no soak)

# Saturation
python3 k6/framework-person/run_all.py ceiling med

# Equivalent via env var (comma-separated keywords)
TEST_FILTER="query,load,med,uncached" python3 k6/framework-person/run_all.py
```

Smoke matrix mode (1 VU / 1 iteration per cell):

```bash
MODE=smoke python3 k6/framework-person/run_all.py query load med
```

## Useful env vars

- Core:
  - `BASE_URL` (default `http://localhost:5251`)
  - `ENDPOINT_PATH` (default `/person/QueryConcrete`) — full entity queries only
  - `QUERY_PROJECT_PATH` (default `/person/QueryProject`) — projection scenarios (01 case 3, 03, 04 case 2, 06, 07)
  - `TOKEN` (optional bearer token)
  - `SLEEP_SECONDS`
- Matrix control:
  - `MODE` (`full` or `smoke`) in `run_all.py`
  - `RUN_LABEL` (prefixes the results directory name; shows up in the dashboard snapshot dropdown)
  - `INTENSITY` (`low|med|high`, default `med` when running a stub directly; `run_all.py` expands all three unless filtered)
  - `CACHE_MODE` (`uncached|cached`) preferred cache axis; `CACHE_HIT_MODE` (`true|false`) legacy equivalent
  - Cached mode pins request shapes (fixed `Start=0`/`Amount=amountMin`, round-robin cases, no include/Select/sort randomization) so each case hits one query-cache key
  - `RANDOM_SEED` (integer, default `20260623`) — always pinned by `run_all.py` for every cell
  - `MATRIX_CASES` (`all` or comma-separated case ids; applies to matrix suites). Default is `baseline,filter_sort,complex_querynode,query_with_subquery,realistic_include` for `query` load, otherwise `all`.
  - `MATRIX_AMOUNT_MIN`, `MATRIX_AMOUNT_MAX`, `MATRIX_START_MAX` (global matrix overrides)
  - Fairness default: both `/person/QueryConcrete` and `/person/QueryProject` use the same matrix pagination range unless you explicitly override endpoint-specific values.
  - `MATRIX_SLEEP_SECONDS` (global matrix iteration sleep override)
  - `RANDOMIZE_CASE_SELECTION` (`true|false`, default `true`) weighted random case selection instead of strict round-robin (forced off in cached mode)
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
- Profile tuning (defaults come from `INTENSITY` presets in `lib/intensityPresets.js`; env overrides still win):
  - Load: `LOAD_RATE`, `LOAD_DURATION`, `LOAD_PREALLOCATED_VUS`, `LOAD_MAX_VUS`
  - Stress: `STRESS_START_VUS`, `STRESS_TARGET1`, `STRESS_TARGET2`, stage durations
  - Spike: `SPIKE_START_RATE`, `SPIKE_TARGET_RATE`, `SPIKE_RECOVER_RATE`, `SPIKE_PREALLOCATED_VUS`, `SPIKE_MAX_VUS`
  - Soak: `SOAK_VUS`, `SOAK_DURATION`, `SOAK_HEAVY_EVERY`
  - Ceiling: `CEILING_RATES`, `CEILING_STEP_DURATION`, `CEILING_MAX_VUS`, `CEILING_GRACEFUL_STOP`
- Heavy include:
  - `BYPASS_CACHE=true|false`
  - `HEAVY_AMOUNT`, `HEAVY_MIN_AMOUNT`, `HEAVY_MAX_AMOUNT` (defaults: `200`, `150`, `300`)

## Example runs

Heavy include stress, cache bypass:

```bash
k6 run \
  -e BASE_URL="http://localhost:5251" \
  -e ENDPOINT_PATH="/person/QueryConcrete" \
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
  -e ENDPOINT_PATH="/person/QueryConcrete" \
  -e SOAK_DURATION="1h" \
  -e SOAK_VUS=12 \
  k6/framework-person/scenarios/04_soak_mixed_leak_watch.js
```

## Notes

- Query paths use entity property names (e.g. `SourceEntityType`). Values are full `EntityRef` type names such as `Lyo.Endato.Postgres.Database.EndatoPsPersonEntity` (Person Search) and `Lyo.Endato.Postgres.Database.EndatoCePersonEntity` (Contact Enrichment). JSON responses from `/person/QueryConcrete` expose the mapped field as `source` on `PersonRes`.
- `QueryProject` keeps `Include` empty by design in these workloads; navigation loading is derived from `Select` (and where-clause collection paths) by the API.
- Query and QueryProject generators now share seeded randomization and probability controls so cross-endpoint comparisons can use matched shape distributions (nav branches, sort count, sort direction mix).
- Matrix suites emit per-case tagged success metrics: `status_success_rate`, `latency_success_rate`, `shape_success_rate`, and per-case `query_duration`.
- Scenarios that send `Select` post to `/person/QueryProject` via shared person client routing.
- Legacy mixed scenarios still exist for migration/backward compatibility, but production execution should use dedicated matrix suites.
