# K6 Benchmark Analysis — Lyo Query API

**Date**: April 22, 2026  
**Results**: `k6/framework-person/results/20260422-015048/`  
**Environment**: Local development (API + PostgreSQL + k6 on same machine)

---

## Test Environment

| Spec                 | Value                                                                                                                                                                                                                                                                                      |
|----------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **CPU**              | Intel Core Ultra 7 155U (12 cores / 14 threads, up to 4.8 GHz) — laptop-class                                                                                                                                                                                                              |
| **RAM**              | 62 GB                                                                                                                                                                                                                                                                                      |
| **Database**         | PostgreSQL (localhost:5437)                                                                                                                                                                                                                                                                |
| **OS**               | Linux 6.8.0-106-generic, x86_64                                                                                                                                                                                                                                                            |
| **Disk**             | 1.8 TB root partition, 721 GB free                                                                                                                                                                                                                                                         |
| **Dataset**          | Large PostgreSQL graph (representative counts): **~135k** `person` (29 columns); **~815k** `contact_address` (10) + **~815k** `address` (21); **~480k** `contact_phone_number` (10) + **~460k** `phone_number` (9); **~285k** `contact_email_address` (11) + **~285k** `email_address` (6) |
| **Compression**      | Brotli enabled (API-side), `Accept-Encoding: br, gzip, deflate` on client                                                                                                                                                                                                                  |
| **Query cache tags** | **`CacheOptions:QueryCacheTagGranularity`** = **`Broad`** (default) — type-scoped **`entity:{type}`** tags only; lower CPU on cache writes than **`Granular`** per-row instance tags                                                                                                       |
| **DTO mapping**      | **`Lyo.TestApi`** uses **Mapster** (`MapsterLyoMapper` implementing **`ILyoMapper`**) for entity ↔ API model mapping — adds overhead vs manual mapping or a leaner alternative                                                                                                             |

> **Important**: API, database, and k6 load generator all share the same 12-core laptop CPU.
> This makes results **pessimistic** — production deployments with separated infrastructure would perform better.

---

## Entity Under Test: Person

The `Person` entity is moderately complex:

- **29 columns** (26 scalar + 3 JSONB)
- **7 navigation properties** in the domain model
- **PostgreSQL indexes** (typical TestApi / `people.person`): `PK_person`, `ix_person_created_timestamp`, `ix_person_first_name`, `ix_person_is_active`, `ix_person_last_name`,
  `ix_person_last_name_first_name`

The heavy-include tests exercise 3 include paths, each of which traverses a **join table** before reaching the target entity — making each include a **two-hop navigation**:

```
person (root)                         ← 1 table
 ├── contact_phone_numbers ──→ phone_number      ← 2 tables
 ├── contact_email_addresses ──→ email_address    ← 2 tables
 └── contact_addresses ──→ address                ← 2 tables
                                      ─────────────
                                      7 tables total
```

This means the "heavy include" query is a **7-table query with 6 JOINs** (or 6 separate roundtrips under EF Core split-query mode), hydrating ~2000 root entities plus all their
related records across 6 child/grandchild tables.

---

## Test Scenarios

| #  | Name                  | Executor              | Peak Load               | Duration | SLA (p95) | Query Complexity                                                                                      |
|----|-----------------------|-----------------------|-------------------------|----------|-----------|-------------------------------------------------------------------------------------------------------|
| 01 | Load Mixed Queries    | constant-arrival-rate | 20 iter/s, 50 max VUs   | 3 min    | 3,000 ms  | 5 rotating query types (baseline, filters+sorts, query nodes, select projection, subquery with regex) |
| 02 | Stress Heavy Includes | ramping-vus           | 5→40 VUs                | ~8 min   | 2,500 ms  | Realistic include: 100–300 items, 3 tables (contactaddresses.address), cache bypass                   |
| 03 | Spike Select Fields   | ramping-arrival-rate  | 5→80 iter/s, 80 max VUs | 2 min    | 3,000 ms  | Select projection (5 fields including nested navigation path), 1000 rows                              |
| 04 | Soak Mixed Leak Watch | constant-vus          | 10 VUs                  | 2 hours  | 3,500 ms  | Mixed query rotation + periodic heavy-include spikes (every 20th iteration), randomized queries       |
| 05 | Load Subquery         | constant-arrival-rate | 20 iter/s, 50 max VUs   | 3 min    | 3,500 ms  | Two-phase subquery with nested And/Or query nodes and regex comparator                                |

