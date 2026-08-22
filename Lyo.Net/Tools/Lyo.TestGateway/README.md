# Lyo.TestGateway

Interactive Blazor Server workbench for the Lyo platform. It hosts about 30 routed test pages (cache, locks, file storage, PDF, and more) and a thin proxy layer so every page can run against a remote API (`Lyo.Gateway.Api` by default via `ApiClient` / `LyoAuthClient`, or `Lyo.TestApi` for kitchen-sink) or against in-process services registered the same way as a real host.

## Hosting model

- Logging, `IHttpContextAccessor`, anti-forgery, HTTPS redirect, status-code re-execution to `/not-found`.
- `MapStaticAssets()` + `MapRazorComponents<App>().AddInteractiveServerRenderMode()` for the Blazor app.
- A SignalR hub with `MaximumReceiveMessageSize = 32 MiB` so the PDF annotator can round-trip large iframe HTML through JS interop.
- A server-side minimal-API route (see [Proxy routes](#proxy-routes)) registered before the Blazor app.

## Routed pages

Every workbench page lives under `Components/Pages/` and uses `@attribute [Route("/" + Constants.Page.X)]` so route strings come from `Lyo.TestGateway.Constants.Page`.

| Route | Page | Backed by |
| ------------------------------------------------------------------------------------------------------------------ | -------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `/` | `Home` | `AuthorizedPage` placeholder |
| `/PeopleManagement` | `People/PeopleManagement` | `Lyo.Api.Client` against Gateway.Api `Person` CRUD |
| `/query-builder` | `QueryBuilderExample` | `Lyo.Query.Web.Components` |
| `/id-generator` | `IdGeneratorTest` | `Lyo.Web.Components` |
| `/messaging`, `/translation`, `/tts`, `/profanity` | Sms/Email, Translate, TTS, Profanity | `Lyo.Email`, `Lyo.Sms.Twilio`, `Lyo.Translation.Aws`, `Lyo.Tts.Typecast`, `Lyo.Profanity` |
| `/csv-xlsx` (legacy `/csv`, `/xlsx`) | `CsvTest` (single workbench, two tabs) | `Lyo.Csv`, `Lyo.Xlsx` |
| `/file-service` | `FileToolsTest` | In-process compression + encryption demos |
| `/filestorage-workbench` | `FileStorageWorkbenchPage` | Remote API via `IApiClient`. Hosts Files (stream/direct/staged/multipart upload + crypto) and Browser (metadata + expected storage grids). Keystore CRUD is not in this UI. |
| `/keystore-workbench` | `KeyStoreWorkbenchPage` | In-process `IKeyStore` (`AddLocalKeyStore`). No HTTP. |
| `/html-to-pdf` | `HtmlToPdfTest` | `Lyo.Web.WebRenderer` + `Lyo.Pdf` |
| `/pdf-annotator` | `PdfAnnotationTest` | `Lyo.Pdf.Web.Components.PdfAnnotator` |
| `/qr-code-generator`, `/barcode-generator` | `QrCodeTest`, `BarcodeTest` | `Lyo.QRCode`, `Lyo.Barcode.Native` |
| `/spritesheet-animator` | `SpriteSheetTest` | `Lyo.Images` sprite sheet export |
| `/image-workbench` | `ImageTest` | `Lyo.Images` (ImageSharp) |
| `/text-diff` | `TextDiffTest` | `Lyo.Web.Components` diff viewer |
| `/formatter` | `FormatterTest` | `Lyo.Formatter.Web.Components` live template editor + annotated preview (`AddFormatterService()`) |
| `/rich-text-editor` | `RichTextEditorTest` | `Lyo.Web.Components` editor |
| `/cache`, `/locks`, `/rabbitmq`, `/metrics`, `/schedule`, `/diagnostics`, `/jobs`, `/config`, `/privacy-redaction` | Infrastructure workbenches | `Lyo.Cache`, `Lyo.Lock`, `Lyo.MessageQueue.RabbitMq.Web.Components`, `Lyo.Metrics`, `Lyo.Schedule.Web.Components`, `Lyo.Diagnostic.Web.Components`, `Lyo.Job.Web.Components`, `Lyo.Config.Web.Components`, `Lyo.Privacy.Web.Components` |

Constants are defined in `Lyo.TestGateway.Constants.Page` (workbench routes) and `Lyo.TestGateway.Models.Constants` (Person/FileStorageWorkbench API routes).

## Proxy routes

Downloads open the API `GET {ApiRoutePrefix}/files/{id}/download` URL. There is no Gateway download proxy.

## File storage workbench wiring

- Files and Browser call the remote API (`ApiClient:BaseUrl`) through `IApiClient` using `ApiRoutePrefix` (default `Workbench/FileStorage`).
- `AddFileStorageWorkbenchSupport` binds `FileStorageWebOptions` from the `FileStorageWorkbench` section (`ApiRoutePrefix`, `StreamUploadRelativePath`).
- `AddLocalKeyStore()` feeds the in-process `/keystore-workbench` page (`Lyo.KeyStore.Web.Components`). That store is not the TestApi encryption KEK.

## Other services in `Program.cs`

- Infra: `AddCsvService`, `AddXlsxService`, `AddCompressionService` + `AddDefaultCompressionService<CompressionService>` (`ICompressionResolver` included), `AddLyoMetrics`, `AddScheduler`, `AddLocalCacheFromConfiguration`, `AddLocalLock(enableMetrics)`, `AddLocalKeyedSemaphore(enableMetrics)`, `AddImageSharpImageServiceFromConfiguration`, `AddPdfService`, `AddSpriteSheetExportService`, `AddPdfAnnotatorInterop`.
- Communication: `AddEmailServiceFromConfiguration`, `AddTwilioSmsServiceFromConfiguration`, `SetupRabbitMqServiceFromConfiguration`, `AddAwsTranslationServiceFromConfiguration`, `AddProfanityFilterServiceFromConfiguration`, Typecast client + TTS service, `AddQRCodeServiceFromConfiguration`, `AddNativeBarcodeServiceFromConfiguration`.
- Web: `AddWebRendererServiceFromConfiguration`, `AddBlazoredLocalStorage`, `AddMudServices(...)`, `IIOTempService` rooted at `lyo-gateway-uploads`, `TestGatewayFileTransformer` for the file-tools workbench.
- API client: `Configure<ApiClientOptions>(…ApiClientOptions.SectionName)`, `AddLyoApiClient`.
- File workbench: `AddFileStorageWorkbenchSupport(builder.Configuration)`. Key store: `AddLocalKeyStore()`.
- Config workbench: `AddPostgresConfigStoreFromConfiguration(builder.Configuration)` (`IConfigStore` for `/config`).

## Configuration sections

`appsettings.json` ships placeholders for every section the host binds:

| Section | Used by |
| ----------------------------------- | ------------------------------------------------------------------------------------------------ |
| `ApiClient` | `Lyo.Api.Client` (`BaseUrl` → `Lyo.Gateway.Api`, typically `http://localhost:5251/`) |
| `LyoAuthClient` | `AuthBaseUrl` → same Gateway.Api host for OIDC BFF handoff / refresh / logout |
| `FileStorageWorkbench` | `AddFileStorageWorkbenchSupport` (`ApiRoutePrefix`, `StreamUploadRelativePath`) |
| `AwsKeyStore` | Unused by TestGateway after in-process S3 was dropped; TestApi still uses it for file encryption |
| `PostgresConfig` | `AddPostgresConfigStoreFromConfiguration` for the `/config` workbench (`IConfigStore`) |
| `AwsTranslationOptions` | `AddAwsTranslationServiceFromConfiguration` |
| `TypecastClient`, `TypecastOptions` | Typecast TTS workbench |
| `EmailServiceOptions` | SMTP-based `Lyo.Email` |
| `TwilioOptions` | `Lyo.Sms.Twilio` |
| `RabbitMqOptions` | `Lyo.MessageQueue.RabbitMq` |
| `WebRenderOptions` | `Lyo.Web.WebRenderer` (HTML → PDF) |
| `CacheOptions` | `AddLocalCacheFromConfiguration` |
| `JobDashboard` | `Lyo.Job.Web.Components` Jobs page |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` (direct, lyo)
- `Lyo.Authentication.Client` (direct, lyo)
- `Lyo.Authentication.Web.Components` (direct, lyo)
- `Lyo.Authentication.Web.Components.Server` (direct, lyo)
- `Lyo.Barcode.Native` (direct, lyo)
- `Lyo.Barcode.TestWorkbench.Web.Components` (direct, lyo)
- `Lyo.Cache` (direct, lyo)
- `Lyo.Common` (direct, lyo)
- `Lyo.Compression` (direct, lyo)
- `Lyo.Compression.BZip2` (direct, lyo)
- `Lyo.Compression.Lz4` (direct, lyo)
- `Lyo.Compression.Lzma` (direct, lyo)
- `Lyo.Compression.Snappier` (direct, lyo)
- `Lyo.Compression.Xz` (direct, lyo)
- `Lyo.Compression.Zstd` (direct, lyo)
- `Lyo.Config.Postgres` (direct, lyo)
- `Lyo.Config.Web.Components` (direct, lyo)
- `Lyo.Csv` (direct, lyo)
- `Lyo.Diagnostic.Web.Components` (direct, lyo)
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
- `Lyo.Pdf` (direct, lyo)
- `Lyo.Pdf.Web.Components` (direct, lyo)
- `Lyo.People.Models` (direct, lyo)
- `Lyo.Privacy.Web.Components` (direct, lyo)
- `Lyo.Profanity` (direct, lyo)
- `Lyo.QRCode` (direct, lyo)
- `Lyo.QRCode.Web.Components` (direct, lyo)
- `Lyo.Query.Web.Components` (direct, lyo)
- `Lyo.Reporting.Web.Components` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Lyo.Schedule.Web.Components` (direct, lyo)
- `Lyo.Scheduler` (direct, lyo)
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
- `Lyo.Web.WebRenderer` (direct, lyo)
- `Lyo.Xlsx` (direct, lyo)
- `Blazored.LocalStorage` `4.5.0` (direct, third-party)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Api.FileStorage.Models` (transitive, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Authentication.Models` (transitive, lyo)
- `Lyo.Barcode` (transitive, lyo)
- `Lyo.Barcode.Web.Components` (transitive, lyo)
- `Lyo.Config` (transitive, lyo)
- `Lyo.Csv.Models` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Diagnostic.AspNetCore` (transitive, lyo)
- `Lyo.Email.Models` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Lyo.EntityReference.Postgres` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Geolocation.Models` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.Job.Models` (transitive, lyo)
- `Lyo.MessageQueue.Web.Components` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Pdf.Models` (transitive, lyo)
- `Lyo.Postgres` (transitive, lyo)
- `Lyo.Privacy` (transitive, lyo)
- `Lyo.Query` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Reporting.Client` (transitive, lyo)
- `Lyo.Reporting.Models` (transitive, lyo)
- `Lyo.Schedule.Models` (transitive, lyo)
- `Lyo.Sms` (transitive, lyo)
- `Lyo.Sms.Models` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Translation` (transitive, lyo)
- `Lyo.Tts` (transitive, lyo)
- `Lyo.Tts.AwsPolly` (transitive, lyo)
- `Lyo.Tts.Models` (transitive, lyo)
- `Lyo.Typecast.Client` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Xlsx.Models` (transitive, lyo)
- `AWSSDK.Polly` `4.0.100.3` (transitive, third-party)
- `AWSSDK.SecretsManager` `4.0.100.3` (transitive, third-party)
- `AWSSDK.Translate` `4.0.100.3` (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `ClosedXML` `0.105.0` (transitive, third-party)
- `DocumentFormat.OpenXml` `3.1.1` (transitive, third-party)
- `Dorssel.Security.Cryptography.AesExtra` `2.0.0` (transitive, third-party)
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
- `Microsoft.AspNetCore.Components.Authorization` `10.0.5` (transitive, microsoft)
- `Microsoft.AspNetCore.Components.Web` `10.0.5` (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
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
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
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
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)
- `Twilio` `7.14.9` (transitive, third-party)
- `ZXing.Net` `0.16.11` (transitive, third-party)
- `ZXing.Net.Bindings.ImageSharp.V3` `0.16.18` (transitive, third-party)