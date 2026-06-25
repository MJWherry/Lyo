# K6 Benchmark Summary — Lyo Query API

The interactive review lives in the HTML dashboard (auto-loaded from latest k6 summary JSON):

- **[K6 benchmark dashboard](../../docs/benchmarks/k6.html)**
- Hub: [`docs/benchmarks/index.html`](../../docs/benchmarks/index.html)

## Refresh after a k6 run

```bash
# Automatic when using the matrix runner:
k6/framework-person/run_all.sh

# Or manually:
python3 scripts/benchmarks/build-manifests.py --k6-only
```

Generated data: `docs/benchmarks/data/k6-latest.js` (+ JSON). Open `docs/benchmarks/k6.html` in a browser after refresh.

Raw k6 artifacts: `k6/framework-person/results/`.

## Notes

- Single-instance setup (API + PostgreSQL + Redis + k6 on one machine).
- Harness bypasses caching; benchmark DB has no indexes on queried/sorted columns.
- Workload defaults: randomized sorting disabled except the `filter_sort` case.