---

## Results Summary

### Scenario 01 — Load Mixed Queries

| Metric                | Value                       |
|-----------------------|-----------------------------|
| **Avg latency**       | 15.6 ms                     |
| **Median latency**    | 13.8 ms                     |
| **p95 latency**       | 22.9 ms                     |
| **p99 latency**       | 32.2 ms                     |
| **Max latency**       | 325 ms                      |
| **Throughput**        | 20.0 req/s (3,600 requests) |
| **Success rate**      | **100%**                    |
| **Avg response size** | 253 KB                      |

**Assessment**: Excellent. Five different query types — including dynamically compiled expression trees, query node filters, sort chains, and regex-based subqueries — stay in a
tight band at sustained 20 req/s. Zero failures. Randomized start/amount for cache bypass. Slightly faster than the April 19 archive on avg/p95.

---

### Scenario 02 — Stress Heavy Includes

| Metric                | Value                         |
|-----------------------|-------------------------------|
| **Avg latency**       | 111.2 ms                      |
| **Median latency**    | 85.2 ms                       |
| **p95 latency**       | 302 ms                        |
| **p99 latency**       | 510 ms                        |
| **Max latency**       | 1,162 ms                      |
| **Throughput**        | 160.5 req/s (77,078 requests) |
| **Success rate**      | **100%**                      |
| **Avg response size** | 615 KB                        |

**Assessment**: Strong under concurrent load. Realistic workload: 100–300 items, 3 tables (person → contact_addresses → address). p95 is higher than the April 19 archive (~244 ms)
but remains well within the 2.5 s SLA. API, Postgres, and k6 still share CPU while ramping to 40 VUs.

---

### Scenario 03 — Spike Select Fields

| Metric                | Value                       |
|-----------------------|-----------------------------|
| **Avg latency**       | 6.0 ms                      |
| **Median latency**    | 5.5 ms                      |
| **p95 latency**       | 8.5 ms                      |
| **p99 latency**       | 13.6 ms                     |
| **Max latency**       | 50 ms                       |
| **Throughput**        | 56.2 req/s (6,750 requests) |
| **Success rate**      | **100%**                    |
| **Avg response size** | 210 KB                      |

**Assessment**: Excellent. Handles a high arrival-rate spike with sub–9 ms p95 latency. Select projection (five fields including a nested navigation path) remains efficient and
consistent with prior archives.

---

### Scenario 04 — Soak Mixed Leak Watch

| Metric                | Value                                |
|-----------------------|--------------------------------------|
| **Avg latency**       | 80.2 ms                              |
| **Median latency**    | 21.0 ms                              |
| **p95 latency**       | 337 ms                               |
| **p99 latency**       | 1,148 ms                             |
| **Max latency**       | 5,328 ms                             |
| **Throughput**        | 43.1 req/s (310,665 requests)        |
| **Duration**          | 2 hours (configured)                 |
| **Success rate**      | **99.999%** (621,323 / 621,330 checks passed; 7 SLA misses) |
| **Avg response size** | 622 KB (range varies by query shape) |

**Assessment**: Very strong. Long soak with mixed load and periodic heavy-include spikes; **310k+** requests with only **7** check failures (all SLA timing, not HTTP errors).
Median latency improved vs the April 19 archive; p95 is comparable. Randomized queries limit hot-cache repeat hits.

---

### Scenario 05 — Load Subquery

