# Lyo.Diagnostic

Decode stack traces, classify exceptions, keep breadcrumb trails, hold an in-memory error inbox, sanitise output, and write structured logs.

Source is grouped by feature folder. Namespaces match, for example `StackTrace/` maps to `Lyo.Diagnostic.StackTrace`. The root `Lyo.Diagnostic` namespace holds `AddDiagnosticsPackage` in `Registration/`. Package metadata DTOs and `IPackageMetadataStore` live in [`Lyo.PackageMetadata`](../PackageMetadata/Lyo.PackageMetadata).

| Folder | Namespace | |-------------------|---------------------------------| | `StackTrace/` | `Lyo.Diagnostic.StackTrace` | | `Classification/` | `Lyo.Diagnostic.Classification` | | `Context/` | `Lyo.Diagnostic.Context` | | `Breadcrumbs/` | `Lyo.Diagnostic.Breadcrumbs` | | `Inbox/` | `Lyo.Diagnostic.Inbox` | | `Logging/` | `Lyo.Diagnostic.Logging` | | `Sanitisation/` | `Lyo.Diagnostic.Sanitisation` | | `Registration/` | `Lyo.Diagnostic` |

## Examples

### Register in DI

```csharp
using Lyo.Diagnostic;
using Lyo.Diagnostic.Inbox;

services.AddDiagnosticsPackage();
services.AddInMemoryErrorInbox(o => o.MaxOccurrences = 5_000);
```

### Optional package metadata store (`IPackageMetadataStore`)

```csharp
using Lyo.Diagnostic;
using Lyo.PackageMetadata;

var store = new InMemoryPackageMetadataStore();
store.Register(["Npgsql."], new PackageMetadata(
    Guid.Parse("a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11"),
    PackageEcosystem.NuGet,
    "Npgsql",
    Version: "8.0.0",
    ArtifactDigestAlgorithm.Sha512,
    PackageArtifactDigest.ComputeHexSha512(System.Array.Empty<byte>())));
services.AddDiagnosticsPackage(packageMetadataStore: store);
```

## What ships in the package

- **Stack trace decoding** (`IStackTraceDecoder`). Frames, crash site, fingerprint (stable hash of user-frame method signatures). Optional `IPackageMetadataStore` from `Lyo.PackageMetadata` enriches frames with `PackageMetadata` (`Id` + `Name`, …). With a configured store, `IDiagnosticContextBuilder.BuildAsync` / `DecodeAsync` perform one bulk metadata resolve for the entire exception/textual inner stack-tree (embedded inner blocks and chained `InnerException`), not once per subtree. Sync `Build` / `Decode` throw when a store is set.
- **Classification** (`IExceptionClassifier`). Severity, kind, labels.
- **Diagnostic context** (`IDiagnosticContextBuilder`). One payload for a single failure.
- **Structured logging** (`IStructuredLogEnricher`). `ILogger` scopes with `diag.*` properties.
- **Trace sanitisation** (`ITraceSanitiser`). Redact PII/paths before API responses or logs.
- **Breadcrumbs** (`RingBufferBreadcrumbTrail`, `IBreadcrumbTrail`). Bounded FIFO trail of short events for triage. Cap per scope, for example an HTTP request.
- **Error inbox** (`IErrorInboxReader`, `IErrorOccurrenceSink`, `InMemoryErrorInbox`). Record and query grouped occurrences by fingerprint + exception kind + service. Single-process. Not shared across instances.

## Optional package metadata store (`IPackageMetadataStore`)

Use `Lyo.PackageMetadata` (`InMemoryPackageMetadataStore`, or `Lyo.PackageMetadata.Postgres`). Pass the store as the last argument to `AddDiagnosticsPackage`, or set
`StackTraceDecoderOptions.PackageMetadataStore`. `TryGetManyForStrippedMethodPrefixesAsync` resolves many stripped methods at once behind that call. Postgres may cache the
ordered prefix catalog in-process. See `PostgresPackageMetadataOptions.PrefixCatalogCaching` and `ClearPrefixCatalogCache`. When a store is configured, use `BuildAsync`
and `DecodeAsync`. Sync `Build` / `Decode` throws. Lookup methods expose a `namespacePrefix` parameter that remains reserved (unused for matching). See
`IPackageMetadataStore` remarks.

For ASP.NET Core (scoped breadcrumbs + automatic recording), use `Lyo.Diagnostic.AspNetCore` and `UseDiagnosticExceptionRecording` / `AddLyoDiagnosticsWeb`.

## PII and breadcrumbs

Call `IBreadcrumbTrail.Add` at meaningful steps (downstream call started, cache miss, etc.). Keep `Data` and `Message` values small. Avoid tokens, secrets, full query strings, emails, and raw URLs. Prefer coarse categories and opaque IDs. Optional `IBreadcrumbRedactor` can strip known keys on each add.

## Limits of the in-memory inbox

When over `MaxOccurrences`, `InMemoryErrorInbox` drops the oldest occurrences. Data is lost on restart and is not visible across multiple server processes. For durable aggregation, implement `IErrorInboxReader` / `IErrorOccurrenceSink` with Postgres or an external product such as Sentry.

## Fingerprint

Only user stack-frame method names enter the fingerprint hash — line numbers do not. Minor edits often keep the same key when the defect shape is unchanged. `StackTraceDecoder` has the details.

## Related / deferred work

- An inbox on Postgres that implements the same interfaces.
- HTTP export endpoints for support tooling.
- A dedicated Serilog enricher package.

## Developing

Unit tests live in `Lyo.PackageMetadata.Tests` and `Lyo.Diagnostic.Tests`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Hashing` (direct, lyo)
- `Lyo.PackageMetadata` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft)