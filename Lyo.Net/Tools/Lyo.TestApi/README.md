# Lyo.TestApi

Minimal-API host backing `Lyo.TestGateway` and `Lyo.TestConsole`. It wires the Lyo Postgres stores, RabbitMQ-driven job system, S3 file storage with two-key encryption, and exposes the file storage workbench endpoints that the TestGateway `/filestorage-workbench` page talks to.

## Host wiring (`Program.cs`)

- **Kestrel.** `MaxRequestBodySize = long.MaxValue` and matching `FormOptions.MultipartBodyLengthLimit` so multi-GB direct uploads stream without cutting off.
- **OpenAPI / Scalar.** `AddOpenApi()`. In development, `MapOpenApi()` and `MapScalarApiReference()` are mounted so that `/scalar/v1` (or whatever Scalar picks) shows every endpoint.
- **Compression.** Brotli + Gzip response compression (level `Fastest`) and request decompression so Gateway calls can send Brotli payloads.
- **JSON.** `LyoJsonSerializerOptions.ApplyTo` + `AddLyoDateOnlyModelConverters` + `ReferenceHandler.IgnoreCycles` + `JsonIgnoreCondition.WhenWritingNull`.
- **Infra.** `AddLyoMetricsWithOpenTelemetryFromConfiguration` (`OpenTelemetry` section: `ServiceName` `Lyo.TestApi`, `Protocol` `grpc`, `MetricExportIntervalMs` `1000`; OTLP endpoint from `OTEL_EXPORTER_OTLP_ENDPOINT` when set), `AddMetrics`, `AddFormatterService`, `AddCsvService`, `AddXlsxService`, `AddCompressionService` + `AddDefaultCompressionService<CompressionService>` (wires `ICompressionResolver` for file-storage codec dispatch), `AddLocalCacheFromConfiguration`, `AddHttpContextAccessor`, Mapster via `ConfigureMapster()`.
- **Locks.** `AddRedisLock` if `Redis:ConnectionString` (or `ConnectionStrings:Redis`) is set, otherwise `AddLocalLock()`.
- **Messaging.** `SetupRabbitMqServiceFromConfiguration` + `AddMqJobEventPublisherFromConfiguration` (job state changes travel through MQ). `JobMqOptions:WorkerTypes` includes `example` so `job.run.example` is declared at startup for `Lyo.Job.Worker.Example`.
- **Postgres stores.** One `ConnectionStrings:Postgres` is shared by `Job`, `Drift`, `People`, `TwilioSms`, `Discord`, `Config`, `Comic`, `HomeInventory`, `FileMetadataStore`. Every `AddXxxDbContextFactory` is called with `EnableAutoMigrations = true` so a fresh database is brought up on the first run.
- **CRUD/QueryConcrete.** `AddLyoCrudServices<TContext>()` on `JobContext`, `PeopleDbContext`, `TwilioSmsDbContext`, `FileMetadataStoreDbContext`; `AddLyoQueryServices()`; Person uses typed `CreateBuilder` at `/Person/*` plus root From/Joins `POST /Query` via `MapRootQueryEndpoints<PeopleDbContext>()`; Twilio uses `MapDynamicCrudEndpoints` ( includes `POST /Twilio/Query`); `AddLyoApiExport<TContext>()` + `AddCsvExport()` / `AddXlsxExport()` for `PeopleDbContext`, `DiscordDbContext`, `JobContext`; `AddPostgresSprocService<PeopleDbContext>()`.
- **File storage.** `AddTwoKeyEncryptionFromConfiguration(…, Constants.FileStorageWorkbench.ServiceKey, "AwsKeyStore")` + `AddPostgresFileMetadataStoreKeyed("gateway-filestorage-metadata")` reading `PostgresFileMetadataStore` (uses the shared Postgres connection when unset) + `AddS3FileStorageServiceKeyed("gateway-filestorage")` chained with `UseFileMetadataStore`, `UseEncryptionService`, `ConfigureS3FileStorage()` + `AddFileStorageArchiveServiceKeyed` for `GET files/archive`.
- **Audit.** `AddPostgresFileAuditSink()` writes into the file metadata DB; `AddScoped<IFileAuditEventHandler, FileMetadataQueryCacheInvalidationHandler>` invalidates the `Lyo.Cache` QueryProject cache for `FileMetadataEntity` on successful `Save`/`Delete`/`MultipartComplete` and on any `MigrateDeks` / `RotateDeks` so workbench grids see fresh rows.
- **Scheduler.** `AddJobScheduler()` is commented out; MQ queues jobs, but this host does not poll them. Run `Lyo.Job.Worker.Example` to consume `job.run.example`.

