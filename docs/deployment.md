# Deployment and operations

Lyo is primarily a set of libraries consumed by your own apps, plus a few sample
hosts under [`Lyo.Net/Apps/`](../Lyo.Net/Apps/) and [`Lyo.Net/Tools/`](../Lyo.Net/Tools/).
There is no single "deploy Lyo" target; you deploy the app that consumes the
packages. This page covers the operational surface that *does* ship in the repo
and the things to keep in mind when running Lyo-based services.

## What the bundled container stack is (and isn't)

The repo's [`docker-compose.yml`](../docker-compose.yml) and
[`docker/`](../docker/) directory define a **benchmark/test runner**, not a
production application stack. A single `run` service:

- compiles and runs only the projects named by `TARGET` (a group, exact name,
  glob, or list);
- bakes the source into the image (the host tree is never mounted for building);
- mounts the host Docker socket so Testcontainers can spin up sibling
  Redis/Postgres containers;
- mounts `docs/benchmarks/data` and `docs/benchmarks/history` back to the host for dashboard manifests.

Run it via the wrapper (see [Testing](testing.md)):

```bash
python3 scripts/docker/run.py benchmarks      # or: tests / all / <project> / '<glob>'
docker compose logs -f                        # follow progress
```

Resource limits and other knobs come from `.env`; see [Configuration](configuration.md)
and the full runner reference in [`docker/README.md`](../docker/README.md).

## Deploying an app that uses Lyo

Treat a Lyo-consuming service like any other .NET app:

1. **Target framework:** build against .NET 10 (`net10.0`); some packages also
   support `netstandard2.0`. Shared build settings:
   [`Lyo.Net/Directory.Build.props`](../Lyo.Net/Directory.Build.props).
2. **Packages:** resolve the Lyo packages from your feed (see
   [Publishing](publishing.md)) and pin versions explicitly.
3. **Backing services:** provision the infrastructure the packages you use
   require — PostgreSQL for `*.Postgres` packages, Redis for distributed locks
   and Fusion caching, plus any vendor credentials for Integration/Communication
   providers.
4. **Database migrations:** packages that ship EF Core migrations expose hosted
   migration helpers (see [`Lyo.Postgres`](../Lyo.Net/Data/Postgres/Lyo.Postgres/README.md)).
   Decide whether migrations run on startup or as a separate deploy step.
5. **Secrets:** never bake secrets into images. Supply key-store/KEK secrets and
   vendor API keys via your platform's secret manager and bind them through
   configuration (see [Security](security/README.md)).

## Operational notes

- **Observability:** wire up `Lyo.Metrics` (OpenTelemetry backend available) and
  `Lyo.Diagnostic` for metrics, breadcrumbs, and exception capture.
- **Resilience:** `Lyo.Resilience` builds Polly pipelines from configuration for
  outbound calls to vendors and databases.
- **Health checks:** `Lyo.Health` provides `IHealth`/`HealthResult` for readiness
  and liveness endpoints.
- **Encryption keys in production:** do not use `LocalKeyStore`. Use a managed
  KeyStore (AWS KMS via `Lyo.KeyStore.Aws`, or your own `IKeyStore`), and plan
  key rotation. See [Security](security/README.md) and
  [security/encryption.md](security/encryption.md).
- **Benchmark comparability:** if you run the container suites in CI, keep
  `CPU_LIMIT`/`MEM_LIMIT` fixed so numbers stay comparable across runs.
