# Lyo.TestConsole

Ad-hoc scratch host used to exercise Lyo services from a long-lived `Microsoft.Extensions.Hosting` process. Most of `Program.cs` is registration: nearly every Lyo service is wired
up so you can resolve any of them from a single scope and try things end-to-end. The "main" routine itself is short and intentionally throwaway — at present it opens a Playwright
browser session, navigates to a manga page, and (optionally) starts the Discord bot.

## What it actually does

`Program.cs` builds a default host, configures services (see below), starts the host, then in `Main`:

1. Creates a scope and pulls `IPlaywrightBrowserService`.
2. `CreateSession()` → `StartBrowserAsync()` → `Browser.NavigateToAsync("https://mangafire.to/manga/witch-hat-atelierr.pjyy4")`.
3. `Console.ReadLine()` — keeps the browser open until you hit Enter. A Selenium block below is commented out.
4. Resolves `IOptions<LyoDiscordBotOptions>` and, if `Token` is non-empty, hooks Ctrl+C, runs `LyoDiscordBot.RunAsync(botCts.Token)`, then stops the host.

Treat this file as a scratchpad: it's the place to drop "give me an `IFileStorageService`, do a thing, log the result, exit" experiments without needing a new project.

## Services registered

`ConfigureServices` registers a wide cross-section of Lyo so any of them is one `sp.GetRequiredService<…>()` away:

- **Infra / format** — Logging (simple console, UTC), `AddIOTempService`, `AddPreviewService`, `AddLyoMetrics`, `AddCompressionService`, `AddLyoDiffServices`,
  `AddPdfServiceFromConfiguration`, `AddFormatterService`, `AddCsvService`, `AddXlsxService`, `AddSkiaImageServiceFromConfiguration`, `AddScheduler(o => o.CheckIntervalMs = 1000)`,
  `AddFusionCacheFromConfiguration`.
- **Browser automation** — `AddSeleniumBrowserService`, `AddPlaywrightBrowserService`, `AddWebRendererServiceFromConfiguration` (HTML → PDF via headless browser).
- **Speech** — Typecast (`AddTypecastClientFromConfiguration`, `AddTypecastTtsServiceFromConfiguration`) and AWS Polly (`AddAwsPollyTtsServiceFromConfiguration`). A non-generic
  `ITtsService` is wired to `TypecastTtsAppService` for the Discord bot; swap in `AwsPollyTtsAppService` if you want Polly there.
- **Communication** — `AddAwsTranslationServiceFromConfiguration`, `AddProfanityFilterServiceFromConfiguration`, `AddEmailServiceFromConfiguration`,
  `AddTwilioSmsServiceFromConfiguration`, `AddShortUrlFromConfiguration`, `AddQRCodeServiceFromConfiguration`, `SetupRabbitMqServiceFromConfiguration`,
  `AddFfmpegServicesFromConfiguration`.
- **Integrations** — `AddFantasyFootballClientFromConfiguration` (ESPN), `AddLyoDiscordBot<LyoDiscordBot>(…)`.
- **AWS file storage (S3 + two-key)** — `AddAwsKeyStoreFromConfiguration` + `AddTwoKeyEncryptionServiceKeyed("two-key-aws", "dev/CourtCanary/FileStore")`, then
  `AddPostgresFileMetadataStoreKeyed("postgres-filemetadatastore")` and `AddS3FileStorageServiceKeyed("client-files")` chained to `UseFileMetadataStore` + `UseEncryptionService` +
  `ConfigureS3FileStorage().Build(configuration)`.
- **Local file storage (alt key)** — `AddFileStorageServiceKeyed("two-key-local-filestore", …, "two-key-aws")` rooted at `~/My Documents/local-filestorage` with
  `EnableDuplicateDetection = true` and `DuplicateStrategy = ReturnExisting`, backed by `LocalFileMetadataStore` at `~/My Documents/local-filestore`. Both file storage services
  share the same `two-key-aws` encryption key store.
- **Database stores** — `ConnectionString` (root config key, default `null`) is fed to `AddReportingDbContextFactory`, `AddEndatoDbContextFactory`, `AddShortUrlDbContextFactory`,
  `AddTwilioSmsDbContextFactory`, `AddPostgresAuditRecorder`, `AddPeopleDbContextFactory`, `AddPostgresCommentStore`, `AddPostgresHomeInventoryStore`, `AddEmailDbContextFactory`,
  `AddFileMetadataStoreDbContextFactory`, `AddPostgresJobManagement`, `AddComicDbContextFactory`, `AddPostgresTagStore`. All are `EnableAutoMigrations = true`.