## `SetupCourtEndpoints` (root extension)

`SetupEndpoints.cs` hangs one extension method off `WebApplication` that chains every endpoint group:

```text
BuildJobGroup ← from Lyo.Job.Api (full job/job-run CRUD)
BuildDriftGroup ← from Lyo.Drift.Api (instance upsert, snapshot/diff/change ingest, DiffAgainst)
BuildReportingGroup ← from Lyo.Reporting.Api (definitions CRUD, generations query/get/delete, Generate/Rerun/Download; anonymous in this host)
BuildComicGroup ← from Lyo.Comic.Api (series/volumes/chapters/pages/characters CRUD)
BuildHomeInventoryGroup ← from Lyo.HomeInventory.Api (categories/locations/items/movements CRUD)
BuildPersonGroup ← Person CRUD (Lyo.Api builder) + info/{schema}/{table}/{column}/GetUniqueCounts
BuildDiscordGroup ← Discord dynamic CRUD
BuildTwilioGroup ← Twilio dynamic CRUD (route prefix "Twilio")
BuildFileStorageApi ← from Lyo.Api.FileStorage (Workbench/FileStorage group, POST upload/file, FileMetadata QueryProject)
MapConfigApiEndpoints(requireAuthentication: false) ← Config.Api manage + resolve + query grids; this host encrypts config values
```

Reporting uses `AddPostgresReportingManagement` + `AddLyoApiReporting` + `AddIOTempService`. Generate hooks save staged output via the keyed File Storage workbench service (
`OutputFileId`), and the `OnCleanupAsync` hook deletes that stored file when generation rows are removed (retention cleanup, generation delete, or definition delete).
`ReportingApiOptions.DownloadStreamFactory` streams persisted outputs back from the same keyed `IFileStorageService`, which maps `GET Reporting/Generation/{id}/Download`.
CSV/XLSX/JSON renderers are wired by default; this host also calls `AddWebRendererServiceFromConfiguration` + `AddReportingWebRenderer` so HTML/PDF generate works for the report design workbench. `AllowAdHocGeneration` is true so the workbench can POST composition JSON. Retention cleanup (`ReportRetentionService.CleanupAsync`) is
wired but not scheduled, set `PostgresReportingOptions.GenerationRetention` and trigger it from a job/scheduler to enable it.

Custom Job endpoints exist because the dynamic CRUD route for `JobRun` does not expose Create/Cancel; those need to go through MQ via `JobService`.

The Person group uses `Lyo.Api` `CreateBuilder<…>` with `WithFlags(All | UpsertInheritCreate | UpsertInheritUpdate | PatchInheritsUpdate)`, a `BeforeCreate` that assigns
`LyoGuid.CreateCombPostgres()`, plus `WithMetadata` / `WithProjectionComputedFields`. It also adds a `GET info/{schema}/{table}/{column}/GetUniqueCounts` route that invokes
`StoredProcedures.Info.UniqueValuesWithCount` via `ISprocService`.

## File-storage workbench group (`Workbench/FileStorage`)

`Lyo.Api.FileStorage.BuildFileStorageApi` maps the workbench group at `FileStorageApiOptions.Route` (`Workbench/FileStorage`) with the `FileStorage` OpenAPI tag. All endpoints resolve services as **keyed** `IFileStorageService`, `IMultipartUploadService`, and `IFileStorageArchiveService` under `Constants.FileStorageWorkbench.ServiceKey` (`gateway-filestorage`). No `keys/*` routes.

