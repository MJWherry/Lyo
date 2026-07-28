# Testing and benchmarking

There are three kinds of automated checks in this repo:

- **Unit/integration tests** — xUnit projects named `*.Tests`.
- **Micro-benchmarks** — BenchmarkDotNet projects named `*.Benchmarks`.
- **Load tests** — k6 scripts under [`k6/`](../k6/).

All three can run on the host with the .NET SDK, and the test/benchmark projects
can also run inside the container runner (see [Deployment](deployment.md)).

## Unit tests (host)

```bash
# Whole solution
dotnet test Lyo.Net/Lyo.slnx

# A single project
dotnet test Lyo.Net/Security/Encryption/Lyo.Encryption.Tests

# Filtered
dotnet test Lyo.Net/Data/Csv/Lyo.Csv.Tests --filter 'Category=Fast'
```

Tests that use Testcontainers (the `*.Postgres` suites, Redis-backed locks, etc.)
need a reachable Docker daemon to spin up sibling Redis/Postgres containers.

## Micro-benchmarks (host)

Each BenchmarkDotNet suite is a normal console project run in Release:

```bash
dotnet run -c Release --project Lyo.Net/Security/Encryption/Lyo.Encryption.Benchmarks
```

To run all suites and rebuild the dashboard data in one step, use the helper:

```bash
scripts/benchmarks/run-dotnet-benchmarks.sh                 # all suites
scripts/benchmarks/run-dotnet-benchmarks.sh --no-docker hashing csv
```

## Containerized tests and benchmarks

The container runner builds *only* the projects you select (and their
dependencies), so the image stays lean and your host `obj/bin` is never touched.
Drive it with the wrapper:

```bash
scripts/docker/run.sh Lyo.Lock.Benchmarks          # one benchmark suite
scripts/docker/run.sh Lyo.Query.Tests              # one test project
scripts/docker/run.sh benchmarks                   # every *.Benchmarks
scripts/docker/run.sh tests                        # every *.Tests (OCR libs auto-added)
scripts/docker/run.sh all                          # benchmarks + tests
```

Useful passthrough options:

```bash
scripts/docker/run.sh --fg Lyo.Hashing.Benchmarks                 # run in foreground
scripts/docker/run.sh --build-only benchmarks                     # build image only
scripts/docker/run.sh --no-docker Lyo.Cache.Benchmarks            # skip Testcontainers classes
scripts/docker/run.sh --filter '*Sha256*' Lyo.Hashing.Benchmarks  # BenchmarkDotNet --filter
scripts/docker/run.sh --test-filter 'Category=Fast' Lyo.Csv.Tests # xUnit --filter
```

The `TARGET` grammar (groups, exact names, globs, paths) and the full option list
are documented in [`docker/README.md`](../docker/README.md). Configuration such
as `CPU_LIMIT`/`MEM_LIMIT` lives in [Configuration](configuration.md).

## Load tests (k6)

The k6 workloads live under [`k6/framework-person/`](../k6/framework-person/) and
target the `TestApi` person endpoints. See that folder's
[README](../k6/framework-person/README.md) for running the matrix, and the
[K6 benchmark analysis](../Lyo.Net/Integration/Api/Lyo.Api/K6_BENCHMARK_ANALYSIS.md)
for archived results.

## Where results go: the dashboards

Both BenchmarkDotNet suites and k6 runs are normalized to one schema and rendered
by a single viewer under [`docs/benchmarks/`](benchmarks/index.html):

```bash
# Rebuild dashboard data from existing artifacts / k6 results
python3 scripts/benchmarks/build_manifests.py               # micro + k6
python3 scripts/benchmarks/build_manifests.py --k6-only
```

After regenerating, open [`docs/benchmarks/index.html`](benchmarks/index.html).
The dashboard internals (schema, SLA grading, per-report context) are described
in [benchmarks/README.md](benchmarks/README.md). When run in the container, the
only path written back to the host is `docs/benchmarks/data/`.
