# Lyo.Reporting.Tests

Unit tests for builders/mappers and Testcontainers integration tests for generate, guards, providers, cascade, and consumer FileStorage hooks.

```bash
dotnet build Integration/Reporting/Lyo.Reporting.Tests
./Integration/Reporting/Lyo.Reporting.Tests/bin/Debug/net10.0/Lyo.Reporting.Tests

# unit tests only (no Docker):
./Integration/Reporting/Lyo.Reporting.Tests/bin/Debug/net10.0/Lyo.Reporting.Tests -notrait "Category=Integration"
```

## Coverage highlights

- **Validation** (`ReportParameterValidatorTests`): typed value checks per `ReportParameterType`, regex timeout/invalid-pattern/length-cap handling, unknown-key rejection when
  generating from a definition, required satisfied by `EncryptedValue` alone.
- **Write-time validation** (`ReportDefinitionWriteValidatorTests`): composition JSON parse/size, `DefaultFormat`/`Type` enum checks, regex compile, min/max coherence.
- **Input hygiene & resilience** (`ReportServiceUnitTests`, `ReportGenerationHardenTests`, `ReportFeatureTests`): filename sanitization (traversal, invalid chars, length cap),
  malformed `ReportDataJson` fails fast without a persisted row, `AllowAdHocGeneration=false` behavior, Failed-status persistence, multi-value `ParametersJson` arrays.
- **Concurrency** (`ReportGenerationThrottleTests`): `MaxConcurrentGenerations` throttle saturation → `ReportBusyException`, release/recover, options validation.
- **Sensitive fields** (`ReportingApiOptionsTests`): secure-by-default auth surfaces, `DeniedSelectFieldPolicy` blocking `EncryptedValue` selects/templates on QueryProject and
  Export (incl. nested paths).
- **Features** (`ReportRendererTests`, `ReportFeatureTests`): Xlsx multi-worksheet round-trips with sheet-name dedupe/truncation, Json renderer verbatim output, `RerunAsync`
  snapshot replay, retention cleanup keeps in-flight/recent rows and invokes `OnCleanupAsync`, download stream factory round-trip via `FakeFileStorageService`, generation-delete / definition-delete
  cleanup hooks (`ReportGenerationCleanupTests`).

## Worker → API auth smoke (manual)

Workers must not host `ReportService`. Verify:

1. API host calls `AddLyoApiReporting()` + `BuildReportingGroup(new ReportingApiOptions { GenerateAuth = EndpointAuth.RequireAuthorization("ReportingGenerate"), ... })`.
2. Worker registers `AddReportingClient<TApiClient>()` with a bearer that satisfies `ReportingGenerate`.
3. `Generations.GenerateAsync` returns `ReportGenerationRes`; anonymous Generate is rejected (401/403).

See [`Lyo.Api.Reporting` README](../../Api/Lyo.Api.Reporting/README.md).
