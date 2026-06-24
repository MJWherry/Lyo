# K6 Benchmark Summary — Lyo Query API

## Overview

Production-matrix k6 review for the Lyo person query endpoints using the symmetric suite layout.

**Merged runs analyzed:** query from `k6/results/prod-matrix-20260623-211829/` + QueryProject from `k6/results/prod-matrix-20260623-163003/`  
**Date:** June 23, 2026  
**Run type:** Combined single-instance snapshot (query current run + QueryProject prior full run)

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

Query source run: `prod-matrix-20260623-211829`; QueryProject source run: `prod-matrix-20260623-163003`.

---

## Combined Results (query + QueryProject)

| Suite | Avg | p95 | p99 | Throughput | Requests | Checks pass | Dropped iterations | Source run |
|---|---:|---:|---:|---:|---:|---:|---:|
| `query_load` | 25.1 ms | 85.2 ms | 112.5 ms | 20.00 req/s | 3,601 | 100.00% | 0 | `20260623-211829` |
| `query_stress` | 244.4 ms | 966.9 ms | 1,532.7 ms | 65.67 req/s | 31,521 | 100.00% | 0 | `20260623-211829` |
| `query_spike` | 402.0 ms | 1,938.9 ms | 3,087.4 ms | 50.92 req/s | 6,115 | 99.86% | 635 | `20260623-211829` |
| `query_soak` | 44.4 ms | 166.5 ms | 251.9 ms | 46.69 req/s | 336,166 | 100.00% | 0 | `20260623-211829` |
| `queryproject_load` | 10.5 ms | 22.4 ms | 28.0 ms | 19.99 req/s | 3,600 | 100.00% | 0 | `20260623-163003` |
| `queryproject_stress` | 63.6 ms | 216.4 ms | 252.7 ms | 178.08 req/s | 85,494 | 100.00% | 0 | `20260623-163003` |
| `queryproject_spike` | 10.0 ms | 22.4 ms | 28.9 ms | 56.23 req/s | 6,749 | 100.00% | 1 | `20260623-163003` |
| `queryproject_soak` | 10.1 ms | 24.4 ms | 32.3 ms | 61.09 req/s | 439,848 | 100.00% | 0 | `20260623-163003` |

---

## Endpoint Rollup

Weighted by request count within each endpoint family.

| Endpoint family | Total requests | Checks pass | Status pass | Shape pass | Latency pass |
|---|---:|---:|---:|---:|---:|
| `/person/query` (current run) | 377,403 | 99.998% | 100.00% | 100.00% | 99.99% |
| `/person/QueryProject` (prior full run) | 535,691 | 100.00% | 100.00% | 100.00% | 100.00% |

---

## Benchmark-Type Reviews (1 sentence each)

### Load

| Endpoint | p95 | Throughput | Check pass |
|---|---:|---:|---:|
| `/person/query` | 85.2 ms | 20.00 req/s | 100.00% |
| `/person/QueryProject` | 22.4 ms | 19.99 req/s | 100.00% |

**Review:** Load performance is strong on both endpoint families, with QueryProject showing tighter p95.

### Stress

| Endpoint | p95 | Throughput | Check pass |
|---|---:|---:|---:|
| `/person/query` | 966.9 ms | 65.67 req/s | 100.00% |
| `/person/QueryProject` | 216.4 ms | 178.08 req/s | 100.00% |

**Review:** Stress remains acceptable for Query and excellent for QueryProject with substantial throughput headroom.

### Spike

| Endpoint | p95 | Throughput | Check pass | Dropped iters |
|---|---:|---:|---:|---:|
| `/person/query` | 1,938.9 ms | 50.92 req/s | 99.86% | 635 |
| `/person/QueryProject` | 22.4 ms | 56.23 req/s | 100.00% | 1 |

**Review:** Spike is the only profile needing improvement on full-query, while QueryProject remains stable under burst.

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
| QueryProject load/spike/soak | 100–300 ms | 22–24 ms | Exceeds target |
| QueryProject stress | 300–700 ms | 216 ms | Meets comfortably |
| Query load | 300–700 ms | 85 ms | Exceeds target |
| Query stress | 500–1,000 ms | 967 ms | Meets (near upper bound) |
| Query spike | 700–1,500 ms | 1,939 ms | Miss |
| Query soak | 500–1,000 ms | 167 ms | Exceeds target |
| Status + response-shape correctness | 99.9–100% | 100% | Meets |

**Interpretation:** In this combined single-instance snapshot, QueryProject is business-grade across profiles and full-query is strong except for spike headroom.

---

## Grades (current run)

| Category | Grade | Rationale |
|---|---|---|
| Query functional correctness (status/shape) | **A** | 100% status and shape checks across 377k+ query requests |
| Query load | **A** | Very low p95 and clean checks |
| Query stress | **B+** | Meets common p95 target band, but close to upper bound |
| Query spike | **C** | Large improvement, but p95 still above spike target and dropped iterations remain |
| Query soak | **A** | Stable long-run behavior with strong p95 and zero check failures |
| QueryProject path | **A** | Consistent low p95, high throughput, and perfect functional checks across merged source run |

---

## Key Caveats

1. This is a **single-instance benchmark** (API + DB + Redis + k6 + dev tooling on one machine), so production-separated infra should improve absolute numbers.
2. This document merges two run timestamps (query and QueryProject), so absolute cross-endpoint fairness is directional rather than strict apples-to-apples.
3. Spike remains the limiting profile for `/person/query`, visible in both tail latency and dropped iterations.
4. Throughput under `constant-arrival-rate` can still be capped by available VUs and burst tails.

---

## Recommended Next Steps

1. Rerun the full 8-suite matrix to re-establish a complete fair Query vs QueryProject snapshot.
2. Tune query spike profile (arrival stages, maxVUs, include-heavy case mix) to reduce dropped iterations.
3. Keep using standardized pagination ranges across Query and QueryProject for fairness.
4. Re-validate in an isolated environment (split load generator from API/DB/cache) before final SLO sign-off.
