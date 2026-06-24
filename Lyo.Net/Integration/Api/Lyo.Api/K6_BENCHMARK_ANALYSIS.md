# K6 Benchmark Summary — Lyo Query API

## Overview

Production-matrix k6 review for the Lyo person query endpoints using the symmetric suite layout.

**Merged runs analyzed:** latest load/stress/spike from `k6/framework-person/results/prod-like-20260624-135401/` + soak baseline from `k6/results/prod-matrix-20260623-211829/` (Query) and `k6/results/prod-matrix-20260623-163003/` (QueryProject)  
**Date:** June 24, 2026  
**Run type:** Mixed single-instance snapshot (new 6-suite refresh + prior soak baselines)

---

## Environment (single-instance caveat)

These results are from a **single host** where API + PostgreSQL + Redis + k6 load generator all run on the same machine while development tools/processes (for example Rider) are active.

| Component | Context |
|---|---|
| API host | `dotnet` release build, local process |
| Database | PostgreSQL (local/docker) |
| Cache | Redis (local/docker) |
| Load generator | k6 (same machine) |
| Additional contention | IDE/background processes present |

> This setup is intentionally pessimistic for latency and throughput versus production multi-node deployments.

---

## Dataset Snapshot (current)

Approximate row counts used for this benchmark cycle:

| Table | Rows |
|---|---:|
| `person` | ~176k |
| `address` | ~1.1m |
| `contact_address` | ~1.1m |
| `phone_number` | ~631k |
| `contact_phone_number` | ~668k |
| `email_address` | ~384k |
| `contact_email_address` | ~397k |

---

## Benchmark Coverage (combined snapshot)

| Endpoint | Load | Stress | Spike | Soak |
|---|:---:|:---:|:---:|:---:|
| `/person/query` | ✅ | ✅ | ✅ | ✅ |
| `/person/QueryProject` | ✅ | ✅ | ✅ | ✅ |

Load/stress/spike source run: `prod-like-20260624-135401`; soak source runs remain `prod-matrix-20260623-211829` (Query) and `prod-matrix-20260623-163003` (QueryProject).

---

## Combined Results (query + QueryProject)

| Suite | Avg | p95 | p99 | Throughput | Requests | Checks pass | Dropped iterations | Source run |
|---|---:|---:|---:|---:|---:|---:|---:|
| `query_load` | 56.4 ms | 209.7 ms | 392.0 ms | 7.00 req/s | 1,260 | 100.00% | 0 | `20260624-135401` |
| `query_stress` | 1,765.0 ms | 7,023.7 ms | 10,473.6 ms | 14.44 req/s | 6,931 | 90.92% | 0 | `20260624-135401` |
| `query_spike` | 4,799.1 ms | 17,596.5 ms | 25,866.1 ms | 14.97 req/s | 1,881 | 85.66% | 4,868 | `20260624-135401` |
| `query_soak` | 44.4 ms | 166.5 ms | 251.9 ms | 46.69 req/s | 336,166 | 100.00% | 0 | `20260623-211829` |
| `queryproject_load` | 55.5 ms | 153.4 ms | 231.8 ms | 7.00 req/s | 1,260 | 100.00% | 0 | `20260624-135401` |
| `queryproject_stress` | 1,477.0 ms | 5,225.6 ms | 7,866.5 ms | 17.11 req/s | 8,215 | 91.65% | 0 | `20260624-135401` |
| `queryproject_spike` | 4,066.4 ms | 12,482.5 ms | 18,791.5 ms | 17.60 req/s | 2,146 | 82.53% | 4,603 | `20260624-135401` |
| `queryproject_soak` | 10.1 ms | 24.4 ms | 32.3 ms | 61.09 req/s | 439,848 | 100.00% | 0 | `20260623-163003` |

---

## Endpoint Rollup

Weighted by request count within each endpoint family.

| Endpoint family | Total requests | Checks pass | Status pass | Shape pass | Latency pass |
|---|---:|---:|---:|---:|---:|
| `/person/query` (mixed snapshot) | 346,238 | 99.74% | 100.00% | 100.00% | 99.22% |
| `/person/QueryProject` (mixed snapshot) | 451,469 | 99.77% | 100.00% | 100.00% | 99.30% |

