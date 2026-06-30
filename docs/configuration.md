# Configuration

This page documents the environment-level configuration used by the repo's
tooling — chiefly the containerized benchmark/test runner driven by
[`docker-compose.yml`](../docker-compose.yml). Per-library runtime configuration
(connection strings, DI options, secrets) is documented in each package's own
`README.md`.

## The `.env` file

The docker runner reads an optional `.env` at the repo root. Copy the template
and adjust:

```bash
cp .env.example .env
```

`.env` is optional — [`docker-compose.yml`](../docker-compose.yml) supplies
`${VAR:-default}` fallbacks so the stack runs without it. The template is
[`.env.example`](../.env.example).

## Variables

| Variable | Purpose | Default |
|----------|---------|---------|
| `CPU_LIMIT` | CPUs the runner container may use (fractional allowed, e.g. `1.5`). | `4` |
| `MEM_LIMIT` | Memory cap for the runner container (e.g. `2g`, `512m`). | `8g` |
| `HOST_UID` | UID that `docs/benchmarks/data` manifests are `chown`ed to on exit (the runner is root for the Docker socket). | `1000` |
| `HOST_GID` | GID for the same manifest ownership fix. | `1000` |
| `TARGET` | What the `run` service builds **and** runs: a group (`benchmarks` / `tests` / `all`), an exact project name, a glob, or a space/comma list. | `all` |
| `RUN_IMAGE` | Image tag for the `run` service; set a distinct value per target so builds don't clobber each other. | `lyo-runner-all` |
| `BENCH_FILTER` | BenchmarkDotNet `--filter` glob applied to every selected suite. | `*` |
| `NO_DOCKER` | `1` skips Testcontainers-backed benchmark classes (Redis/Postgres). | `0` |
| `TEST_FILTER` | Optional xUnit `--filter` expression applied to every selected test project. | (empty) |
| `TESTCONTAINERS_HOST_OVERRIDE` | Host that Testcontainers advertises to sibling containers reached over the mounted Docker socket. | `host.docker.internal` |

The wrapper script [`scripts/docker/run.sh`](../scripts/docker/run.sh) sets
`TARGET` and a per-target `RUN_IMAGE` for you, so prefer it over editing those
two by hand. To set ownership to your user in one step:

```bash
echo "HOST_UID=$(id -u)" >> .env && echo "HOST_GID=$(id -g)" >> .env
```

## Caveats

- The source is baked into the image at build time, so **code changes require a
  rebuild** (`scripts/docker/run.sh --build-only <target>` or
  `docker compose build run`). Only `docs/benchmarks/data` is mounted back to the
  host.
- Containers that Testcontainers spawns via the mounted Docker socket run on the
  **host** and are **not** limited by `CPU_LIMIT` / `MEM_LIMIT` (those bound only
  the runner container).
- Constraining CPU changes absolute BenchmarkDotNet numbers; keep the limits
  fixed for run-to-run comparability.

See [Deployment](deployment.md) and [Testing](testing.md) for how these values
are used in practice, and [`docker/README.md`](../docker/README.md) for the full
runner reference.
