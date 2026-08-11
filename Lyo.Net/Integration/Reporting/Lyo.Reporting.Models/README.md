# Lyo.Reporting.Models

Composition models, fluent builders, API contracts, and generation hooks for Lyo Reporting.

## Formats and routes

- `ReportFormat`: `Html`, `Pdf`, `Csv`, `Xlsx`, `Json`.
- `Constants.Rest.Reporting` route constants include `GenerationsGenerate` plus the `GenerationsDownloadSuffix` (`Download`) and `GenerationsRerunSuffix` (`Rerun`) segments used under `Reporting/Generation/{id}/…`.
- `Constants.Metrics` adds `reporting.generation.cleaned` for retention cleanup.

## Notes

- `ValueFormatter` delegates on columns are `[JsonIgnore]` and do not survive JSON persistence into `ReportDataJson`.
- HTML/PDF rendering lives in `Lyo.Reporting.Web`; CSV/XLSX/JSON rendering and orchestration live in `Lyo.Reporting.Postgres`.
- HTTP endpoints live in `Lyo.Api.Reporting`. Persist staged output in consumer `ReportGenerationHooks` (Reporting does not reference FileStorage); delete persisted output in `OnCleanupAsync` when rows are removed by retention or definition delete.
- `ReportDefinitionParameterReq.Options` — JSON picker source (static items or root `QueryReq`); `Value` remains the default/selected scalar. `AllowedValues` stays the pipe-separated validation shorthand.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Metrics` — (direct, lyo)
- `System.Text.Json` `10.0.5` — (direct, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)