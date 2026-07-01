# Lyo documentation

Project-wide documentation for **LYO — Library for Your Organization**, a .NET
toolkit of libraries and apps for business data. The per-package API docs live
next to each library as `README.md` files; this folder holds the cross-cutting
guides that do not belong to a single package.

> The root [`README.md`](../README.md) is the canonical project overview and
> package index. Start there if you are new to the repo.

## Guides

| Document                                       | What it covers                                                                           |
|------------------------------------------------|------------------------------------------------------------------------------------------|
| [Getting started](getting-started.md)          | Prerequisites, consuming a package, a minimal working example.                           |
| [Architecture](architecture.md)                | Area model, the dependency law, and how the package taxonomy fits together.              |
| [Configuration](configuration.md)              | Environment variables and options used by the docker runner and tooling.                 |
| [Testing](testing.md)                          | Running unit tests, BenchmarkDotNet suites, and k6 load tests (local and containerized). |
| [Deployment](deployment.md)                    | The container stack, resource limits, and operational notes.                             |
| [Publishing](publishing.md)                    | How packages are versioned and packed with `build-nuget.sh`.                             |
| [Glossary](glossary.md)                        | Domain terms and recurring concepts.                                                     |
| [Security](security/README.md)                 | Security model, reporting, and crypto design notes.                                      |

## Interactive artifacts (HTML)

These are self-contained web apps, not markdown. GitHub/GitLab will not render
them inline when browsing the repo — open them locally (clone, then open the
file in a browser) or publish them via Pages.

| Artifact                                         | What it is                                                                                                                |
|--------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------|
| [`Lyo.ProjectGraph.html`](Lyo.ProjectGraph.html) | Interactive project-reference graph for the solution (regenerate with [`scripts/gen_graph.py`](../scripts/gen_graph.py)). |
| [`benchmarks/index.html`](benchmarks/index.html) | Benchmark dashboards for BenchmarkDotNet and k6 runs. See [benchmarks/README.md](benchmarks/README.md).                   |

## Related project files

- Contributing: [`CONTRIBUTING.md`](../CONTRIBUTING.md)
- Code of conduct: [`CODE_OF_CONDUCT.md`](../CODE_OF_CONDUCT.md)
- Security policy: [`SECURITY.md`](../SECURITY.md)
- License: [`LICENSE`](../LICENSE) (Apache-2.0)
- Package taxonomy (detailed): [`Lyo.Net/docs/package-layout.md`](../Lyo.Net/docs/package-layout.md)