| Method | Route | Purpose |
| -------- | ------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `GET` | `health` | `IFileStorageService.CheckHealthAsync` |
| `POST` | `files/save` | `SaveFileAsync` from a JSON `SaveFileRequest` (bytes in-band) |
| `POST` | `files/save-stream` | Multipart `IFormFile` → `SaveFromStreamAsync` (anti-forgery is off) |
| `POST` | `files/copy` | `CopyFileAsync(SourceFileId, CopyFileRequest?)` |
| `POST` | `files/move` | `MoveFileAsync(FileId, MoveFileRequest)` |
| `POST` | `files/rename` | `RenameFileAsync(FileId, RenameFileRequest)` |
| `POST` | `files/{fileId:guid}/access-links` | `IFileDownloadAccessService.CreateLinkAsync` → `{linkId, token, downloadUrl, presignedReadUrl}` |
| `GET` | `files/{fileId:guid}/metadata` | `GetMetadataAsync` |
| `GET` | `files/{fileId:guid}/download` | Plain files: try `GetPreSignedReadUrlAsync` 302 (honors `?inline=true` via Content-Disposition); encrypted/compressed or no presign: stream the decrypted bytes |
| `GET` | `files/archive?id=&id=` | `IFileStorageArchiveService.CreateArchiveAsync` (zip of decrypted files; optional `fileName`; 400 when archive caps hit) |
| `GET` | `files/{fileId:guid}/presigned-read` | `GetPreSignedReadUrlAsync(expiresHours, pathPrefix, contentDisposition, contentType)` |
| `DELETE` | `files/{fileId:guid}` | `DeleteFileAsync` |
| `POST` | `files/migrate-deks` | `MigrateDeksAsync` (rotate each DEK that uses a source key) |
| `POST` | `files/rotate-deks` | `RotateDeksAsync(fileIds, targetKey)` |
| `POST` | `direct-upload/begin` | `BeginDirectUploadAsync` (presigned/PUT contract) |
| `PUT` | `direct-upload/{fileId:guid}/put` | Only when the keyed service is `LocalFileStorageService`. `ReceiveDirectPutAsync`; otherwise 501 |
| `POST` | `direct-upload/{fileId:guid}/complete` | `CompleteDirectUploadAsync` |
| `POST` | `multipart/begin` | `IMultipartUploadService.BeginAsync` driven by `BeginMultipartRequest` |
| `GET` | `multipart/{sessionId:guid}/part-url?partNumber` | `GetPresignedPartUploadAsync` |
| `POST` | `multipart/complete` | `CompleteAsync(SessionId, Parts[])` |
| `POST` | `multipart/{sessionId:guid}/abort` | `AbortAsync` |
| `GET` | `diagnostics/storage-keys?prefix&maxKeys` | `IFileStorageDiagnosticsService.ListStorageKeysAsync` clamped to 1 to 10 000; 501 if the backend has no diagnostics |

`access-links` yields relative paths (`Workbench/FileStorage/files/access/{token}/download` and `…/presigned-read`); both routes call
`IFileDownloadAccessService.ValidateAndConsumeDownloadAsync(token, user, remoteIp)` and translate `FileDownloadAccessConsumeFailureReason` to 400/403/404/410/429 via
`MapFailureStatusCode`.

After every mutating call (`save`, `save-stream`, `direct-upload/complete`, `multipart/complete`, `copy`, `move`, `rename`, `DELETE files/{id}`, `migrate-deks`, `rotate-deks`) the handler calls
`cache.InvalidateQueryCacheAsync<FileMetadataEntity>()` so the read-only QueryProject endpoint stays consistent. `FileMetadataQueryCacheInvalidationHandler` performs the same
invalidation when audit events flow in from the file storage layer.

## Direct upload

`BuildDirectFileUploadEndpoint` maps `POST upload/file` (`Constants.DirectFileUpload.FilePath`, tagged `DirectFileUpload`) which shares `SaveStreamFromFormAsync` with `Workbench/FileStorage/files/save-stream`. Same query string contract (`originalFileName`, `compress`, `encrypt`, `keyId`, `pathPrefix`, `chunkSize`, `contentType`, `tenantId`), anti-forgery disabled, useful when callers do not want to nest under the workbench prefix.

## FileMetadata Query/QueryProject

