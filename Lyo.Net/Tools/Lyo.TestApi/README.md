# Lyo.TestApi

Minimal-API host that backs `Lyo.Gateway` and `Lyo.TestConsole`. It wires the Lyo Postgres stores, RabbitMQ-driven job system, S3 file storage with two-key encryption, and exposes the file storage workbench surface that the Gateway's `/filestorage-workbench` page talks to.

## Host setup (`Program.cs`)

- **Kestrel** — `MaxRequestBodySize = long.MaxValue` and matching `FormOptions.MultipartBodyLengthLimit` so multi-GB direct uploads stream cleanly.
- **OpenAPI / Scalar** — `AddOpenApi()`. In development, `MapOpenApi()` and `MapScalarApiReference()` are mounted so `/scalar/v1` (or whatever Scalar picks) shows every endpoint.
- **Compression** — Brotli + Gzip response compression (level `Fastest`) and request decompression so Gateway calls can ship Brotli payloads.
- **JSON** — `LyoJsonSerializerOptions.ApplyTo` + `AddLyoDateOnlyModelConverters` + `ReferenceHandler.IgnoreCycles` + `JsonIgnoreCondition.WhenWritingNull`.
- **Infra** — `AddMetrics`, `AddFormatterService`, `AddCsvService`, `AddXlsxService`, `AddCompressionService` + `AddDefaultCompressionService<CompressionService>` (registers `ICompressionResolver` for file-storage codec dispatch), `AddLocalCacheFromConfiguration`, `AddHttpContextAccessor`, Mapster via `ConfigureMapster()`.
- **Locks** — `AddRedisLock` if `Redis:ConnectionString` (or `ConnectionStrings:Redis`) is set, otherwise `AddLocalLock()`.
- **Messaging** — `SetupRabbitMqServiceFromConfiguration` + `AddMqJobEventPublisher` (job state changes flow through MQ).
- **Postgres stores** — A single `ConnectionStrings:Postgres` is shared by `Job`, `People`, `TwilioSms`, `Discord`, `Config`, `Comic`, `FileMetadataStore`. Every `AddXxxDbContextFactory` is called with `EnableAutoMigrations = true` so a fresh database is brought up on first run.
- **CRUD/QueryConcrete** — `AddLyoCrudServices<TContext>()` for `JobContext`, `PeopleDbContext`, `TwilioSmsDbContext`, `FileMetadataStoreDbContext`; `AddLyoQueryServices()`; Person uses typed `CreateBuilder` at `/Person/*` plus root From/Joins `POST /Query` via `MapRootQueryEndpoints<PeopleDbContext>()`; Twilio uses `MapDynamicCrudEndpoints` ( includes `POST /Twilio/Query`); `AddLyoApiExport<TContext>()` + `AddCsvExport()` / `AddXlsxExport()` for `PeopleDbContext`, `DiscordDbContext`, `JobContext`; `AddPostgresSprocService<PeopleDbContext>()`.
- **File storage** — `AddTwoKeyEncryptionFromConfiguration(…, Constants.FileStorageWorkbench.ServiceKey, "AwsKeyStore")` + `AddPostgresFileMetadataStoreKeyed("gateway-filestorage-metadata")` reading `PostgresFileMetadataStore` (falls back to the shared Postgres connection if not set) + `AddS3FileStorageServiceKeyed("gateway-filestorage")` chained with `UseFileMetadataStore`, `UseEncryptionService`, `ConfigureS3FileStorage()`.
- **Audit** — `AddPostgresFileAuditSink()` writes to the file metadata DB; `AddScoped<IFileAuditEventHandler, FileMetadataQueryCacheInvalidationHandler>` invalidates the `Lyo.Cache` QueryProject cache for `FileMetadataEntity` on successful `Save`/`Delete`/`MultipartComplete` and on any `MigrateDeks` / `RotateDeks` so workbench grids see fresh rows.
- **Scheduler** — `AddJobScheduler()` is commented out; jobs are queued by MQ but not polled by this host.

## `SetupCourtCanaryEndpoints` (root extension)

`SetupEndpoints.cs` adds one extension method on `WebApplication` that chains every endpoint group:

