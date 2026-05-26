# Lyo.Gateway

Interactive Blazor Server workbench for the Lyo platform. It hosts ~30 routed test pages (cache, locks, file storage, PDF, comics, etc.) and a thin proxy layer that lets every page
run against either the Test API (`Lyo.TestApi`) or against in-process services registered the same way as in production.

## Hosting model

`Program.cs` wires a single ASP.NET Core Blazor Server host:

- Logging, `IHttpContextAccessor`, anti-forgery, HTTPS redirect, status-code re-execution to `/not-found`.
- `MapStaticAssets()` + `MapRazorComponents<App>().AddInteractiveServerRenderMode()` for the Blazor app.
- A SignalR hub with `MaximumReceiveMessageSize = 32 MiB` so the PDF annotator can round-trip large iframe HTML through JS interop.
- Two server-side minimal-API routes (see [Proxy routes](#proxy-routes)) registered before the Blazor app.

The MudBlazor shell (`Components/Layout/MainLayout.razor`) shows a `MudDrawer` nav menu with grouped sections — Communication, Documents & Files, Infrastructure — plus a dark-mode
toggle persisted in browser local storage via `ClientStore` / `Blazored.LocalStorage`.

## Routed pages

Every workbench page lives under `Components/Pages/` and uses `@attribute [Route("/" + Constants.Page.X)]` so route strings come from `Lyo.Gateway.Constants.Page`. Highlights:

| Route                                                                                                   | Page                                   | Backed by                                                                                                                                                                                                  |
|---------------------------------------------------------------------------------------------------------|----------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `/`                                                                                                     | `Home`                                 | `AuthorizedPage` placeholder                                                                                                                                                                               |
| `/PeopleManagement`                                                                                     | `People/PeopleManagement`              | `Lyo.Api.Client` against the Test API `Person` CRUD                                                                                                                                                        |
| `/comics`, `/comics/series/{id}`, `/comics/volume/{id}`, `/comics/read/{id}`                            | `Comics*Page`                          | `Lyo.Comic.Api.Client`                                                                                                                                                                                     |
| `/query-builder`                                                                                        | `QueryBuilderExample`                  | `Lyo.Query.Web.Components`                                                                                                                                                                                 |
| `/id-generator`                                                                                         | `IdGeneratorTest`                      | `Lyo.Web.Components`                                                                                                                                                                                       |
| `/messaging`, `/translation`, `/tts`, `/profanity`                                                      | Sms/Email, Translate, TTS, Profanity   | `Lyo.Email`, `Lyo.Sms.Twilio`, `Lyo.Translation.Aws`, `Lyo.Tts.Typecast`, `Lyo.Profanity`                                                                                                                  |
| `/csv-xlsx` (legacy `/csv`, `/xlsx`)                                                                    | `CsvTest` (single workbench, two tabs) | `Lyo.Csv`, `Lyo.Xlsx`                                                                                                                                                                                      |
| `/file-service`                                                                                         | `FileToolsTest`                        | In-process compression + encryption demos                                                                                                                                                                  |
| `/filestorage-workbench`                                                                                | `FileStorageWorkbenchPage`             | Test API workbench routes via `TestApi*` proxy services (see below)                                                                                                                                        |
| `/html-to-pdf`                                                                                          | `HtmlToPdfTest`                        | `Lyo.Web.WebRenderer` + `Lyo.Pdf`                                                                                                                                                                          |
| `/pdf-annotator`                                                                                        | `PdfAnnotationTest`                    | `Lyo.Pdf.Web.Components.PdfAnnotator`                                                                                                                                                                      |
| `/qr-code-generator`, `/barcode-generator`                                                              | `QrCodeTest`, `BarcodeTest`            | `Lyo.QRCode`, `Lyo.Barcode.Native`                                                                                                                                                                         |
| `/spritesheet-animator`                                                                                 | `SpriteSheetTest`                      | `Lyo.Images` sprite sheet export                                                                                                                                                                           |
| `/image-workbench`                                                                                      | `ImageTest`                            | `Lyo.Images` (ImageSharp)                                                                                                                                                                                  |
| `/text-diff`                                                                                            | `TextDiffTest`                         | `Lyo.Web.Components` diff viewer                                                                                                                                                                           |
| `/rich-text-editor`                                                                                     | `RichTextEditorTest`                   | `Lyo.Web.Components` editor                                                                                                                                                                                |
| `/cache`, `/locks`, `/rabbitmq`, `/metrics`, `/schedule`, `/diagnostics`, `/jobs`, `/privacy-redaction` | Infrastructure workbenches             | `Lyo.Cache`, `Lyo.Lock`, `Lyo.MessageQueue.RabbitMq.Web.Components`, `Lyo.Metrics`, `Lyo.Schedule.Web.Components`, `Lyo.Diagnostic.Web.Components`, `Lyo.Job.Web.Components`, `Lyo.Privacy.Web.Components` |

Constants are defined in `Lyo.Gateway.Constants.Page` (workbench routes) and `Lyo.Gateway.Models.Constants` (Person/FileStorageWorkbench API routes).

## Proxy routes

Two minimal-API endpoints sit in front of the Blazor app:

- `GET /filestorage-download/{fileId:guid}?expiresHours=…` (`Constants.FileStorageWorkbench.ProxyDownloadRoute`) — Requires `FileStorageWorkbench:UseTestApiServices=true`. Asks the
  Test API for metadata; for plain files it requests `…/files/{id}/presigned-read` and 302s to the storage URL so bytes never cross the Gateway. For encrypted/compressed files it
  streams decrypted output from `…/files/{id}/download`, copying through `HttpResponseStream` and setting `Content-Length` from metadata so browser progress works.
- `GET /comic-files/{id:guid}` — Calls `IComicApiClient.GetFileWithTypeAsync($"files/{id}")` and returns the bytes with the Comic API's content type. Used so a phone on the LAN can
  load images even though the Comic API only listens on `localhost`.

## File Storage Workbench wiring

`AddFileStorageWorkbenchSupport(IConfiguration)` (in `Services/FileStorageWorkbenchExtensions.cs`) is the switch between the two hosting modes for `/filestorage-workbench`. It
binds `FileStorageWorkbenchOptions` from `FileStorageWorkbench` and:

- **Proxy mode (`UseTestApiServices = true`, the default in `appsettings.json`)** — Registers keyed `IFileStorageService` → `TestApiFileStorageService`, keyed `IKeyStore` →
  `TestApiKeyStore`, and `IFileStorageWorkbenchQueryService` → `TestApiFileStorageWorkbenchQueryService`. All three call back into the Test API using `IApiClient`, prefixed by
  `ApiRoutePrefix` (default `Workbench/FileStorage`).
- **In-process mode (`UseTestApiServices = false`, `AutoRegisterS3Services = true`)** — Registers `AddTwoKeyEncryptionFromConfiguration` (KEK from `AwsKeyStoreConfigSection`),
  Postgres file metadata store (`MetadataStoreConfigSection`), and S3 file storage (`S3FileStorageConfigSection`), all keyed by `FileStorageServiceKey` / `MetadataStoreKey` so the
  same workbench page binds to a real backend.

Service keys default to `gateway-filestorage` (and `gateway-filestorage-metadata`).

## Other services in `Program.cs`

A single `WebApplication` builder registers the entire surface used across workbenches:

- Infra: `AddCsvService`, `AddXlsxService`, `AddCompressionService`, `AddLyoMetrics`, `AddScheduler`, `AddLocalCacheFromConfiguration`, `AddLocalLock(enableMetrics)`,
  `AddLocalKeyedSemaphore(enableMetrics)`, `AddImageSharpImageServiceFromConfiguration`, `AddPdfService`, `AddSpriteSheetExportService`, `AddPdfAnnotatorInterop`.
- Communication: `AddEmailServiceFromConfiguration`, `AddTwilioSmsServiceFromConfiguration`, `SetupRabbitMqServiceFromConfiguration`, `AddAwsTranslationServiceFromConfiguration`,
  `AddProfanityFilterServiceFromConfiguration`, Typecast client + TTS service, `AddQRCodeServiceFromConfiguration`, `AddNativeBarcodeServiceFromConfiguration`.
- Web: `AddWebRendererServiceFromConfiguration`, `AddBlazoredLocalStorage`, `AddMudServices(...)`, `IIOTempService` rooted at `lyo-gateway-uploads`, `TestGatewayFileTransformer`
  for the file-tools workbench.
- API client: `Configure<ApiClientOptions>(…ApiClientOptions.SectionName)`, `AddLyoApiClient`.
- Comic: `AddComicApiClientFromConfiguration`.
- File workbench: `AddFileStorageWorkbenchSupport(builder.Configuration)`.

## Configuration sections

`appsettings.json` ships placeholders for every section the host binds:

| Section                                                            | Used by                                                  |
|--------------------------------------------------------------------|----------------------------------------------------------|
| `ApiClient`                                                        | `Lyo.Api.Client` (talking to `Lyo.TestApi`)              |
| `FileStorageWorkbench`                                             | `AddFileStorageWorkbenchSupport`                         |
| `AwsKeyStore`, `S3FileStorageOptions`, `PostgresFileMetadataStore` | In-process S3 + metadata when `UseTestApiServices=false` |
| `AwsTranslationOptions`                                            | `AddAwsTranslationServiceFromConfiguration`              |
| `TypecastClient`, `TypecastOptions`                                | Typecast TTS workbench                                   |
| `EmailServiceOptions`                                              | SMTP-based `Lyo.Email`                                   |
| `TwilioOptions`                                                    | `Lyo.Sms.Twilio`                                         |
| `RabbitMqOptions`                                                  | `Lyo.MessageQueue.RabbitMq`                              |
| `WebRenderOptions`                                                 | `Lyo.Web.WebRenderer` (HTML → PDF)                       |
| `CacheOptions`                                                     | `AddLocalCacheFromConfiguration`                         |
| `JobDashboard`                                                     | `Lyo.Job.Web.Components` Jobs page                       |
| `ComicApi`                                                         | `IComicApiClient` (also used by `/comic-files/{id}`)     |

## Related projects

- [`Lyo.Api.Client`](../../Integration/Api/Lyo.Api.Client/README.md)
- [`Lyo.Barcode.Native`](../../Data/Barcode/Lyo.Barcode.Native/README.md)
- [`Lyo.Barcode.TestWorkbench.Web.Components`](../../Data/Barcode/Lyo.Barcode.TestWorkbench.Web.Components/README.md)
- [`Lyo.Cache`](../../Core/Cache/Lyo.Cache/README.md)
- [`Lyo.Comic.Api.Client`](../../Apps/Comic/Lyo.Comic.Api.Client/README.md)
- [`Lyo.Comic.Api.Models`](../../Apps/Comic/Lyo.Comic.Api.Models/README.md)
- [`Lyo.Comic.Postgres`](../../Features/Comic/Lyo.Comic.Postgres/README.md)
- [`Lyo.Comic.Web.Components`](../../Features/Comic/Lyo.Comic.Web.Components/README.md)
- [`Lyo.Common`](../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Compression`](../../Data/Compression/Lyo.Compression/README.md)
- [`Lyo.Csv`](../../Data/Csv/Lyo.Csv/README.md)
- [`Lyo.Diagnostic.Web.Components`](../../Core/Diagnostic/Lyo.Diagnostic.Web.Components/README.md)
- [`Lyo.Email.Web.Components`](../../Communication/Email/Lyo.Email.Web.Components/README.md)
- [`Lyo.Email`](../../Communication/Email/Lyo.Email/README.md)
- [`Lyo.Encryption`](../../Security/Encryption/Lyo.Encryption/README.md)
- [`Lyo.FileMetadataStore.Postgres`](../../Data/FileMetadataStore/Lyo.FileMetadataStore.Postgres/README.md)
- [`Lyo.FileMetadataStore`](../../Data/FileMetadataStore/Lyo.FileMetadataStore/README.md)
- [`Lyo.FileStorage.Blob`](../../Data/FileStorage/Lyo.FileStorage.Blob/README.md)
- [`Lyo.FileStorage.S3`](../../Data/FileStorage/Lyo.FileStorage.S3/README.md)
- [`Lyo.FileStorage.Web.Components`](../../Data/FileStorage/Lyo.FileStorage.Web.Components/README.md)
- [`Lyo.FileStorage`](../../Data/FileStorage/Lyo.FileStorage/README.md)
- [`Lyo.Hashing`](../../Security/Hashing/Lyo.Hashing/README.md)
- [`Lyo.IO.Temp`](../../Data/IOTemp/Lyo.IO.Temp/README.md)
- [`Lyo.Images.Web.Components`](../../Data/Images/Lyo.Images.Web.Components/README.md)
- [`Lyo.Images`](../../Data/Images/Lyo.Images/README.md)
- [`Lyo.Job.Web.Components`](../../Integration/Job/Lyo.Job.Web.Components/README.md)
- [`Lyo.Keystore.Aws`](../../Security/Encryption/Lyo.Keystore.Aws/README.md)
- [`Lyo.Keystore`](../../Security/Encryption/Lyo.Keystore/README.md)
- [`Lyo.Lock`](../../Core/Lock/Lyo.Lock/README.md)
- [`Lyo.MessageQueue.RabbitMq.Web.Components`](../../Communication/MessageQueue/Lyo.MessageQueue.RabbitMq.Web.Components/README.md)
- [`Lyo.MessageQueue.RabbitMq`](../../Communication/MessageQueue/Lyo.MessageQueue.RabbitMq/README.md)
- [`Lyo.MessageQueue.Web.Components`](../../Communication/MessageQueue/Lyo.MessageQueue.Web.Components/README.md)
- [`Lyo.MessageQueue`](../../Communication/MessageQueue/Lyo.MessageQueue/README.md)
- [`Lyo.Metrics`](../../Core/Metrics/Lyo.Metrics/README.md)
- [`Lyo.Pdf.Web.Components`](../../Data/Pdf/Lyo.Pdf.Web.Components/README.md)
- [`Lyo.Pdf`](../../Data/Pdf/Lyo.Pdf/README.md)
- [`Lyo.People.Models`](../../Core/People/Lyo.People.Models/README.md)
- [`Lyo.Privacy.Web.Components`](../../Core/Privacy/Lyo.Privacy.Web.Components/README.md)
- [`Lyo.Profanity`](../../Features/Profanity/Lyo.Profanity/README.md)
- [`Lyo.QRCode.Web.Components`](../../Data/QRCode/Lyo.QRCode.Web.Components/README.md)
- [`Lyo.QRCode`](../../Data/QRCode/Lyo.QRCode/README.md)
- [`Lyo.Query.Web.Components`](../../Data/Query/Lyo.Query.Web.Components/README.md)
- [`Lyo.Result`](../../Core/Result/Lyo.Result/README.md)
- [`Lyo.Schedule.Web.Components`](../../Core/Schedule/Lyo.Schedule.Web.Components/README.md)
- [`Lyo.Scheduler`](../../Core/Scheduler/Lyo.Scheduler/README.md)
- [`Lyo.Sms.Twilio`](../../Communication/Sms/Lyo.Sms.Twilio/README.md)
- [`Lyo.Sms.Web.Components`](../../Communication/Sms/Lyo.Sms.Web.Components/README.md)
- [`Lyo.Tag`](../../Features/Tag/Lyo.Tag/README.md)
- [`Lyo.Translation.Aws`](../../Communication/Translation/Lyo.Translation.Aws/README.md)
- [`Lyo.Translation.Web.Components`](../../Communication/Translation/Lyo.Translation.Web.Components/README.md)
- [`Lyo.Tts.AwsPolly.Web.Components`](../../Communication/Speech/Lyo.Tts.AwsPolly.Web.Components/README.md)
- [`Lyo.Tts.Typecast.Web.Components`](../../Communication/Speech/Lyo.Tts.Typecast.Web.Components/README.md)
- [`Lyo.Tts.Typecast`](../../Communication/Speech/Lyo.Tts.Typecast/README.md)
- [`Lyo.Web.Components`](../../Integration/Web/Lyo.Web.Components/README.md)
- [`Lyo.Web.WebRenderer`](../../Integration/Web/Renderer/Lyo.Web.WebRenderer/README.md)
- [`Lyo.Xlsx`](../../Data/Xlsx/Lyo.Xlsx/README.md)