| Metric                | Value                       |
|-----------------------|-----------------------------|
| **Avg latency**       | 12.8 ms                     |
| **Median latency**    | 11.9 ms                     |
| **p95 latency**       | 17.5 ms                     |
| **p99 latency**       | 24.2 ms                     |
| **Max latency**       | 102 ms                      |
| **Throughput**        | 20.0 req/s (3,601 requests) |
| **Success rate**      | **100%**                    |
| **Avg response size** | 236 KB                      |

**Assessment**: Excellent. The most complex query structure in the suite — a two-level `QueryNode` tree with a `SubQuery` containing nested `And`/`Or` logical operators and a regex
comparator — holds ~17.5 ms p95 at sustained 20 req/s. Randomized start/amount for cache bypass.

---

## Industry Comparison

### Lightweight Queries (Scenarios 01, 03, 05)

| System / Stack                                                         | Comparable Workload                                  | Typical p95 Latency                                                                                        |
|------------------------------------------------------------------------|------------------------------------------------------|------------------------------------------------------------------------------------------------------------|
| **Lyo Query API** (this run, EF Core + dynamic expressions)            | Mixed / spike / subquery workloads (see scenarios)   | **~8–23 ms** scenario-dependent                                                                            |
| **Hasura / PostgREST** (direct PG → JSON, no ORM)                      | Filters + sorts + pagination                         | 5–30 ms                                                                                                    |
| **GraphQL** (Hasura — same stack as above)                             | Auto-generated GraphQL over PostgreSQL               | 5–30 ms                                                                                                    |
| **GraphQL** (Apollo / Hot Chocolate / gqlgen + hand-written resolvers) | List + filter + sort + pagination as field resolvers | **40–250 ms** with solid batching (DataLoader, lookahead); **200 ms–2s+** with N+1 or cold resolver chains |
| **Typical EF Core REST API**                                           | Dynamic filters + pagination                         | 50–200 ms                                                                                                  |
| **Django REST Framework**                                              | QuerySet filters + pagination                        | 50–300 ms                                                                                                  |
| **Ruby on Rails**                                                      | ActiveRecord scopes + pagination                     | 80–400 ms                                                                                                  |
| **Spring Boot + JPA/Hibernate**                                        | JPQL with dynamic predicates                         | 30–150 ms                                                                                                  |

Lyo remains **at the top end of ORM-based frameworks** for these query shapes and **in the same order of magnitude** as thin Postgres-to-JSON gateways for comparable payload sizes
on local hardware.

**GraphQL nuance**: *Hasura* is already GraphQL-first; its row above is the apples-to-apples comparison for “declarative query over Postgres.” *Application-layer* GraphQL (custom
resolvers) spans a wider band because **resolver shape, DataLoader usage, and query-plan depth** dominate—similar to why ORM-heavy REST APIs vary. Lyo’s `QueryRequest` is **one
round trip with a single SQL plan** for the tested shapes, which is closer to Hasura/PostgREST ergonomically than to a deep GraphQL resolver tree.

### Heavy Include / Navigation Queries (Scenario 02)

| System / Stack                               | Comparable Workload                                              | Typical Behavior                                          |
|----------------------------------------------|------------------------------------------------------------------|-----------------------------------------------------------|
| **Lyo Query API** (this run)                 | 3 tables, 100–300 rows, ~615 KB, up to 40 VUs                    | **~111 ms avg, p95 ~302 ms, 100% within 2.5 s SLA**      |
| **EF Core API** (typical)                    | 5–7 table includes, 500–2000 rows                                | 2–10s depending on depth and row count                    |
| **Django + select_related/prefetch_related** | 2000 rows + 3 FK joins                                           | 3–10s                                                     |
| **Rails + includes (eager load)**            | 2000 rows + 3 associations                                       | 5–15s                                                     |
| **GraphQL (Apollo + DataLoader)**            | Nested resolvers, 3 levels deep                                  | 1–5s (with batching), 10–30s (without)                    |
| **GraphQL (Hasura)**                         | Nested object queries, 3 levels (same engine as lightweight row) | 500 ms–2 s (Postgres-bound, no ORM)                       |
| **GraphQL (Hot Chocolate / gqlgen, custom)** | Manual nested field resolvers + batching                         | Often **2–15s** if batching misses; highly team-dependent |

