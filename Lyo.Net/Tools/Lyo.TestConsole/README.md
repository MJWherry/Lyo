# Lyo.TestConsole

Ad-hoc scratch host used to exercise Lyo services from a long-lived `Microsoft.Extensions.Hosting` process. Most of `Program.cs` is registration: nearly every Lyo service is wired up so you can resolve any of them from a single scope and try things end-to-end. The "main" routine itself is short and intentionally throwaway — at present it opens a Playwright browser session, navigates to a manga page, and (optionally) starts the Discord bot.

## What it actually does

- Creates a scope and pulls `IPlaywrightBrowserService`.
- `CreateSession()` → `StartBrowserAsync()` → `Browser.NavigateToAsync("https://mangafire.to/manga/witch-hat-atelierr.pjyy4")`.
- `Console.ReadLine()` — keeps the browser open until you hit Enter. A Selenium block below is commented out.
- Resolves `IOptions<LyoDiscordBotOptions>` and, if `Token` is non-empty, hooks Ctrl+C, runs `LyoDiscordBot.RunAsync(botCts.Token)`, then stops the host.

## Services registered

- **Infra / format** — Logging (simple console, UTC), `AddIOTempService`, `AddPreviewService`, `AddLyoMetrics`, `AddCompressionService`, `AddLyoDiffServices`, `AddPdfServiceFromConfiguration`, `AddFormatterService`, `AddCsvService`, `AddXlsxService`, `AddSkiaImageServiceFromConfiguration`, `AddScheduler(o => o.CheckIntervalMs = 1000)`, `AddFusionCacheFromConfiguration`.
- **Browser automation** — `AddSeleniumBrowserService`, `AddPlaywrightBrowserService`, `AddWebRendererServiceFromConfiguration` (HTML → PDF via headless browser).
- **Speech** — Typecast (`AddTypecastClientFromConfiguration`, `AddTypecastTtsServiceFromConfiguration`) and AWS Polly (`AddAwsPollyTtsServiceFromConfiguration`). A non-generic `ITtsService` is wired to `TypecastTtsAppService` for the Discord bot; swap in `AwsPollyTtsAppService` if you want Polly there.
- **Communication** — `AddAwsTranslationServiceFromConfiguration`, `AddProfanityFilterServiceFromConfiguration`, `AddEmailServiceFromConfiguration`, `AddTwilioSmsServiceFromConfiguration`, `AddShortUrlFromConfiguration`, `AddQRCodeServiceFromConfiguration`, `SetupRabbitMqServiceFromConfiguration`, `AddFfmpegServicesFromConfiguration`.
- **Integrations** — `AddFantasyFootballClientFromConfiguration` (ESPN), `AddLyoDiscordBot<LyoDiscordBot>(…)`.
- **AWS file storage (S3 + two-key)** — `AddAwsKeyStoreFromConfiguration` + `AddTwoKeyEncryptionServiceKeyed("two-key-aws", "dev/CourtCanary/FileStore")`, then `AddPostgresFileMetadataStoreKeyed("postgres-filemetadatastore")` and `AddS3FileStorageServiceKeyed("client-files")` chained to `UseFileMetadataStore` + `UseEncryptionService` + `ConfigureS3FileStorage().Build(configuration)`.
- **Local file storage (alt key)** — `AddFileStorageServiceKeyed("two-key-local-filestore", …, "two-key-aws")` rooted at `~/My Documents/local-filestorage` with `EnableDuplicateDetection = true` and `DuplicateStrategy = ReturnExisting`, backed by `LocalFileMetadataStore` at `~/My Documents/local-filestore`. Both file storage services share the same `two-key-aws` encryption key store.
- **Database stores** — `ConnectionString` (root config key, default `null`) is fed to `AddReportingDbContextFactory`, `AddEndatoDbContextFactory`, `AddShortUrlDbContextFactory`, `AddTwilioSmsDbContextFactory`, `AddPostgresAuditRecorder`, `AddPeopleDbContextFactory`, `AddPostgresCommentStore`, `AddPostgresHomeInventoryStore`, `AddEmailDbContextFactory`, `AddFileMetadataStoreDbContextFactory`, `AddPostgresJobManagement`, `AddComicDbContextFactory`, `AddPostgresTagStore`. All are `EnableAutoMigrations = true`.
- **CRUD/QueryConcrete** — `AddLyoCrudServices<TwilioSmsDbContext>()`, `AddLyoCrudServices<AuditDbContext>()`, `AddLyoCrudServices<JobContext>()`, `AddLyoQueryServices()`, `AddScoped<JobService>`.
- **Mapping** — Mapster `TypeAdapterConfig` configured with `EnumMappingStrategy.ByName`, `MaxDepth(8)`, `NameMatchingStrategy.IgnoreCase`, a polymorphic `ConstructUsing(src => src)` for the abstract `WhereClause`, and a bidirectional mapping between `TwilioSmsResult` and `TwilioSmsLogEntity` (forward by Mapster, reverse via `SmsLogMappingHelper.MapToTwilioSmsResult`). Wired up via `IMapper`/`ILyoMapper` (`MapsterLyoMapper`).
- **API client** — `IApiClient` registered as `new ApiClient(LyoJsonSerializerOptions.Create().AddLyoDateOnlyModelConverters())`.
- **Job scheduler** — `AddJobScheduler(new() { ApiBaseUrl = "http://localhost:5092/" })`.

