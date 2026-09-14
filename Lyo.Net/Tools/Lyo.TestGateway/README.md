# Lyo.TestGateway

Blazor Server workbench for the Lyo platform. It hosts about 30 routed test pages (cache, locks, file storage, PDF, and more) and a thin proxy layer so every page can run against a remote API (`Lyo.Gateway.Api` by default via `ApiClient` / `LyoAuthClient`, or `Lyo.TestApi` for kitchen-sink) or against in-process services registered the same way as a real host.

## How the host is built

- Logging, `IHttpContextAccessor`, anti-forgery, HTTPS redirect, and status-code re-execution to `/not-found`.
- `MapStaticAssets()` + `MapRazorComponents<App>().AddInteractiveServerRenderMode()` for the Blazor app.
- A SignalR hub with `MaximumReceiveMessageSize = 32 MiB` so the PDF annotator can round-trip large iframe HTML via JS interop.
- A server-side minimal-API route (see [Proxy routes](#proxy-routes)) mapped before the Blazor app.

## Routed Blazor pages

Each workbench page lives under `Components/Pages/` and uses `@attribute [Route("/" + Constants.Page.X)]` so route strings are taken from `Lyo.TestGateway.Constants.Page`.

| Route | Page | Backed by |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `/` | `Home` | `AuthorizedPage` placeholder |
| `/PeopleManagement` | `People/PeopleManagement` | `Lyo.Api.Client` aimed at Gateway.Api `Person` CRUD |
| `/query-builder` | `QueryBuilderExample` | `Lyo.Query.Web.Components` |
| `/id-generator` | `IdGeneratorTest` | `Lyo.Web.Components` |
| `/messaging`, `/translation`, `/tts`, `/profanity` | Sms/Email, Translate, TTS, Profanity | `Lyo.Email`, `Lyo.Sms.Twilio`, `Lyo.Translation.Aws`, `Lyo.Tts.Typecast`, `Lyo.Profanity` |
| `/csv-xlsx` (legacy `/csv`, `/xlsx`) | `CsvTest` (one workbench, two tabs) | `Lyo.Csv`, `Lyo.Xlsx` |
| `/file-service` | `FileToolsTest` | In-process compression and encryption demos |
| `/filestorage-workbench` | `FileStorageWorkbenchPage` | Remote API via `IApiClient`. Hosts Files (stream/direct/staged/multipart upload + crypto) and Browser (metadata + expected storage grids). This UI has no keystore CRUD. |
| `/keystore-workbench` | `KeyStoreWorkbenchPage` | In-process `IKeyStore` (`AddLocalKeyStore`). No HTTP involved. |
| `/html-to-pdf` | `HtmlToPdfTest` | `Lyo.Web.WebRenderer` + `Lyo.Pdf` |
| `/pdf-annotator` | `PdfAnnotationTest` | `Lyo.Pdf.Web.Components.PdfAnnotator` |
| `/qr-code-generator`, `/barcode-generator` | `QrCodeTest`, `BarcodeTest` | `Lyo.QRCode`, `Lyo.Barcode.Native` |
| `/spritesheet-animator` | `SpriteSheetTest` | `Lyo.Images` sprite-sheet export |
| `/image-workbench` | `ImageTest` | `Lyo.Images` (ImageSharp) |
| `/text-diff` | `TextDiffTest` | `Lyo.Web.Components` diff viewer |
| `/formatter` | `FormatterTest` | `Lyo.Formatter.Web.Components` live template editor plus annotated preview (`AddFormatterService()`) |
| `/rich-text-editor` | `RichTextEditorTest` | `Lyo.Web.Components` editor |
| `/cache`, `/locks`, `/rabbitmq`, `/metrics`, `/schedule`, `/diagnostics`, `/jobs`, `/drift`, `/reports`, `/report-design`, `/config`, `/privacy-redaction`, `/seed` | Infra workbenches | `Lyo.Cache`, `Lyo.Lock`, `Lyo.MessageQueue.RabbitMq.Web.Components`, `Lyo.Metrics`, `Lyo.Schedule.Web.Components`, `Lyo.Diagnostic.Web.Components`, `Lyo.Job.Web.Components`, `Lyo.Drift.Web.Components`, `Lyo.Reporting.Web.Components` (`ReportManagement` + `ReportDesignWorkbench`; Seed posts people plus designer report templates), `Lyo.Config.Web.Components`, `Lyo.Privacy.Web.Components` |

Constants live in `Lyo.TestGateway.Constants.Page` (workbench routes) and `Lyo.TestGateway.Models.Constants` (Person/FileStorageWorkbench API routes).

## Proxied routes

Downloads open the API `GET {ApiRoutePrefix}/files/{id}/download` URL. The Gateway has no download proxy.

## File-storage workbench setup

- Files and Browser call the remote API (`ApiClient:BaseUrl`) through `IApiClient` using `ApiRoutePrefix` (defaults to `Workbench/FileStorage`).
- `AddFileStorageWorkbenchSupport` reads `FileStorageWebOptions` from the `FileStorageWorkbench` section (`ApiRoutePrefix`, `StreamUploadRelativePath`).
- `AddLocalKeyStore()` feeds the in-process `/keystore-workbench` page (`Lyo.KeyStore.Web.Components`). That store is not TestApi's encryption KEK.

## Other `Program.cs` registrations

- Infra: `AddCsvService`, `AddXlsxService`, `AddCompressionService` + `AddDefaultCompressionService<CompressionService>` (`ICompressionResolver` included), `AddLyoMetricsWithOpenTelemetryFromConfiguration` (`OpenTelemetry` section: `ServiceName` `Lyo.TestGateway`; OTLP endpoint from env when set), `AddScheduler`, `AddLocalCacheFromConfiguration`, `AddLocalLock(enableMetrics)`, `AddLocalKeyedSemaphore(enableMetrics)`, `AddImageSharpImageServiceFromConfiguration`, `AddPdfService`, `AddSpriteSheetExportService`, `AddPdfAnnotatorInterop`.
- Communication: `AddEmailServiceFromConfiguration`, `AddTwilioSmsServiceFromConfiguration`, `SetupRabbitMqServiceFromConfiguration`, `AddAwsTranslationServiceFromConfiguration`, `AddProfanityFilterServiceFromConfiguration`, Typecast client + TTS service, `AddQRCodeServiceFromConfiguration`, `AddNativeBarcodeServiceFromConfiguration`.
- Web: `AddWebRendererServiceFromConfiguration`, `AddBlazoredLocalStorage`, `AddMudServices(...)`, `IIOTempService` rooted at `lyo-gateway-uploads`, `TestGatewayFileTransformer` on the file-tools workbench.
- API client: `Configure<ApiClientOptions>(…ApiClientOptions.SectionName)`, `AddLyoApiClient`.
- File workbench: `AddFileStorageWorkbenchSupport(builder.Configuration)`. Key store: `AddLocalKeyStore()`.
- Config workbench: `AddConfigApiStore()` (`IConfigStore` → TestApi manage routes). Encryption remains on TestApi.
- Seed page: `AddLyoSeed()` plus `ApiSeedTransport` against `POST Person/Bulk` (People API) and `POST Reporting/Definition/Bulk` (designer templates: controls gallery, sales summary, invoice, operations dashboard).

## Config sections

`appsettings.json` includes placeholders for each section the host binds:

| Section | Used by |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------- |
| `ApiClient` | `Lyo.Api.Client` (`BaseUrl` → TestApi, typically `http://localhost:5251/`). Config workbench writes and query grids go through this client. |
| `LyoAuthClient` | `AuthBaseUrl` → the same Gateway.Api host for OIDC BFF handoff / refresh / logout |
| `FileStorageWorkbench` | `AddFileStorageWorkbenchSupport` (`ApiRoutePrefix`, `StreamUploadRelativePath`) |
| `AwsKeyStore` | Unused by TestGateway after in-process S3 was removed; TestApi still uses it for file encryption |
| `AddConfigApiStore` | `IConfigStore` over TestApi manage routes for the `/config` workbench. Encryption happens on TestApi. |
| `AwsTranslationOptions` | `AddAwsTranslationServiceFromConfiguration` |
| `TypecastClient`, `TypecastOptions` | Typecast TTS workbench |
| `EmailServiceOptions` | SMTP-based `Lyo.Email` |
| `TwilioOptions` | `Lyo.Sms.Twilio` |
| `RabbitMqOptions` | `Lyo.MessageQueue.RabbitMq` |
| `WebRenderOptions` | `Lyo.Web.WebRenderer` (HTML → PDF) |
| `CacheOptions` | `AddLocalCacheFromConfiguration` |
| `JobDashboard` | `Lyo.Job.Web.Components` Jobs page |
| `DriftDashboard` | `Lyo.Drift.Web.Components` Drift page (`BaseApiUrl` → TestApi `/Drift`) |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` (direct, lyo)
- `Lyo.Authentication.Client` (direct, lyo)
- `Lyo.Authentication.Web.Components` (direct, lyo)
- `Lyo.Authentication.Web.Components.Server` (direct, lyo)
- `Lyo.Barcode.Native` (direct, lyo)
- `Lyo.Barcode.TestWorkbench.Web.Components` (direct, lyo)
- `Lyo.Cache` (direct, lyo)
- `Lyo.Common.Json` (direct, lyo)
- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Compression` (direct, lyo)
- `Lyo.Compression.BZip2` (direct, lyo)
- `Lyo.Compression.Lz4` (direct, lyo)
- `Lyo.Compression.Lzma` (direct, lyo)
- `Lyo.Compression.Snappier` (direct, lyo)
- `Lyo.Compression.Xz` (direct, lyo)
- `Lyo.Compression.Zstd` (direct, lyo)
- `Lyo.Config.Api.Client` (direct, lyo)
- `Lyo.Config.Web.Components` (direct, lyo)
- `Lyo.Csv` (direct, lyo)
- `Lyo.Diagnostic.Web.Components` (direct, lyo)
- `Lyo.Drift.Web.Components` (direct, lyo)
- `Lyo.Email` (direct, lyo)
- `Lyo.Email.Web.Components` (direct, lyo)
- `Lyo.Encryption` (direct, lyo)
- `Lyo.Encryption.AesCcm` (direct, lyo)
- `Lyo.Encryption.AesSiv` (direct, lyo)
- `Lyo.Encryption.XChaCha20Poly1305` (direct, lyo)
- `Lyo.Endato.Client` (direct, lyo)
- `Lyo.Endato.Web.Components` (direct, lyo)
- `Lyo.FileMetadataStore` (direct, lyo)
- `Lyo.FileStorage.Web.Components` (direct, lyo)
- `Lyo.Formatter` (direct, lyo)
- `Lyo.Formatter.Web.Components` (direct, lyo)
- `Lyo.Hashing` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `Lyo.IO.Temp` (direct, lyo)
- `Lyo.Images` (direct, lyo)
- `Lyo.Images.Web.Components` (direct, lyo)
- `Lyo.Job.Web.Components` (direct, lyo)
- `Lyo.KeyStore` (direct, lyo)
- `Lyo.KeyStore.Aws` (direct, lyo)
- `Lyo.KeyStore.Web.Components` (direct, lyo)
- `Lyo.Lock` (direct, lyo)
- `Lyo.MessageQueue` (direct, lyo)
- `Lyo.MessageQueue.RabbitMq` (direct, lyo)
- `Lyo.MessageQueue.RabbitMq.Web.Components` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Metrics.OpenTelemetry` (direct, lyo)
- `Lyo.Pdf` (direct, lyo)
- `Lyo.Pdf.Web.Components` (direct, lyo)
- `Lyo.People.Models` (direct, lyo)
- `Lyo.Privacy.Web.Components` (direct, lyo)
- `Lyo.Profanity` (direct, lyo)
- `Lyo.QRCode` (direct, lyo)
- `Lyo.QRCode.Web.Components` (direct, lyo)
- `Lyo.Query.Web.Components` (direct, lyo)
- `Lyo.Reporting.Business.Example` (direct, lyo)
- `Lyo.Reporting.Models` (direct, lyo)
- `Lyo.Reporting.Web` (direct, lyo)
- `Lyo.Reporting.Web.Components` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Lyo.Schedule.Web.Components` (direct, lyo)
- `Lyo.Scheduler` (direct, lyo)
- `Lyo.Seed` (direct, lyo)
- `Lyo.Sms.Twilio` (direct, lyo)
- `Lyo.Sms.Web.Components` (direct, lyo)
- `Lyo.Tag` (direct, lyo)
- `Lyo.Translation.Aws` (direct, lyo)
- `Lyo.Translation.Web.Components` (direct, lyo)
- `Lyo.Tts.AwsPolly.Web.Components` (direct, lyo)
- `Lyo.Tts.Typecast` (direct, lyo)
- `Lyo.Tts.Typecast.Web.Components` (direct, lyo)
- `Lyo.Web.Components` (direct, lyo)
- `Lyo.Web.Components.Export` (direct, lyo)
- `Lyo.Web.Components.Export.Csv` (direct, lyo)
- `Lyo.Web.Components.Export.Xlsx` (direct, lyo)
- `Lyo.Web.Host` (direct, lyo)
- `Lyo.Web.WebRenderer` (direct, lyo)
- `Lyo.Xlsx` (direct, lyo)
- `Blazored.LocalStorage` `4.5.0` (direct, third-party)
- `Bogus` `35.6.5` (direct, third-party)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Api.FileStorage.Models` (transitive, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Authentication.Models` (transitive, lyo)
- `Lyo.Barcode` (transitive, lyo)
- `Lyo.Barcode.Web.Components` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Config` (transitive, lyo)
- `Lyo.Config.Api.Models` (transitive, lyo)
- `Lyo.Csv.Models` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Diagnostic.AspNetCore` (transitive, lyo)
- `Lyo.Drift.Models` (transitive, lyo)
- `Lyo.Email.Models` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.FileSystemWatcher.Models` (transitive, lyo)
- `Lyo.Geolocation.Models` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.IO.FileSystem` (transitive, lyo)
- `Lyo.Job.Models` (transitive, lyo)
- `Lyo.MessageQueue.Web.Components` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Pdf.Models` (transitive, lyo)
- `Lyo.Privacy` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Reporting.Client` (transitive, lyo)
- `Lyo.Schedule.Models` (transitive, lyo)
- `Lyo.Sms` (transitive, lyo)
- `Lyo.Sms.Models` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.SystemInformation` (transitive, lyo)
- `Lyo.Translation` (transitive, lyo)
- `Lyo.Tts` (transitive, lyo)
- `Lyo.Tts.AwsPolly` (transitive, lyo)
- `Lyo.Tts.Models` (transitive, lyo)
- `Lyo.Typecast.Client` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `Lyo.Web.Primitives` (transitive, lyo)
- `Lyo.Xlsx.Models` (transitive, lyo)
- `AWSSDK.Polly` `4.0.100.3` (transitive, third-party)
- `AWSSDK.SecretsManager` `4.0.100.3` (transitive, third-party)
- `AWSSDK.Translate` `4.0.100.3` (transitive, third-party)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `ClosedXML` `0.105.0` (transitive, third-party)
- `DocumentFormat.OpenXml` `3.1.1` (transitive, third-party)
- `Dorssel.Security.Cryptography.AesExtra` `2.0.0` (transitive, third-party)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `EasyCompressor.LZ4` `2.1.0` (transitive, third-party)
- `EasyCompressor.LZMA` `2.1.0` (transitive, third-party)
- `EasyCompressor.Snappier` `2.1.0` (transitive, third-party)
- `EasyCompressor.ZstdSharp` `2.1.0` (transitive, third-party)
- `ExcelDataReader` `3.9.0` (transitive, third-party)
- `ExcelDataReader.DataSet` `3.9.0` (transitive, third-party)
- `Joveler.Compression.XZ` `5.0.2` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `MailKit` `4.17.0` (transitive, third-party)
- `Microsoft.AspNetCore.Components.Web` `10.0.5` (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.DataAnnotations` `10.0.5` (transitive, microsoft)
- `OpenTelemetry` `1.16.0` (transitive, third-party)
- `OpenTelemetry.Exporter.Console` `1.16.0` (transitive, third-party)
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` `1.16.0` (transitive, third-party)
- `OpenTelemetry.Extensions.Hosting` `1.16.0` (transitive, third-party)
- `PDFsharp` `6.2.4` (transitive, third-party)
- `PdfPig` `0.1.15` (transitive, third-party)
- `PuppeteerSharp` `24.0.0` (transitive, third-party)
- `RabbitMQ.Client` `7.2.1` (transitive, third-party)
- `SharpZipLib` `1.4.2` (transitive, third-party)
- `SixLabors.Fonts` `2.1.3` (transitive, third-party)
- `SixLabors.ImageSharp` `3.1.12` (transitive, third-party)
- `SixLabors.ImageSharp.Drawing` `2.1.7` (transitive, third-party)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.Collections.Immutable` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.Diagnostics.DiagnosticSource` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)
- `Twilio` `7.14.9` (transitive, third-party)
- `ZXing.Net` `0.16.11` (transitive, third-party)
- `ZXing.Net.Bindings.ImageSharp.V3` `0.16.18` (transitive, third-party)