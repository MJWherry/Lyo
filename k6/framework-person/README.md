# k6 Framework: Person Query API

Reusable k6 framework for `TestApi` person querying with multiple workload profiles and query shapes based on your `QueryRequest` model.

## What this covers

- Workload profiles:
  - `load` (constant arrival rate)
  - `stress` (ramping VUs)
  - `spike` (ramping arrival rate burst)
  - `soak` (long-running leak watch)
- Query shapes:
  - baseline pagination
  - filter groups + multi-sort
  - select-field projection (`Select`)
  - complex `QueryNode` tree
  - `QueryNode` + `SubQuery` (two-phase style)
  - heavy includes (cache-bypass or cache-hit mode)

## Directory layout

- `lib/`
  - `env.js` env parsing helpers
  - `client.js` HTTP query request helper + checks
  - `metrics.js` custom k6 metrics
  - `profiles.js` workload profile option builders
  - `queryFactory.js` `QueryRequest` body builders
- `scenarios/`
  - `01_load_mixed_queries.js`
  - `02_stress_heavy_includes.js`
  - `03_spike_select_fields.js`
  - `04_soak_mixed_leak_watch.js`
  - `05_load_query_subquery.js`
- `run_all.sh` run all scenario files

## Quick start

```bash
k6 run -e BASE_URL="http://localhost:5251" -e ENDPOINT_PATH="/person/query" \
  k6/framework-person/scenarios/01_load_mixed_queries.js
```

Run everything:

```bash
./k6/framework-person/run_all.sh
```

## Useful env vars

- Core:
  - `BASE_URL` (default `http://localhost:5251`)
  - `ENDPOINT_PATH` (default `/person/query`)
  - `TOKEN` (optional bearer token)
  - `SLEEP_SECONDS`
- Query behavior:
  - `TOTAL_COUNT_MODE` (`None`, `HasMore`, `Exact`) — default `None`. `HasMore` is the in-between: fetches one extra row to detect more pages (no `COUNT`). `Exact` runs an extra `COUNT(*)` and can roughly double query time.
  - `INCLUDE_FILTER_MODE` (`Full`, `MatchedOnly`)
  - `INCLUDES` (comma separated include paths)
  - `SELECT_FIELDS` (comma separated projection field paths)
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
  -e ENDPOINT_PATH="/person/query" \
  -e SELECT_FIELDS="Id,FirstName,LastName,Source" \
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

- `04_soak_mixed_leak_watch.js` is designed for long-duration trend drift detection; pair it with API/container memory graphs (OTel/Prometheus) for leak confirmation.
- `02_stress_heavy_includes.js` intentionally pushes heavy include pagination patterns (including multi-hop includes).