## Configuration

`appsettings.json` ships with every section nulled out so the host won't accidentally talk to a real service. The notable values:

| Key | Purpose |
| ------------------------------------------ | ---------------------------------------------------------------------------- |
| `ConnectionString` | Root-level Postgres connection string used by every `AddXxxDbContextFactory` |
| `AwsKeystore`, `S3FileStorageOptions` | Two-key encryption + S3 file storage (`client-files`) |
| `AwsPollyOptions`, `AwsTranslationOptions` | AWS Polly TTS / Translate |
| `TypecastClient` | Typecast TTS API key + base URL |
| `EmailServiceOptions` | SMTP host/port/credentials |
| `TwilioOptions` | Twilio account SID + auth token + default `From` number |
| `RabbitMqOptions` | RabbitMQ connection + admin URL |
| `Redis:ConnectionString` | Distributed cache target (`localhost:6379` by default) |
| `CacheOptions` | `AddFusionCacheFromConfiguration` settings |

Bot/scheduler/file storage settings come from the same hierarchy via their respective `AddXxxFromConfiguration` extensions.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api` — (direct, lyo)
- `Lyo.Api.Client` — (direct, lyo)
- `Lyo.Api.Models` — (direct, lyo)
- `Lyo.Audit.Postgres` — (direct, lyo)
- `Lyo.Cache.Fusion` — (direct, lyo)
- `Lyo.Comic.Postgres` — (direct, lyo)
- `Lyo.Comment.Postgres` — (direct, lyo)
- `Lyo.Common` — (direct, lyo)
- `Lyo.Compression` — (direct, lyo)
- `Lyo.Csv` — (direct, lyo)
- `Lyo.Discord.Bot` — (direct, lyo)
- `Lyo.Email` — (direct, lyo)
- `Lyo.Email.Postgres` — (direct, lyo)
- `Lyo.Encryption` — (direct, lyo)
- `Lyo.Endato.Client` — (direct, lyo)
- `Lyo.Endato.Postgres` — (direct, lyo)
- `Lyo.Espn.Fantasy.Football` — (direct, lyo)
- `Lyo.Ffmpeg` — (direct, lyo)
- `Lyo.FileMetadataStore` — (direct, lyo)
- `Lyo.FileMetadataStore.Postgres` — (direct, lyo)
- `Lyo.FileStorage.S3` — (direct, lyo)
- `Lyo.FileSystemWatcher` — (direct, lyo)
- `Lyo.Formatter` — (direct, lyo)
- `Lyo.HomeInventory.Postgres` — (direct, lyo)
- `Lyo.IO.Temp` — (direct, lyo)
- `Lyo.Images.Skia` — (direct, lyo)
- `Lyo.Job.Models` — (direct, lyo)
- `Lyo.Job.Postgres` — (direct, lyo)
- `Lyo.Job.Scheduler` — (direct, lyo)
- `Lyo.Keystore.Aws` — (direct, lyo)
- `Lyo.MessageQueue` — (direct, lyo)
- `Lyo.MessageQueue.RabbitMq` — (direct, lyo)
- `Lyo.Pdf` — (direct, lyo)
- `Lyo.Pdf.Web.Components` — (direct, lyo)
- `Lyo.People.Postgres` — (direct, lyo)
- `Lyo.Preview` — (direct, lyo)
- `Lyo.Profanity` — (direct, lyo)
- `Lyo.QRCode` — (direct, lyo)
- `Lyo.Reporting.Models` — (direct, lyo)
- `Lyo.Reporting.Postgres` — (direct, lyo)
- `Lyo.Reporting.Web` — (direct, lyo)
- `Lyo.Scheduler` — (direct, lyo)
- `Lyo.ShortUrl` — (direct, lyo)
- `Lyo.ShortUrl.Postgres` — (direct, lyo)
- `Lyo.Sms.Twilio` — (direct, lyo)
- `Lyo.Sms.Twilio.Postgres` — (direct, lyo)
- `Lyo.Tag.Postgres` — (direct, lyo)
- `Lyo.Translation.Aws` — (direct, lyo)
- `Lyo.Tts.AwsPolly` — (direct, lyo)
- `Lyo.Tts.Typecast` — (direct, lyo)
- `Lyo.Web.Automation.Playwright` — (direct, lyo)
- `Lyo.Web.Automation.Selenium` — (direct, lyo)
- `Lyo.Web.WebRenderer` — (direct, lyo)
- `Lyo.Xlsx` — (direct, lyo)
- `Bogus` `35.6.5` — (direct, third-party)
- `Mapster` `10.0.10` — (direct, third-party)
- `Mapster.DependencyInjection` `10.0.10` — (direct, third-party)
- `Lyo.Api.Export` — (transitive, lyo)
- `Lyo.Audit` — (transitive, lyo)
- `Lyo.Cache` — (transitive, lyo)
- `Lyo.Comic` — (transitive, lyo)
- `Lyo.Comment` — (transitive, lyo)
- `Lyo.ContentThreatScan` — (transitive, lyo)
- `Lyo.Csv.Models` — (transitive, lyo)
- `Lyo.DataTable.Models` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Diagnostic` — (transitive, lyo)
- `Lyo.Diff` — (transitive, lyo)
- `Lyo.Discord.Client` — (transitive, lyo)
- `Lyo.Discord.Models` — (transitive, lyo)
- `Lyo.Email.Models` — (transitive, lyo)
- `Lyo.EntityReference.Models` — (transitive, lyo)
- `Lyo.EntityReference.Postgres` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Ffmpeg.Models` — (transitive, lyo)
- `Lyo.FileStorage` — (transitive, lyo)
- `Lyo.Geolocation.Models` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.Health` — (transitive, lyo)
- `Lyo.HomeInventory` — (transitive, lyo)
- `Lyo.Images` — (transitive, lyo)
- `Lyo.Keystore` — (transitive, lyo)
- `Lyo.Lock` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.Notification` — (transitive, lyo)
- `Lyo.PackageMetadata` — (transitive, lyo)
- `Lyo.Pdf.Models` — (transitive, lyo)
- `Lyo.People.Models` — (transitive, lyo)
- `Lyo.Postgres` — (transitive, lyo)
- `Lyo.Query` — (transitive, lyo)
- `Lyo.Query.Models` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Schedule.Models` — (transitive, lyo)
- `Lyo.Sms` — (transitive, lyo)
- `Lyo.Sms.Models` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `Lyo.Tag` — (transitive, lyo)
- `Lyo.Translation` — (transitive, lyo)
- `Lyo.Tts` — (transitive, lyo)
- `Lyo.Tts.Models` — (transitive, lyo)
- `Lyo.Typecast.Client` — (transitive, lyo)
- `Lyo.Validation` — (transitive, lyo)
- `Lyo.Web.Automation` — (transitive, lyo)
- `Lyo.Web.Components` — (transitive, lyo)
- `Lyo.Xlsx.Models` — (transitive, lyo)
- `AWSSDK.Core` `4.0.100.4` — (transitive, third-party)
- `AWSSDK.Polly` `4.0.100.3` — (transitive, third-party)
- `AWSSDK.S3` `4.0.101` — (transitive, third-party)
- `AWSSDK.SecretsManager` `4.0.100.3` — (transitive, third-party)
- `AWSSDK.Translate` `4.0.100.3` — (transitive, third-party)
- `Blazored.LocalStorage` `4.5.0` — (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party, netstandard2.0)
- `CliWrap` `3.10.2` — (transitive, third-party)
- `ClosedXML` `0.105.0` — (transitive, third-party)
- `CsvHelper` `33.1.0` — (transitive, third-party)
- `DSharpPlus` `4.5.2` — (transitive, third-party)
- `DSharpPlus.CommandsNext` `4.5.2` — (transitive, third-party)
- `DSharpPlus.Interactivity` `4.5.2` — (transitive, third-party)
- `DSharpPlus.SlashCommands` `4.5.2` — (transitive, third-party)
- `DocumentFormat.OpenXml` `3.1.1` — (transitive, third-party)
- `EasyCompressor` `2.1.0` — (transitive, third-party)
- `ExcelDataReader` `3.9.0` — (transitive, third-party)
- `ExcelDataReader.DataSet` `3.9.0` — (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `MailKit` `4.17.0` — (transitive, third-party)
- `MetadataExtractor` `2.9.3` — (transitive, third-party)
- `Microsoft.AspNetCore.Authorization` `10.0.5` — (transitive, microsoft)
- `Microsoft.AspNetCore.Components.Web` `10.0.5` — (transitive, microsoft)
- `Microsoft.AspNetCore.Http.Abstractions` `2.*` — (transitive, microsoft)
- `Microsoft.AspNetCore.OpenApi` `10.0.5` — (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` — (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Analyzers` `10.0.5` — (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` — (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Configuration` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.DataAnnotations` `10.0.5` — (transitive, microsoft)
- `Microsoft.Playwright` `1.59.0` — (transitive, microsoft)
- `MudBlazor` `9.3` — (transitive, third-party)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` — (transitive, third-party)
- `PDFsharp` `6.2.4` — (transitive, third-party)
- `PdfPig` `0.1.15` — (transitive, third-party)
- `PuppeteerSharp` `24.0.0` — (transitive, third-party)
- `RabbitMQ.Client` `7.2.1` — (transitive, third-party)
- `Selenium.Support` `4.46.0` — (transitive, third-party)
- `Selenium.WebDriver` `4.46.0` — (transitive, third-party)
- `SixLabors.Fonts` `2.1.3` — (transitive, third-party)
- `SixLabors.ImageSharp` `3.1.12` — (transitive, third-party)
- `SixLabors.ImageSharp.Drawing` `2.1.7` — (transitive, third-party)
- `SkiaSharp` `3.*` — (transitive, third-party)
- `SkiaSharp.NativeAssets.Linux.NoDependencies` `3.*` — (transitive, third-party)
- `SmartFormat.NET` `3.6.1` — (transitive, third-party)
- `System.Buffers` `4.6.0` — (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` — (transitive, microsoft)
- `System.Diagnostics.DiagnosticSource` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` — (transitive, microsoft)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `Twilio` `7.14.9` — (transitive, third-party)
- `ZXing.Net` `0.16.11` — (transitive, third-party)
- `ZXing.Net.Bindings.ImageSharp.V3` `0.16.18` — (transitive, third-party)
- `ZiggyCreatures.FusionCache` `2.6.0` — (transitive, third-party)
- `ZiggyCreatures.FusionCache.Backplane.StackExchangeRedis` `2.6.0` — (transitive, third-party)