# Lyo.Reporting.Models

Composition models, fluent builders, API contracts, and generation hooks for Lyo Reporting.

## Contents

- **Composition** — `Report<T>`, sections, columns, grids, content blocks (`Lyo.Reporting.Models`)
- **Builders** — `ReportBuilder<T>` and nested builders (`Lyo.Reporting.Builders`)
- **API** — `ReportDefinitionReq`/`Res`, `GenerateReportReq`, `ReportGenerationRes`, enums, `Constants.Rest`
- **Rendering** — `IReportRenderer`, `ReportGenerationHooks` (incl. `OnCleanupAsync` + `ReportCleanupContext`), generate contexts
- **Providers / profiles** — `IReportDataProvider`, `ReportingGenerationProfile` (API host)
- **Exceptions** — `ReportValidationException` (API maps to 400), `ReportBusyException` (API maps to 503)

Target frameworks: `netstandard2.0;net10.0`.

## Formats and routes

- `ReportFormat`: `Html`, `Pdf`, `Csv`, `Xlsx`, `Json`.
- `Constants.Rest.Reporting` route constants include `GenerationsGenerate` plus the `GenerationsDownloadSuffix` (`Download`) and `GenerationsRerunSuffix` (`Rerun`) segments used under `Reporting/Generation/{id}/…`.
- `Constants.Metrics` adds `reporting.generation.cleaned` for retention cleanup.

## Notes

- `ValueFormatter` delegates on columns are `[JsonIgnore]` and do not survive JSON persistence into `ReportDataJson`.
- HTML/PDF rendering lives in `Lyo.Reporting.Web`; CSV/XLSX/JSON rendering and orchestration live in `Lyo.Reporting.Postgres`.
- HTTP endpoints live in `Lyo.Api.Reporting`. Persist staged output in consumer `ReportGenerationHooks` (Reporting does not reference FileStorage); delete persisted output in `OnCleanupAsync` when rows are removed by retention or definition delete.
