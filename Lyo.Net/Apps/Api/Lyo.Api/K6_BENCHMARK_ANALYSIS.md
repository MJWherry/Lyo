# K6 Benchmark Summary — Lyo Query API

The interactive review lives in the HTML dashboard (auto-loaded from latest k6 summary JSON):

- **[Query API (k6) report](../../../../docs/benchmarks/report.html#query-api)**
- Hub: [`docs/benchmarks/index.html`](../../../../docs/benchmarks/index.html)

Latest archived run: **`20260726-235847`** (July 2026) — full 12-suite matrix (QueryConcrete / QueryProject / root `/Query` × load/stress/spike/soak), ~1.35M requests, 100%
status/shape checks. Use the report's **Snapshot** dropdown to compare against earlier archives (e.g. June 2026 `prod-like-20260624-234715`).

## Refresh after a k6 run

```bash
# Automatic when using the matrix runner:
k6/framework-person/run_all.py

# Or manually:
python3 scripts/benchmarks/build_manifests.py --k6-only
```

Generated data: `docs/benchmarks/data/query-api.js` (+ JSON), with per-run snapshots under `docs/benchmarks/history/query-api/`. Open `docs/benchmarks/report.html#query-api` in a
browser after refresh.

Raw k6 artifacts: `k6/framework-person/results/`.

## Notes

- Single-instance setup (API + PostgreSQL + Redis + k6 on one machine).
- The harness bypasses the server query cache by default (randomized `Start`/`Amount` and include/Select shapes produce a unique cache key per request). Set `CACHE_HIT_MODE=true`
  to pin request shapes and benchmark the cache-hit path instead.
- The benchmark DB (schema `people`) carries the model's declared indexes: name columns (`last_name`, `first_name`, `(last_name, first_name)`), junction FK columns, and — since the
  July 2026 `AddPersonQueryIndexes` migration — `source_entity_type`, `(last_name, first_name, id)`, and `(first_name, last_name, date_of_birth)`. An earlier note here claiming "no
  indexes on queried/sorted columns" was stale.
- Workload defaults: randomized sorting disabled except the `filter_sort` case.
- `ceiling` suites (saturation) measure the max sustained arrival rate per endpoint family; the dashboard reports the highest dropped-iteration-free step and its p95.
