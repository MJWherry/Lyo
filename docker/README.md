# Dockerized benchmarks and tests

Run the Lyo BenchmarkDotNet suites or the xUnit test projects inside a container,
in the background, with configurable CPU/memory limits.

A single `run` service is driven by **`TARGET`**, which selects projects by
group, exact name, or a list. The same `TARGET` decides both what the image
**compiles** and what the container **runs**, so every image is lean — it only
contains the projects you asked for. The source is baked in (multi-stage build
copies the artifacts and warmed NuGet cache into the final .NET 10 SDK + python3
image), so the host source tree is **never** mounted for building and a container
run can never write to your host `obj/bin` (no more broken `.slnx`). Notes:

- `Tools/` apps (`Lyo.TestApi`, `Lyo.TestConsole`, `Lyo.Gateway`, ...) and the
  whole-solution build are never compiled — only the selected `*.Benchmarks` /
  `*.Tests` projects (and their dependencies).
- The **Tesseract OCR native libs are installed automatically** only when a
  selected project needs them (the OCR test). No flag to set.
- Source changes require a rebuild to take effect (`python3 scripts/docker/run.py
  --build-only <target>`, or `docker compose build run`). Per-target images keep
  the build small.
- The SDK is in the final image on purpose: BenchmarkDotNet compiles a small
  per-run executable, so the SDK is needed at runtime too.

## TARGET grammar

`TARGET` is a space- or comma-separated list of tokens (see
[`scripts/docker/resolve_targets.py`](../scripts/docker/resolve_targets.py)):

| Token | Resolves to |
| --- | --- |
| `benchmarks` (or `bench`) | every `Lyo.Net/**/*.Benchmarks.csproj` |
| `tests` (or `test`) | every `Lyo.Net/**/*.Tests.csproj` |
| `all` | both groups |
| `Lyo.Lock.Benchmarks` | the project file `Lyo.Lock.Benchmarks.csproj` |
| `'*.Benchmarks'`, `'Lyo.Lock.*'` | a glob over project file names (matches runnable `*.Tests`/`*.Benchmarks` only) |
| `path/to/Foo.csproj` | that exact csproj |

Globs are shell wildcards matched against the project file name (`*`, `?`, `[...]`);
`.csproj` is appended if you omit it. Quote them (`'*.Benchmarks'`) so your shell
doesn't expand them before the runner sees them. To build a non-runnable library,
pass its exact name or `path/to/Foo.csproj`.

## Setup

```bash
cp .env.example .env        # tune CPU_LIMIT / MEM_LIMIT, HOST_UID/HOST_GID, options
```

### One-time host cleanup

If you previously ran the old bind-mounted setup, clear the root-owned/poisoned
build output once so Rider builds clean again:

```bash
sudo git clean -xdff -- '**/obj' '**/bin'   # or: sudo rm -rf **/obj **/bin
dotnet restore Lyo.Net/Lyo.slnx
```

## Run

The wrapper [`scripts/docker/run.py`](../scripts/docker/run.py) builds the right
per-target image (auto-tagged so targets don't clobber each other) and runs it
detached by default:

```bash
python3 scripts/docker/run.py Lyo.Lock.Benchmarks            # one benchmark suite
python3 scripts/docker/run.py Lyo.Query.Tests                # one test project
python3 scripts/docker/run.py benchmarks                     # every benchmark suite
python3 scripts/docker/run.py tests                          # every *.Tests (OCR libs auto-added)
python3 scripts/docker/run.py all                            # benchmarks + tests
python3 scripts/docker/run.py Lyo.Lock.Benchmarks Lyo.Cache.Benchmarks   # a list
```

Options (passthrough):

```bash
python3 scripts/docker/run.py --fg Lyo.Hashing.Benchmarks                 # foreground (default: detached)
python3 scripts/docker/run.py --build-only benchmarks                     # build the image, don't run
python3 scripts/docker/run.py --no-docker Lyo.Cache.Benchmarks            # skip Testcontainers classes
python3 scripts/docker/run.py --filter '*Sha256*' Lyo.Hashing.Benchmarks  # BenchmarkDotNet --filter
python3 scripts/docker/run.py --test-filter 'Category=Fast' Lyo.Csv.Tests # xUnit --filter
```

Prefer driving compose directly? Set `TARGET` and a `RUN_IMAGE` tag yourself so
distinct targets cache separately:

```bash
TARGET=Lyo.Lock.Benchmarks RUN_IMAGE=lyo-runner-lock docker compose run -d --rm run
TARGET=tests RUN_IMAGE=lyo-runner-tests docker compose build run   # just (re)build
```

Follow progress:

```bash
docker compose logs -f
# or, for a specific detached container:
docker logs -f <container-id>
```

## Where results go

Mounted back to the host:

- `docs/benchmarks/data/` — aggregated dashboard manifests (`encryption.json`, …)
- `docs/benchmarks/history/` — timestamped snapshots for the portfolio Snapshot dropdown

After a benchmark run, `scripts/benchmarks/build_manifests.py` (invoked automatically)
writes both, and the runner `chown`s them to `HOST_UID:HOST_GID`. Open
`docs/benchmarks/index.html` or the portfolio `/benchmarks/<suite>` page.

Everything else stays inside the container and is discarded with `--rm`:

- Per-suite raw `BenchmarkDotNet.Artifacts/` (consumed by the manifest builder
  before exit).
- `dotnet test` results (`TestResults/`); pass/fail is reported via the exit
  code and `docker compose logs`.

## Configuration

Set in `.env` (see `.env.example` for the full list):

| Variable | Purpose | Default |
| --- | --- | --- |
| `TARGET` | Projects to build + run (group/name/list) | `all` |
| `RUN_IMAGE` | Image tag for the `run` service (wrapper sets per-target) | `lyo-runner-all` |
| `CPU_LIMIT` | CPUs the runner container may use | `4` |
| `MEM_LIMIT` | Runner container memory cap | `8g` |
| `HOST_UID` | UID the `docs/benchmarks/{data,history}` mounts are chowned to | `1000` |
| `HOST_GID` | GID the `docs/benchmarks/{data,history}` mounts are chowned to | `1000` |
| `BENCH_FILTER` | BenchmarkDotNet `--filter` glob | `*` |
| `NO_DOCKER` | `1` skips Testcontainers-backed benchmark classes | `0` |
| `TEST_FILTER` | xUnit `--filter` expression | (none) |
| `TESTCONTAINERS_HOST_OVERRIDE` | Host advertised to sibling containers | `host.docker.internal` |

## Testcontainers / Docker-backed suites

The host Docker socket is mounted (`/var/run/docker.sock`) so suites that use
Testcontainers (`cache`, `query`, `lock` benchmarks and all `*.Postgres`/Redis
tests) can spin up sibling Redis/Postgres containers.

Caveats:

- Sibling containers spawned via the socket run on the host and are **not**
  bound by `CPU_LIMIT` / `MEM_LIMIT` (those constrain only the runner).
- Constraining CPU changes absolute BenchmarkDotNet numbers; keep limits fixed
  for run-to-run comparability.
- To run fully isolated without Docker-backed work, pass `--no-docker` (benchmark
  suites only run their in-process classes).