---

## Benchmark-Type Reviews (1 sentence each)

### Load

| Endpoint | p95 | Throughput | Check pass |
|---|---:|---:|---:|
| `/person/query` | 209.7 ms | 7.00 req/s | 100.00% |
| `/person/QueryProject` | 153.4 ms | 7.00 req/s | 100.00% |

**Review:** Load remains strong on both endpoint families in the latest rerun, with QueryProject retaining the tighter p95.

### Stress

| Endpoint | p95 | Throughput | Check pass |
|---|---:|---:|---:|
| `/person/query` | 7,023.7 ms | 14.44 req/s | 90.92% |
| `/person/QueryProject` | 5,225.6 ms | 17.11 req/s | 91.65% |

**Review:** Stress is still the main bottleneck in this snapshot, with high tail latency and check misses on both endpoints.

### Spike

| Endpoint | p95 | Throughput | Check pass | Dropped iters |
|---|---:|---:|---:|---:|
| `/person/query` | 17,596.5 ms | 14.97 req/s | 85.66% | 4,868 |
| `/person/QueryProject` | 12,482.5 ms | 17.60 req/s | 82.53% | 4,603 |

**Review:** Spike remains the weakest profile for both endpoints, with heavy tail latency and substantial dropped-iteration pressure.

### Soak

| Endpoint | p95 | Throughput | Requests | Check pass |
|---|---:|---:|---:|---:|
| `/person/query` | 166.5 ms | 46.69 req/s | 336,166 | 100.00% |
| `/person/QueryProject` | 24.4 ms | 61.09 req/s | 439,848 | 100.00% |

**Review:** Soak stability is strong for both endpoints with clean correctness and low sustained p95.

---

## Comparison to Business Standards (single-instance context)

Typical internal API SLO bands are shown for orientation.

| Area | Typical business target (p95) | Latest | Result |
|---|---:|---:|---|
| QueryProject load/spike/soak | 100–300 ms | 24–12,482 ms | Miss |
| QueryProject stress | 300–700 ms | 5,226 ms | Miss |
| Query load | 300–700 ms | 210 ms | Exceeds target |
| Query stress | 500–1,000 ms | 7,024 ms | Miss |
| Query spike | 700–1,500 ms | 17,597 ms | Miss |
| Query soak | 500–1,000 ms | 167 ms | Exceeds target |
| Status + response-shape correctness | 99.9–100% | 100% | Meets |

**Interpretation:** In this mixed snapshot, load and correctness are strong, but stress and spike remain well outside business latency targets on both endpoint families.

---

## Grades (current run)

| Category | Grade | Rationale |
|---|---|---|
| Query functional correctness (status/shape) | **A** | 100% status and shape checks across mixed snapshot requests |
| Query load | **A** | Strong p95 with perfect checks in latest rerun |
| Query stress | **D+** | High p95 and check failures indicate ongoing bottleneck |
| Query spike | **F** | Very high p95 and high dropped iterations in burst profile |
| Query soak | **A** | Stable long-run behavior with strong p95 and zero check failures |
| QueryProject path | **C-** | Load remains healthy, but stress/spike tails and dropped iterations remain high |

---

## Key Caveats

1. This is a **single-instance benchmark** (API + DB + Redis + k6 + dev tooling on one machine), so production-separated infra should improve absolute numbers.
2. This document merges multiple timestamps and profile subsets (new load/stress/spike + prior soak), so strict cross-profile fairness is directional rather than apples-to-apples.
3. Stress and spike are the limiting profiles for both endpoints in the latest rerun, visible in tail latency and dropped iterations.
4. Throughput under `constant-arrival-rate` can still be capped by available VUs and burst tails.

---

## Recommended Next Steps

1. Rerun soak to complete a fully fresh 8-suite snapshot from one timestamp.
2. Prioritize stress/spike tuning (arrival stages, maxVUs, case-mix) to reduce dropped iterations and p95 tails.
3. Keep using standardized pagination ranges across Query and QueryProject for fairness.
4. Re-validate in an isolated environment (split load generator from API/DB/cache) before final SLO sign-off.