- **CRUD/Query** — `AddLyoCrudServices<TwilioSmsDbContext>()`, `AddLyoCrudServices<AuditDbContext>()`, `AddLyoCrudServices<JobContext>()`, `AddLyoQueryServices()`,
  `AddScoped<JobService>`.
- **Mapping** — Mapster `TypeAdapterConfig` configured with `EnumMappingStrategy.ByName`, `MaxDepth(8)`, `NameMatchingStrategy.IgnoreCase`, a polymorphic
  `ConstructUsing(src => src)` for the abstract `WhereClause`, and a bidirectional mapping between `TwilioSmsResult` and `TwilioSmsLogEntity` (forward by Mapster, reverse via
  `SmsLogMappingHelper.MapToTwilioSmsResult`). Wired up via `IMapper`/`ILyoMapper` (`MapsterLyoMapper`).
- **API client** — `IApiClient` registered as `new ApiClient(LyoJsonSerializerOptions.Create().AddLyoDateOnlyModelConverters())`.
- **Job scheduler** — `AddJobScheduler(new() { ApiBaseUrl = "http://localhost:5092/" })`.

## Configuration

`appsettings.json` ships with every section nulled out so the host won't accidentally talk to a real service. The notable values:

| Key                                        | Purpose                                                                      |
|--------------------------------------------|------------------------------------------------------------------------------|
| `ConnectionString`                         | Root-level Postgres connection string used by every `AddXxxDbContextFactory` |
| `AwsKeystore`, `S3FileStorageOptions`      | Two-key encryption + S3 file storage (`client-files`)                        |
| `AwsPollyOptions`, `AwsTranslationOptions` | AWS Polly TTS / Translate                                                    |
| `TypecastClient`                           | Typecast TTS API key + base URL                                              |
| `EmailServiceOptions`                      | SMTP host/port/credentials                                                   |
| `TwilioOptions`                            | Twilio account SID + auth token + default `From` number                      |
| `RabbitMqOptions`                          | RabbitMQ connection + admin URL                                              |
| `Redis:ConnectionString`                   | Distributed cache target (`localhost:6379` by default)                       |
| `CacheOptions`                             | `AddFusionCacheFromConfiguration` settings                                   |

Bot/scheduler/file storage settings come from the same hierarchy via their respective `AddXxxFromConfiguration` extensions.

## Related projects

