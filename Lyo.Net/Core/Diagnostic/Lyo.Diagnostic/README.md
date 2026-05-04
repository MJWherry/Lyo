# Lyo.Diagnostic

Diagnostic utilities: stack trace decoding, exception classification, **breadcrumb trails**, an **in-memory error inbox**, sanitisation, and structured logging for observability.

Source is grouped by feature folder; namespaces match (e.g. `StackTrace/` → `Lyo.Diagnostic.StackTrace`). **`Lyo.Diagnostic`** (root) holds `AddDiagnosticsPackage` in
`Registration/`. Package metadata DTOs and **`IPackageMetadataStore`** live in **[`Lyo.PackageMetadata`](../PackageMetadata/Lyo.PackageMetadata)**.

| Folder            | Namespace                       |
|-------------------|---------------------------------|
| `StackTrace/`     | `Lyo.Diagnostic.StackTrace`     |
| `Classification/` | `Lyo.Diagnostic.Classification` |
| `Context/`        | `Lyo.Diagnostic.Context`        |
| `Breadcrumbs/`    | `Lyo.Diagnostic.Breadcrumbs`    |
| `Inbox/`          | `Lyo.Diagnostic.Inbox`          |
| `Logging/`        | `Lyo.Diagnostic.Logging`        |
| `Sanitisation/`   | `Lyo.Diagnostic.Sanitisation`   |
| `Registration/`   | `Lyo.Diagnostic`                |

## Core features

- **Stack trace decoding** (`IStackTraceDecoder`) — frames, crash site, fingerprint (stable hash of user-frame method signatures). Optional **`IPackageMetadataStore`** from **`Lyo.PackageMetadata`** enriches frames with **`PackageMetadata`** (`Id` + `Name`, …). With a configured store, **`DecodeAsync`** / **`IDiagnosticContextBuilder.BuildAsync`** perform **one** bulk metadata resolve for the entire exception/textual inner stack-tree (embedded inner blocks and chained **`InnerException`**), not once per subtree. Sync **`Decode`** / **`Build`** throw when a store is set.
- **Classification** (`IExceptionClassifier`) — kind, severity, labels.
- **Diagnostic context** (`IDiagnosticContextBuilder`) — single payload for one failure.
- **Structured logging** (`IStructuredLogEnricher`) — `ILogger` scopes with `diag.*` properties.
- **Trace sanitisation** (`ITraceSanitiser`) — redact paths/PII before logs or API responses.
- **Breadcrumbs** (`IBreadcrumbTrail`, `RingBufferBreadcrumbTrail`) — bounded FIFO trail of short events for triage (cap per scope, e.g. HTTP request).
- **Error inbox** (`IErrorOccurrenceSink`, `IErrorInboxReader`, `InMemoryErrorInbox`) — record and query grouped occurrences by fingerprint + exception kind + service (
  single-process; not shared across instances).

## Registering services

```csharp
using Lyo.Diagnostic;
using Lyo.Diagnostic.Inbox;

services.AddDiagnosticsPackage();
services.AddInMemoryErrorInbox(o => o.MaxOccurrences = 5_000);
```

### Optional package metadata (`IPackageMetadataStore`)

Use **`Lyo.PackageMetadata`** (`InMemoryPackageMetadataStore`, or **`Lyo.PackageMetadata.Postgres`**). Pass the store as the last argument to `AddDiagnosticsPackage`, or set `StackTraceDecoderOptions.PackageMetadataStore`. **`TryGetManyForStrippedMethodPrefixesAsync`** resolves many stripped methods at once behind that call; Postgres may cache the ordered prefix catalog in-process (**see `PostgresPackageMetadataOptions.PrefixCatalogCaching`** and **`ClearPrefixCatalogCache`**). When a store is configured, use **`DecodeAsync`** and **`BuildAsync`** — sync **`Decode`** / **`Build`** throws. Lookup methods expose a **`namespacePrefix`** parameter that remains **reserved (unused for matching)**; see **`IPackageMetadataStore`** remarks.

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

For ASP.NET Core (scoped breadcrumbs + automatic recording), use **`Lyo.Diagnostic.AspNetCore`** and `AddLyoDiagnosticsWeb` / `UseDiagnosticExceptionRecording`.

## Breadcrumbs and PII

Call `IBreadcrumbTrail.Add` at meaningful steps (cache miss, downstream call started, etc.). Keep **`Message` and `Data` values small** and **avoid secrets, tokens, full query
strings, emails, and raw URLs**. Prefer opaque IDs and coarse categories. Optional **`IBreadcrumbRedactor`** can strip known keys on each add.

## In-memory inbox limits

`InMemoryErrorInbox` drops **oldest** occurrences when over `MaxOccurrences`. Data is **lost on restart** and **is not visible across multiple server processes**. For production
aggregation, implement `IErrorOccurrenceSink` / `IErrorInboxReader` with Postgres or an external product (e.g. Sentry).

## Fingerprint

The fingerprint hashes **user** stack frame method names only (not line numbers), so the same defect shape often keeps the same key across minor edits. See `StackTraceDecoder`.

## Deferred / related work

- Postgres-backed inbox implementing the same interfaces.
- HTTP export endpoints for support tooling.
- Dedicated Serilog enricher package.

## Developing

Unit tests live in **`Lyo.Diagnostic.Tests`** and **`Lyo.PackageMetadata.Tests`**.