`BuildFileStorageApi` also wires a read-only Lyo.Api builder over `FileMetadataStoreDbContext` / `FileMetadataEntity` at `FileStorageApiOptions.FileMetadataRoute` (`Workbench/FileStorage/FileMetadata`). The standard `/QueryConcrete` and `/QueryProject` routes are produced by `WithReadOnlyEndpoints()`, allowing anonymous access; the Gateway's `Lyo.Query.Web.Components` grids POST against this route directly.

## Config sections

| Section | Used by |
| --------------------------------------------------------- | ------------------------------------------------------------------------------------------------- |
| `ConnectionStrings:Postgres` | Each Postgres `AddXxxDbContextFactory` (default: `Host=localhost;Port=5437;…`) |
| `Redis:ConnectionString` *(or `ConnectionStrings:Redis`)* | Points `IDistributedLock` at Redis; falls back to `AddLocalLock()` |
| `AwsKeyStore` | KEK / two-key encryption used by `gateway-filestorage` |
| `S3FileStorageOptions` | `AddS3FileStorageServiceKeyed("gateway-filestorage").ConfigureS3FileStorage()` |
| `PostgresFileMetadataStore` | File-metadata DB (`ConnectionString`, `EnableAutoMigrations`) |
| `QueryOptions` | `Lyo.Api` query cache plus split-query toggle |
| `JobScheduler` | Job dashboard wiring (the scheduler itself is opt-in via the commented `AddJobScheduler()`) |
| `JobMqOptions` | `WorkerTypes` includes `example` so `job.run.example` is provisioned for `Lyo.Job.Worker.Example` |
| `CacheOptions` | `AddLocalCacheFromConfiguration` (query-cache grain, payload compression) |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api` (direct, lyo)
- `Lyo.Api.Authentication` (direct, lyo)
- `Lyo.Api.Export` (direct, lyo)
- `Lyo.Api.Export.Csv` (direct, lyo)
- `Lyo.Api.Export.Xlsx` (direct, lyo)
- `Lyo.Api.FileStorage` (direct, lyo)
- `Lyo.Audit.Postgres` (direct, lyo)
- `Lyo.Authentication` (direct, lyo)
- `Lyo.Authentication.AspNetCore` (direct, lyo)
- `Lyo.Authentication.Google` (direct, lyo)
- `Lyo.Authentication.Keycloak` (direct, lyo)
- `Lyo.Authentication.OpenIdConnect` (direct, lyo)
- `Lyo.Authentication.Postgres` (direct, lyo)
- `Lyo.Comic.Api` (direct, lyo)
- `Lyo.Comic.Postgres` (direct, lyo)
- `Lyo.Comment.Postgres` (direct, lyo)
- `Lyo.Common.Json` (direct, lyo)
- `Lyo.Compression` (direct, lyo)
- `Lyo.Config.Api` (direct, lyo)
- `Lyo.Config.Postgres` (direct, lyo)
- `Lyo.ContactUs.Postgres` (direct, lyo)
- `Lyo.Discord.Postgres` (direct, lyo)
- `Lyo.Drift.Api` (direct, lyo)
- `Lyo.Drift.Postgres` (direct, lyo)
- `Lyo.Email.Postgres` (direct, lyo)
- `Lyo.Endato.Postgres` (direct, lyo)
- `Lyo.FileMetadataStore.Postgres` (direct, lyo)
- `Lyo.FileStorage` (direct, lyo)
- `Lyo.FileStorage.S3` (direct, lyo)
- `Lyo.HomeInventory.Api` (direct, lyo)
- `Lyo.HomeInventory.Postgres` (direct, lyo)
- `Lyo.IO.Temp` (direct, lyo)
- `Lyo.Job.Api` (direct, lyo)
- `Lyo.Job.Postgres` (direct, lyo)
- `Lyo.Job.Scheduler` (direct, lyo)
- `Lyo.KeyStore` (direct, lyo)
- `Lyo.KeyStore.Aws` (direct, lyo)
- `Lyo.Lock` (direct, lyo)
- `Lyo.Lock.Redis` (direct, lyo)
- `Lyo.MessageQueue.RabbitMq` (direct, lyo)
- `Lyo.Metrics.OpenTelemetry` (direct, lyo)
- `Lyo.Note.Postgres` (direct, lyo)
- `Lyo.People.Postgres` (direct, lyo)
- `Lyo.Rating.Postgres` (direct, lyo)
- `Lyo.Reporting.Api` (direct, lyo)
- `Lyo.Reporting.Business.Example` (direct, lyo)
- `Lyo.Reporting.Models` (direct, lyo)
- `Lyo.Reporting.Postgres` (direct, lyo)
- `Lyo.Reporting.Web` (direct, lyo)
- `Lyo.ShortUrl.Postgres` (direct, lyo)
- `Lyo.Sms.Twilio.Postgres` (direct, lyo)
- `Lyo.Tag.Postgres` (direct, lyo)
- `Lyo.Web.WebRenderer` (direct, lyo)
- `Mapster` `10.0.10` (direct, third-party)
- `Mapster.DependencyInjection` `10.0.10` (direct, third-party)
- `Microsoft.AspNetCore.OpenApi` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Npgsql` `10.0.3` (direct, third-party)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (direct, third-party)
- `Scalar.AspNetCore` `2.16.11` (direct, third-party)
- `Lyo.Api.Client` (transitive, lyo)
- `Lyo.Api.FileStorage.Models` (transitive, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Audit` (transitive, lyo)
- `Lyo.Authentication.Models` (transitive, lyo)
- `Lyo.Cache` (transitive, lyo)
- `Lyo.Comic` (transitive, lyo)
- `Lyo.Comment` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Config` (transitive, lyo)
- `Lyo.ContactUs` (transitive, lyo)
- `Lyo.Csv` (transitive, lyo)
- `Lyo.Csv.Models` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Diagnostic.AspNetCore` (transitive, lyo)
- `Lyo.Diff` (transitive, lyo)
- `Lyo.Discord.Models` (transitive, lyo)
- `Lyo.Drift.Models` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Lyo.EntityReference.Postgres` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.FileMetadataStore` (transitive, lyo)
- `Lyo.FileSystemWatcher.Models` (transitive, lyo)
- `Lyo.Formatter` (transitive, lyo)
- `Lyo.Geolocation.Models` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.HomeInventory` (transitive, lyo)
- `Lyo.Http.Client` (transitive, lyo)
- `Lyo.IO.FileSystem` (transitive, lyo)
- `Lyo.Job.Models` (transitive, lyo)
- `Lyo.MessageQueue` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Note` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.People.Models` (transitive, lyo)
- `Lyo.Postgres` (transitive, lyo)
- `Lyo.Query` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Rating` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Schedule.Models` (transitive, lyo)
- `Lyo.Scheduler` (transitive, lyo)
- `Lyo.ShortUrl` (transitive, lyo)
- `Lyo.Sms` (transitive, lyo)
- `Lyo.Sms.Models` (transitive, lyo)
- `Lyo.Sms.Twilio` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.SystemInformation` (transitive, lyo)
- `Lyo.Tag` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `Lyo.Xlsx` (transitive, lyo)
- `Lyo.Xlsx.Models` (transitive, lyo)
- `AWSSDK.Core` `4.0.100.4` (transitive, third-party)
- `AWSSDK.S3` `4.0.101` (transitive, third-party)
- `AWSSDK.SecretsManager` `4.0.100.3` (transitive, third-party)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `ClosedXML` `0.105.0` (transitive, third-party)
- `DocumentFormat.OpenXml` `3.1.1` (transitive, third-party)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `ExcelDataReader` `3.9.0` (transitive, third-party)
- `ExcelDataReader.DataSet` `3.9.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.AspNetCore.Components.Web` `10.0.5` (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Analyzers` `10.0.5` (transitive, microsoft)
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
- `OpenTelemetry` `1.16.0` (transitive, third-party)
- `OpenTelemetry.Exporter.Console` `1.16.0` (transitive, third-party)
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` `1.16.0` (transitive, third-party)
- `OpenTelemetry.Extensions.Hosting` `1.16.0` (transitive, third-party)
- `PuppeteerSharp` `24.0.0` (transitive, third-party)
- `RabbitMQ.Client` `7.2.1` (transitive, third-party)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `StackExchange.Redis` `2.12.0` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.Diagnostics.DiagnosticSource` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)
- `Twilio` `7.14.9` (transitive, third-party)