- [`Lyo.Api.Client`](../../Integration/Api/Lyo.Api.Client/README.md)
- [`Lyo.Api.Models`](../../Integration/Api/Lyo.Api.Models/README.md)
- [`Lyo.Api`](../../Integration/Api/Lyo.Api/README.md)
- [`Lyo.Audit.Postgres`](../../Core/Audit/Lyo.Audit.Postgres/README.md)
- [`Lyo.Cache.Fusion`](../../Core/Cache/Lyo.Cache.Fusion/README.md)
- [`Lyo.Comic.Postgres`](../../Features/Comic/Lyo.Comic.Postgres/README.md)
- [`Lyo.Comment.Postgres`](../../Features/Comment/Lyo.Comment.Postgres/README.md)
- [`Lyo.Common`](../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Compression`](../../Data/Compression/Lyo.Compression/README.md)
- [`Lyo.Csv`](../../Data/Csv/Lyo.Csv/README.md)
- [`Lyo.Discord.Bot`](../../Integration/Discord/Lyo.Discord.Bot/README.md)
- [`Lyo.Email.Postgres`](../../Communication/Email/Lyo.Email.Postgres/README.md)
- [`Lyo.Email`](../../Communication/Email/Lyo.Email/README.md)
- [`Lyo.Encryption`](../../Security/Encryption/Lyo.Encryption/README.md)
- [`Lyo.Endato.Postgres`](../../Integration/Endato/Lyo.Endato.Postgres/README.md)
- [`Lyo.Espn.Fantasy.Football`](../../Integration/Espn/Lyo.Espn.Fantasy.Football/README.md)
- [`Lyo.Ffmpeg`](../../Data/Ffmpeg/Lyo.Ffmpeg/README.md)
- [`Lyo.FileMetadataStore.Postgres`](../../Data/FileMetadataStore/Lyo.FileMetadataStore.Postgres/README.md)
- [`Lyo.FileMetadataStore`](../../Data/FileMetadataStore/Lyo.FileMetadataStore/README.md)
- [`Lyo.FileStorage.S3`](../../Data/FileStorage/Lyo.FileStorage.S3/README.md)
- [`Lyo.FileSystemWatcher`](../../Data/FileSystemWatcher/Lyo.FileSystemWatcher/README.md)
- [`Lyo.Formatter`](../../Data/Formatter/Lyo.Formatter/README.md)
- [`Lyo.HomeInventory.Postgres`](../../Features/HomeInventory/Lyo.HomeInventory.Postgres/README.md)
- [`Lyo.IO.Temp`](../../Data/IOTemp/Lyo.IO.Temp/README.md)
- [`Lyo.Images.Skia`](../../Data/Images/Lyo.Images.Skia/README.md)
- [`Lyo.Job.Models`](../../Integration/Job/Lyo.Job.Models/README.md)
- [`Lyo.Job.Postgres`](../../Integration/Job/Lyo.Job.Postgres/README.md)
- [`Lyo.Job.Scheduler`](../../Integration/Job/Lyo.Job.Scheduler/README.md)
- [`Lyo.Keystore.Aws`](../../Security/Encryption/Lyo.Keystore.Aws/README.md)
- [`Lyo.MessageQueue.RabbitMq`](../../Communication/MessageQueue/Lyo.MessageQueue.RabbitMq/README.md)
- [`Lyo.MessageQueue`](../../Communication/MessageQueue/Lyo.MessageQueue/README.md)
- [`Lyo.Pdf.Web.Components`](../../Data/Pdf/Lyo.Pdf.Web.Components/README.md)
- [`Lyo.Pdf`](../../Data/Pdf/Lyo.Pdf/README.md)
- [`Lyo.People.Postgres`](../../Core/People/Lyo.People.Postgres/README.md)
- [`Lyo.Preview`](../Lyo.Preview/README.md)
- [`Lyo.Profanity`](../../Features/Profanity/Lyo.Profanity/README.md)
- [`Lyo.QRCode`](../../Data/QRCode/Lyo.QRCode/README.md)
- [`Lyo.Scheduler`](../../Core/Scheduler/Lyo.Scheduler/README.md)
- [`Lyo.ShortUrl.Postgres`](../../Features/ShortUrl/Lyo.ShortUrl.Postgres/README.md)
- [`Lyo.ShortUrl`](../../Features/ShortUrl/Lyo.ShortUrl/README.md)
- [`Lyo.Sms.Twilio.Postgres`](../../Communication/Sms/Lyo.Sms.Twilio.Postgres/README.md)
- [`Lyo.Sms.Twilio`](../../Communication/Sms/Lyo.Sms.Twilio/README.md)
- [`Lyo.Tag.Postgres`](../../Features/Tag/Lyo.Tag.Postgres/README.md)
- [`Lyo.Translation.Aws`](../../Communication/Translation/Lyo.Translation.Aws/README.md)
- [`Lyo.Tts.AwsPolly`](../../Communication/Speech/Lyo.Tts.AwsPolly/README.md)
- [`Lyo.Tts.Typecast`](../../Communication/Speech/Lyo.Tts.Typecast/README.md)
- [`Lyo.Web.Automation.Selenium`](../../Integration/Web/Automation/Lyo.Web.Automation.Selenium/README.md)
- [`Lyo.Web.Reporting.Postgres`](../../Integration/Web/Reporting/Lyo.Web.Reporting.Postgres/README.md)
- [`Lyo.Web.Reporting`](../../Integration/Web/Reporting/Lyo.Web.Reporting/README.md)
- [`Lyo.Web.WebRenderer`](../../Integration/Web/Renderer/Lyo.Web.WebRenderer/README.md)
- [`Lyo.Xlsx`](../../Data/Xlsx/Lyo.Xlsx/README.md)
- `Lyo.FileSystemWatcher.Tests` (../../Data/FileSystemWatcher/Lyo.FileSystemWatcher.Tests)