```text
BuildJobGroup ← from Lyo.Job.Postgres (full job/job-run CRUD)
BuildReportingGroup ← from Lyo.Api.Reporting (definitions CRUD, generations read-only, Generate/Rerun/Download; anonymous in this host)
BuildPersonGroup ← Person CRUD (Lyo.Api builder) + info/{schema}/{table}/{column}/GetUniqueCounts
BuildDiscordGroup ← Discord dynamic CRUD
BuildTwilioGroup ← Twilio dynamic CRUD (route prefix "Twilio")
BuildFileStorageWorkbenchGroup ← MapGroup("Workbench/FileStorage") (see below)
BuildDirectFileUploadEndpoint ← POST upload/file (mirrors files/save-stream)
BuildFileStorageWorkbenchFileMetadataQuery ← ReadOnly Lyo.Api builder over FileMetadataEntity at "Workbench/FileStorage/FileMetadata"
```

Reporting uses `AddPostgresReportingManagement` + `AddLyoApiReporting` + `AddIOTempService`. Generate hooks save staged output via the keyed File Storage workbench service (
`OutputFileId`), and the `OnCleanupAsync` hook deletes that stored file when generation rows are removed (retention cleanup or definition delete).
`ReportingApiOptions.DownloadStreamFactory` streams persisted outputs back from the same keyed `IFileStorageService`, which maps `GET Reporting/Generation/{id}/Download`.
CSV/XLSX/JSON renderers are registered by default; HTML/PDF requires hosting `AddReportingWebRenderer` separately. Retention cleanup (`ReportRetentionService.CleanupAsync`) is
registered but not scheduled — set `PostgresReportingOptions.GenerationRetention` and trigger it from a job/scheduler to enable it.

Custom Job endpoints exist because the dynamic CRUD route for `JobRun` does not expose Create/Cancel — those need to go through MQ via `JobService`.

The Person group uses `Lyo.Api` `CreateBuilder<…>` with `WithFlags(All | UpsertInheritCreate | UpsertInheritUpdate | PatchInheritsUpdate)`, a `BeforeCreate` that assigns
`LyoGuid.CreateCombPostgres()`, plus `WithMetadata` / `WithProjectionComputedFields`. It also adds a `GET info/{schema}/{table}/{column}/GetUniqueCounts` route that invokes
`StoredProcedures.Info.UniqueValuesWithCount` via `ISprocService`.

## File Storage Workbench group (`Workbench/FileStorage`)

`FileStorageWorkbench/SetupFileStorageWorkbenchEndpoints.cs` mounts the workbench group at `Constants.FileStorageWorkbench.Route` (`Workbench/FileStorage`) with the
`FileStorageWorkbench` OpenAPI tag. All endpoints resolve services as **keyed** — `IFileStorageService`, `IMultipartUploadService`, and `IKeyStore` (cast to `IKeyInventoryStore`
where available) under `Constants.FileStorageWorkbench.ServiceKey` (`gateway-filestorage`).

