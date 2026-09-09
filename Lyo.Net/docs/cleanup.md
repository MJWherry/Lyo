# Cleanup playbook

How to clean this solution without deleting public NuGet features or rewriting 365 projects in one pass. Analysis commands run from the **repo root**.

## Measure first

- `python3 Lyo.Net/scripts/analysis/clone_report.py` — duplication (type-1 literal, type-2 renamed identifiers). Baseline: `Lyo.Net/scripts/analysis/baselines/clone_report.json`.
- `python3 Lyo.Net/scripts/analysis/dep_closure.py '*' --save Lyo.Net/scripts/analysis/baselines/dep_closure_pre.json` — per-TFM project+package closure. After a refactor, `--baseline` that file.
- `python3 Lyo.Net/scripts/analysis/narrow_common.py --dry-run` — Lyo.Common family ProjectReferences vs namespaces the **project's sources** actually name. Safe to apply; it does not delete types.

## Do not merge

Clone clusters that look huge but are not worth sharing: `Constants` error-code classes, QR ISO tables, AwsPolly voice IDs, Mathematics quantity types, Razor DataGrid attribute soup, compression-codec `Extensions.cs` (one package per codec on purpose).

## Do not delete

Public types, `AddX` methods, options, and packable projects with **zero in-tree callers**. Hosts that consume these packages live outside this repo. Zero-inbound examples that stay: `Lyo.Stt`, `Lyo.Tts.WindowsSpeech`, `Lyo.Translation.Google`, `Lyo.Scheduler.Cache`, `Lyo.Pdf.Ocr`, `Lyo.Google.Geolocation.Client`, `Lyo.Config.Api.Hosting`.

`[Obsolete]` shims in `Lyo.Api.Client` remain until an explicit NuGet **major**. Do not delete them as unused in-tree. Catalog (all `error: false`):

| Shim | Replacement |
| --- | --- |
| `LyoHttpClientHandler` | `Lyo.Http.Client.LyoHttpClientHandler` |
| `ApiRequestCompressionType` | `Lyo.Http.Client.LyoHttpRequestCompressionType` |
| `UseLyoHttpClientHandler` / `UseLyoHttpClientHandler<TOptions>` | `Lyo.Http.Client.Extensions.UseLyoHttpClientHandler` |
| `ApplyAcceptEncodingHeaders` | `Lyo.Http.Client.LyoHttpClient.ApplyAcceptEncodingHeaders` |
| vendor `AddLyoApiClient<TClient, TOptions>(...)` overloads in `VendorClientServiceCollectionExtensions` | `AddLyoHttpClient<TClient, TOptions>(...)` returning `IHttpClientBuilder` |

IDE0051 / unused-member analyzers stay off.

Migration `#pragma warning disable 612, 618` in EF snapshots and CS8669 on the four Razor projects (`dotnet/razor#8720`) are not analyzer debt. Hashing CA535x (MD5 fingerprints) and Exceptions CS8777 stay. SDK analyzers (`EnableNETAnalyzers`, `AnalysisLevel=latest`) run at warning; `CodeAnalysisTreatWarningsAsErrors` is false so they do not fail the two `TreatWarningsAsErrors` packages. Do not set `EnforceCodeStyleInBuild`.

`Directory.Build.props` always suppresses CS1591 (it is imported before the csproj). Set `LyoRequireXmlDocs=true` on a package to peel CS1591 in `Directory.Build.targets` after the project file is read. Pilot: `Lyo.Exceptions` (`TreatWarningsAsErrors` plus required XML docs). `Lyo.Geolocation.Models` and `Lyo.Authentication.Models` already treat warnings as errors but keep CS1591 suppressed — their public DTOs are not fully documented, and filling every member is out of scope.

Twilio / `WebhookCrypto` HMAC-SHA1 is the vendor signature contract (`SYSLIB0021` suppressed on `Lyo.Webhook` and `Lyo.Webhook.Twilio`).

## Taxonomy

`Lyo.Webhook` / `Lyo.Webhook.Twilio` live under `Communication/Webhook/` (`AssemblyName` / `PackageId` unchanged). Do not move Communication or Security providers into `Integration/{Vendor}/`.

`Lyo.Web.Primitives` stays in `Core/Web/` as platform-in-Core.

Core → Security crypto is an allowed exception: `Lyo.Cache` / `Lyo.Cache.Fusion` → `Lyo.Encryption`; `Lyo.Diagnostic` / `Lyo.Privacy` / `Lyo.PackageMetadata` → `Lyo.Hashing`. Do not delete those references.

## EF

Do not change entities, Fluent mappings, or migrations as a side effect of DI, tests, or UI cleanup.

## Docs

`docs.json` is the source of truth. Render a subset: `python3 scripts/docs/project-docs.py render Lyo.Email`. Never run `extract`.