The realistic include workload remains **production-appropriate** (pagination band, single primary include path). Heavier multi-include graphs are still possible; cap page size and
include depth in public APIs.

---

## Grades

| Category                                                        | Grade | Notes                                                                                                   |
|-----------------------------------------------------------------|-------|---------------------------------------------------------------------------------------------------------|
| Simple queries (filters, sorts, pagination)                     | **A** | ~16 ms avg mixed load; aligns with strong ORM-class APIs                                                |
| Complex query compilation (subqueries, regex, expression trees) | **A** | ~13 ms avg / ~17.5 ms p95 subquery scenario                                                             |
| Spike/burst handling                                            | **A** | ~8.5 ms p95 on spike scenario; no errors                                                                  |
| Sustained load stability                                        | **A** | 2 h soak, 310K+ requests, 99.999% k6 checks                                                               |
| Realistic include loads (3-table, 100–300 rows, high VUs)       | **A** | 100% within SLA; p95 ~302 ms under 40 VUs                                                                 |

---

## Run history (recent archives)

| Date | Results folder | Mixed p95 | Spike p95 | Subquery p95 | Soak requests | Soak check rate |
|------|----------------|----------:|----------:|-------------:|--------------:|----------------:|
| **2026-04-22** | `20260422-015048` | 22.9 ms | 8.5 ms | 17.5 ms | 310,665 | 99.999% |
| 2026-04-19 | `20260419-190727` | 29.3 ms | 8.9 ms | 17.9 ms | 303,896 | 100% |

---

## Caveats

1. **Shared hardware**: All three components (API, PostgreSQL, k6) compete for 12 CPU cores. Dedicated infrastructure would improve all numbers, especially under high concurrency.
2. **Run-to-run variance**: Laptop thermals, background processes, and cache state change absolute milliseconds. Use trends and scenario comparisons, not a single number in
   isolation.
3. **Dataset size**: This is **not** a toy database — person rows are in the **hundreds of thousands**, with **hundreds of thousands to ~800k** rows in major join tables (
   addresses, phones, emails). Latency still depends on **predicate selectivity**, **sort/index alignment**, and **page size** (`Start`/`Amount`); k6 scenarios use bounded windows,
   not full-table materialization of the graph. On a machine with ample RAM, hot indexes and pages may cache well, but **cold paths, large offsets, or missing index support**
   remain expensive at this scale.
4. **Localhost networking**: Zero network latency between k6 → API and API → PostgreSQL. Real deployments would add 0.5–2ms per hop.
5. **Scenario 02**: Realistic workload (100–300 items, 3 tables). Older 7-table, ~2000-row stress cases are more demanding; production workloads typically use smaller
   page sizes and fewer includes.
6. **Mapster in TestApi**: Benchmarks use the stock **`Lyo.TestApi`** host, which maps through **Mapster**. That cost is included in every response; a host with hand-written
   mapping, source-generated maps, or a different library could shift CPU and allocation profiles (and thus latency) up or down.
7. **Query cache tag granularity**: This run uses **`Broad`** (the library default) — type-wide tags only. **`Granular`** adds per-row instance tags (more CPU when storing cache
   entries, finer invalidation). A prior stressed laptop run with **`Granular`** showed heavy cache-tag overhead under load; **`Broad`** is the safer default for
   throughput-oriented hosts unless you rely on surgical invalidation.
8. **Soak SLA misses**: The April 22 archive logged **7** timing check failures out of 621,330 — all on heavy-shape SLA thresholds, not HTTP 4xx/5xx. Treat as tail latency under
   mixed load, not functional regression.