| Method | Route | Purpose |
| -------- | ------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------- |
| `GET` | `health` | `IFileStorageService.CheckHealthAsync` |
| `POST` | `files/save` | `SaveFileAsync` from JSON `SaveFileRequest` (bytes in-band) |
| `POST` | `files/save-stream` | Multipart `IFormFile` → `SaveFromStreamAsync` (anti-forgery disabled) |
| `POST` | `files/copy` | `CopyFileAsync(SourceFileId, CopyFileRequest?)` |
| `POST` | `files/{fileId:guid}/access-links` | `IFileDownloadAccessService.CreateLinkAsync` → `{linkId, token, downloadUrl, presignedReadUrl}` |
| `GET` | `files/{fileId:guid}/metadata` | `GetMetadataAsync` |
| `GET` | `files/{fileId:guid}/download` | Plain files: try `GetPreSignedReadUrlAsync` 302; encrypted/compressed or no presign: stream decrypted bytes |
| `GET` | `files/{fileId:guid}/presigned-read` | `GetPreSignedReadUrlAsync(expiresHours, pathPrefix, contentDisposition, contentType)` |
| `DELETE` | `files/{fileId:guid}` | `DeleteFileAsync` |
| `POST` | `files/migrate-deks` | `MigrateDeksAsync` (rotate every DEK that uses a source key) |
| `POST` | `files/rotate-deks` | `RotateDeksAsync(fileIds, targetKey)` |
| `GET` | `files/search?searchText&keyId&keyVersion&take` | EF query against `FileMetadataEntity` filtered by ILike on filename/source/path, plus key id/version |
| `POST` | `direct-upload/begin` | `BeginDirectUploadAsync` (presigned/PUT contract) |
| `PUT` | `direct-upload/{fileId:guid}/put` | Only when keyed service is `LocalFileStorageService` — `ReceiveWorkbenchDirectPutAsync`; otherwise 501 |
| `POST` | `direct-upload/{fileId:guid}/complete` | `CompleteDirectUploadAsync` |
| `POST` | `stage/begin` | `IStagedFileUploadService.BeginAsync` (presigned PUT to staging key) |
| `PUT` | `stage/{stageId:guid}/put` | Local only — `ReceiveWorkbenchStagePutAsync`; otherwise 501 |
| `POST` | `stage/{stageId:guid}/complete` | `CompleteAsync` (verify/hash staged object) |
| `POST` | `stage/{stageId:guid}/commit` | `CommitAsync` (compress/encrypt → `file_metadata`) |
| `POST` | `stage/{stageId:guid}/abort` | `AbortAsync` |
| `GET` | `stage/{stageId:guid}` | `GetAsync` |
| `POST` | `multipart/begin` | `IMultipartUploadService.BeginAsync` from `BeginMultipartWorkbenchRequest` |
| `GET` | `multipart/{sessionId:guid}/part-url?partNumber` | `GetPresignedPartUploadAsync` |
| `POST` | `multipart/complete` | `CompleteAsync(SessionId, Parts[])` |
| `POST` | `multipart/{sessionId:guid}/abort` | `AbortAsync` |
| `GET` | `diagnostics/keys?prefix&maxKeys` | `IFileStorageDiagnosticsService.ListStorageKeysAsync` clamped to 1–10 000; 501 if backend doesn't implement diagnostics |
| `GET` | `keys/search?searchText&take` | Distinct `(KeyId, Version)` pairs that have files, with `IsCurrent`, `KeyMetadata`, `FileCount` |
| `GET` | `keys/available` | `IKeyInventoryStore.GetAvailableKeyIdsAsync` (empty if store isn't `IKeyInventoryStore`) |
| `GET` | `keys/{keyId}/versions` | `GetAvailableVersionsAsync` |
| `GET` | `keys/{keyId}/raw?version` | `IKeyStore.GetKeyAsync` (raw bytes; intentionally exposed for the workbench) |
| `GET` | `keys/{keyId}/exists?version` | `HasKeyAsync` |
| `GET` | `keys/{keyId}/current-version` | `GetCurrentVersionAsync` |
| `GET` | `keys/{keyId}/metadata/{version}` | `GetKeyMetadataAsync` |
| `PUT` | `keys/{keyId}/metadata/{version}` | `SetKeyMetadataAsync(KeyMetadata)` |
| `GET` | `keys/{keyId}/salt/{version}` | `GetSaltForVersionAsync` |
| `POST` | `keys/add` / `keys/add-string` | `AddKeyAsync` / `AddKeyFromStringAsync` |
| `POST` | `keys/update` / `keys/update-string` | `UpdateKeyAsync` / `UpdateKeyFromStringAsync` |
| `POST` | `keys/set-current` | `SetCurrentVersionAsync(KeyId, Version)` |

`access-links` returns relative paths (`Workbench/FileStorage/files/access/{token}/download` and `…/presigned-read`); both routes call
`IFileDownloadAccessService.ValidateAndConsumeDownloadAsync(token, user, remoteIp)` and translate `FileDownloadAccessConsumeFailureReason` to 400/403/404/410/429 via
`MapFailureStatusCode`.

After every mutating call (`save`, `save-stream`, `direct-upload/complete`, `stage/commit`, `multipart/complete`, `copy`, `DELETE files/{id}`) the handler calls
`cache.InvalidateQueryCacheAsync<FileMetadataEntity>()` so the read-only QueryProject endpoint stays consistent. `FileMetadataQueryCacheInvalidationHandler` performs the same
invalidation when audit events flow in from the file storage layer.

## Direct file upload

`BuildDirectFileUploadEndpoint` adds `POST upload/file` (`Constants.DirectFileUpload.FilePath`, tagged `DirectFileUpload`) which shares `SaveStreamFromFormAsync` with `Workbench/FileStorage/files/save-stream`. Same query string contract (`originalFileName`, `compress`, `encrypt`, `keyId`, `pathPrefix`, `chunkSize`, `contentType`, `tenantId`), anti-forgery disabled, useful when callers don't want to nest under the workbench prefix.

## Staged file upload

Two-phase uploads live under `Workbench/FileStorage/stage/*` (see endpoint table above). State is stored in **`staged_file_upload`**, not **`file_metadata`**, until * *`stage/{id}/commit`**. Local backends accept **`PUT stage/{stageId}/put`** on this host; S3/Blob return presigned URLs from **`stage/begin`**. Register **`IStagedFileUploadStore` ** via Postgres/Sqlite metadata builders (or in-memory for dev). Hook **`IStagedFileUploadEventHandler`** to enqueue async commit workers after **`UploadCompleted`**.

## FileMetadata Query/QueryProject

`BuildFileStorageWorkbenchFileMetadataQuery` registers a read-only Lyo.Api builder over `FileMetadataStoreDbContext` / `FileMetadataEntity` at `Constants.FileStorageWorkbench.FileMetadata` (`Workbench/FileStorage/FileMetadata`). The standard `/QueryConcrete` and `/QueryProject` routes are produced by `WithReadOnlyEndpoints()`, allowing anonymous access — the Gateway's `Lyo.Query.Web.Components` grids POST against this route directly.

## Configuration sections

| Section | Used by |
| --------------------------------------------------------- | --------------------------------------------------------------------------------------- |
| `ConnectionStrings:Postgres` | Every Postgres `AddXxxDbContextFactory` (default: `Host=localhost;Port=5437;…`) |
| `Redis:ConnectionString` *(or `ConnectionStrings:Redis`)* | Switches `IDistributedLock` to Redis; falls back to `AddLocalLock()` |
| `AwsKeyStore` | KEK / two-key encryption for `gateway-filestorage` |
| `S3FileStorageOptions` | `AddS3FileStorageServiceKeyed("gateway-filestorage").ConfigureS3FileStorage()` |
| `PostgresFileMetadataStore` | File metadata DB (`ConnectionString`, `EnableAutoMigrations`) |
| `QueryOptions` | `Lyo.Api` query cache + split-query toggle |
| `JobScheduler` | Job dashboard wiring (scheduler itself is opt-in via the commented `AddJobScheduler()`) |
| `CacheOptions` | `AddLocalCacheFromConfiguration` (query cache granularity, payload compression) |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api` — (direct, lyo)
- `Lyo.Api.Export` — (direct, lyo)
- `Lyo.Api.Export.Csv` — (direct, lyo)
- `Lyo.Api.Export.Xlsx` — (direct, lyo)
- `Lyo.Api.Reporting` — (direct, lyo)
- `Lyo.Audit.Postgres` — (direct, lyo)
- `Lyo.Authentication` — (direct, lyo)
- `Lyo.Authentication.AspNetCore` — (direct, lyo)
- `Lyo.Authentication.Google` — (direct, lyo)
- `Lyo.Authentication.Keycloak` — (direct, lyo)
- `Lyo.Authentication.OpenIdConnect` — (direct, lyo)
- `Lyo.Authentication.Postgres` — (direct, lyo)
- `Lyo.Comic.Postgres` — (direct, lyo)
- `Lyo.Comment.Postgres` — (direct, lyo)
- `Lyo.Compression` — (direct, lyo)
- `Lyo.Config.Postgres` — (direct, lyo)
- `Lyo.ContactUs.Postgres` — (direct, lyo)
- `Lyo.Discord.Postgres` — (direct, lyo)
- `Lyo.Email.Postgres` — (direct, lyo)
- `Lyo.Endato.Postgres` — (direct, lyo)
- `Lyo.FileMetadataStore.Postgres` — (direct, lyo)
- `Lyo.FileStorage` — (direct, lyo)
- `Lyo.FileStorage.S3` — (direct, lyo)
- `Lyo.IO.Temp` — (direct, lyo)
- `Lyo.Job.Postgres` — (direct, lyo)
- `Lyo.Job.Scheduler` — (direct, lyo)
- `Lyo.Keystore` — (direct, lyo)
- `Lyo.Keystore.Aws` — (direct, lyo)
- `Lyo.Lock` — (direct, lyo)
- `Lyo.Lock.Redis` — (direct, lyo)
- `Lyo.MessageQueue.RabbitMq` — (direct, lyo)
- `Lyo.Note.Postgres` — (direct, lyo)
- `Lyo.People.Postgres` — (direct, lyo)
- `Lyo.Rating.Postgres` — (direct, lyo)
- `Lyo.Reporting.Models` — (direct, lyo)
- `Lyo.Reporting.Postgres` — (direct, lyo)
- `Lyo.ShortUrl.Postgres` — (direct, lyo)
- `Lyo.Sms.Twilio.Postgres` — (direct, lyo)
- `Lyo.Tag.Postgres` — (direct, lyo)
- `Mapster` `10.0.10` — (direct, third-party)
- `Mapster.DependencyInjection` `10.0.10` — (direct, third-party)
- `Microsoft.AspNetCore.OpenApi` `10.0.5` — (direct, microsoft)
- `Microsoft.EntityFrameworkCore` `10.0.5` — (direct, microsoft)
- `Npgsql` `10.0.3` — (direct, third-party)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` — (direct, third-party)
- `Scalar.AspNetCore` `2.16.11` — (direct, third-party)
- `Lyo.Api.Client` — (transitive, lyo)
- `Lyo.Api.Models` — (transitive, lyo)
- `Lyo.Audit` — (transitive, lyo)
- `Lyo.Authentication.Models` — (transitive, lyo)
- `Lyo.Cache` — (transitive, lyo)
- `Lyo.Comic` — (transitive, lyo)
- `Lyo.Comment` — (transitive, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Config` — (transitive, lyo)
- `Lyo.ContactUs` — (transitive, lyo)
- `Lyo.ContentThreatScan` — (transitive, lyo)
- `Lyo.Csv` — (transitive, lyo)
- `Lyo.Csv.Models` — (transitive, lyo)
- `Lyo.DataTable.Models` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Diagnostic` — (transitive, lyo)
- `Lyo.Diff` — (transitive, lyo)
- `Lyo.Discord.Models` — (transitive, lyo)
- `Lyo.Encryption` — (transitive, lyo)
- `Lyo.EntityReference.Models` — (transitive, lyo)
- `Lyo.EntityReference.Postgres` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.FileMetadataStore` — (transitive, lyo)
- `Lyo.Formatter` — (transitive, lyo)
- `Lyo.Geolocation.Models` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.Health` — (transitive, lyo)
- `Lyo.Job.Models` — (transitive, lyo)
- `Lyo.MessageQueue` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.Note` — (transitive, lyo)
- `Lyo.PackageMetadata` — (transitive, lyo)
- `Lyo.People.Models` — (transitive, lyo)
- `Lyo.Postgres` — (transitive, lyo)
- `Lyo.Query` — (transitive, lyo)
- `Lyo.Query.Models` — (transitive, lyo)
- `Lyo.Rating` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Schedule.Models` — (transitive, lyo)
- `Lyo.Scheduler` — (transitive, lyo)
- `Lyo.ShortUrl` — (transitive, lyo)
- `Lyo.Sms` — (transitive, lyo)
- `Lyo.Sms.Models` — (transitive, lyo)
- `Lyo.Sms.Twilio` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `Lyo.Tag` — (transitive, lyo)
- `Lyo.Validation` — (transitive, lyo)
- `Lyo.Xlsx` — (transitive, lyo)
- `Lyo.Xlsx.Models` — (transitive, lyo)
- `AWSSDK.Core` `4.0.100.4` — (transitive, third-party)
- `AWSSDK.S3` `4.0.101` — (transitive, third-party)
- `AWSSDK.SecretsManager` `4.0.100.3` — (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party, netstandard2.0)
- `ClosedXML` `0.105.0` — (transitive, third-party)
- `DocumentFormat.OpenXml` `3.1.1` — (transitive, third-party)
- `EasyCompressor` `2.1.0` — (transitive, third-party)
- `ExcelDataReader` `3.9.0` — (transitive, third-party)
- `ExcelDataReader.DataSet` `3.9.0` — (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.AspNetCore.Authorization` `10.0.5` — (transitive, microsoft)
- `Microsoft.AspNetCore.Http.Abstractions` `2.*` — (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
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
- `RabbitMQ.Client` `7.2.1` — (transitive, third-party)
- `SmartFormat.NET` `3.6.1` — (transitive, third-party)
- `StackExchange.Redis` `2.12.0` — (transitive, third-party)
- `System.Buffers` `4.6.1` — (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` — (transitive, microsoft)
- `System.Diagnostics.DiagnosticSource` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` — (transitive, microsoft)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `Twilio` `7.14.9` — (transitive, third-party)