# Lyo.TestApi

Minimal-API host that backs `Lyo.Gateway` and `Lyo.TestConsole`. It wires the Lyo Postgres stores, RabbitMQ-driven job system, S3 file storage with two-key encryption, and exposes
the file storage workbench surface that the Gateway's `/filestorage-workbench` page talks to.

## Host setup (`Program.cs`)

A standard `WebApplication` with the following pipeline:

- **Kestrel** — `MaxRequestBodySize = long.MaxValue` and matching `FormOptions.MultipartBodyLengthLimit` so multi-GB direct uploads stream cleanly.
- **OpenAPI / Scalar** — `AddOpenApi()`. In development, `MapOpenApi()` and `MapScalarApiReference()` are mounted so `/scalar/v1` (or whatever Scalar picks) shows every endpoint.
- **Compression** — Brotli + Gzip response compression (level `Fastest`) and request decompression so Gateway calls can ship Brotli payloads.
- **JSON** — `LyoJsonSerializerOptions.ApplyTo` + `AddLyoDateOnlyModelConverters` + `ReferenceHandler.IgnoreCycles` + `JsonIgnoreCondition.WhenWritingNull`.
- **Infra** — `AddMetrics`, `AddFormatterService`, `AddCsvService`, `AddXlsxService`, `AddCompressionService` + `AddDefaultCompressionService<CompressionService>` (registers
  `ICompressionResolver` for file-storage codec dispatch), `AddLocalCacheFromConfiguration`, `AddHttpContextAccessor`, Mapster
  via `ConfigureMapster()`.
- **Locks** — `AddRedisLock` if `Redis:ConnectionString` (or `ConnectionStrings:Redis`) is set, otherwise `AddLocalLock()`.
- **Messaging** — `SetupRabbitMqServiceFromConfiguration` + `AddMqJobEventPublisher` (job state changes flow through MQ).
- **Postgres stores** — A single `ConnectionStrings:Postgres` is shared by `Job`, `People`, `TwilioSms`, `Discord`, `Config`, `Comic`, `FileMetadataStore`. Every
  `AddXxxDbContextFactory` is called with `EnableAutoMigrations = true` so a fresh database is brought up on first run.
- **CRUD/Query** — `AddLyoCrudServices<TContext>()` for `JobContext`, `PeopleDbContext`, `TwilioSmsDbContext`, `FileMetadataStoreDbContext`; `AddLyoQueryServices()`;
  `AddLyoApiExport<TContext>()` + `AddCsvExport()` / `AddXlsxExport()` for `PeopleDbContext`, `DiscordDbContext`, `JobContext`; `AddPostgresSprocService<PeopleDbContext>()`.
- **File storage** — `AddTwoKeyEncryptionFromConfiguration(…, Constants.FileStorageWorkbench.ServiceKey, "AwsKeyStore")` +
  `AddPostgresFileMetadataStoreKeyed("gateway-filestorage-metadata")` reading `PostgresFileMetadataStore` (falls back to the shared Postgres connection if not set) +
  `AddS3FileStorageServiceKeyed("gateway-filestorage")` chained with `UseFileMetadataStore`, `UseEncryptionService`, `ConfigureS3FileStorage()`.
- **Audit** — `AddPostgresFileAuditSink()` writes to the file metadata DB; `AddScoped<IFileAuditEventHandler, FileMetadataQueryCacheInvalidationHandler>` invalidates the
  `Lyo.Cache` QueryProject cache for `FileMetadataEntity` on successful `Save`/`Delete`/`MultipartComplete` and on any `MigrateDeks` / `RotateDeks` so workbench grids see fresh
  rows.
- **Scheduler** — `AddJobScheduler()` is commented out; jobs are queued by MQ but not polled by this host.

The pipeline ends with `app.UseResponseCompression()` → `app.UseRequestDecompression()` → `app.SetupCourtCanaryEndpoints()` → `app.Run()`.

## `SetupCourtCanaryEndpoints` (root extension)

`SetupEndpoints.cs` adds one extension method on `WebApplication` that chains every endpoint group:

```text
BuildJobGroup            ← from Lyo.Job.Postgres (full job/job-run CRUD)
BuildJobServiceEndpoints ← custom Job/Run/Create, Job/Run/{id}/Cancel, Job/Run/{id}/Rerun
BuildPersonGroup         ← Person CRUD (Lyo.Api builder) + info/{schema}/{table}/{column}/GetUniqueCounts
BuildDiscordGroup        ← Discord dynamic CRUD
BuildTwilioGroup         ← Twilio dynamic CRUD (route prefix "Twilio")
BuildFileStorageWorkbenchGroup    ← MapGroup("Workbench/FileStorage") (see below)
BuildDirectFileUploadEndpoint     ← POST upload/file (mirrors files/save-stream)
BuildFileStorageWorkbenchFileMetadataQuery ← ReadOnly Lyo.Api builder over FileMetadataEntity at "Workbench/FileStorage/FileMetadata"
```

Custom Job endpoints exist because the dynamic CRUD route for `JobRun` does not expose Create/Cancel — those need to go through MQ via `JobService`.

The Person group uses `Lyo.Api` `CreateBuilder<…>` with `WithFlags(All | UpsertInheritCreate | UpsertInheritUpdate | PatchInheritsUpdate)`, a `BeforeCreate` that assigns
`LyoGuid.CreateCombPostgres()`, plus `WithMetadata` / `WithProjectionComputedFields`. It also adds a `GET info/{schema}/{table}/{column}/GetUniqueCounts` route that invokes
`StoredProcedures.Info.UniqueValuesWithCount` via `ISprocService`.

## File Storage Workbench group (`Workbench/FileStorage`)

`FileStorageWorkbench/SetupFileStorageWorkbenchEndpoints.cs` mounts the workbench group at `Constants.FileStorageWorkbench.Route` (`Workbench/FileStorage`) with the
`FileStorageWorkbench` OpenAPI tag. All endpoints resolve services as **keyed** — `IFileStorageService`, `IMultipartUploadService`, and `IKeyStore` (cast to `IKeyInventoryStore`
where available) under `Constants.FileStorageWorkbench.ServiceKey` (`gateway-filestorage`).

| Method   | Route                                            | Purpose                                                                                                                 |
|----------|--------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------|
| `GET`    | `health`                                         | `IFileStorageService.CheckHealthAsync`                                                                                  |
| `POST`   | `files/save`                                     | `SaveFileAsync` from JSON `SaveFileRequest` (bytes in-band)                                                             |
| `POST`   | `files/save-stream`                              | Multipart `IFormFile` → `SaveFromStreamAsync` (anti-forgery disabled)                                                   |
| `POST`   | `files/copy`                                     | `CopyFileAsync(SourceFileId, CopyFileRequest?)`                                                                         |
| `POST`   | `files/{fileId:guid}/access-links`               | `IFileDownloadAccessService.CreateLinkAsync` → `{linkId, token, downloadUrl, presignedReadUrl}`                         |
| `GET`    | `files/{fileId:guid}/metadata`                   | `GetMetadataAsync`                                                                                                      |
| `GET`    | `files/{fileId:guid}/download`                   | Plain files: try `GetPreSignedReadUrlAsync` 302; encrypted/compressed or no presign: stream decrypted bytes             |
| `GET`    | `files/{fileId:guid}/presigned-read`             | `GetPreSignedReadUrlAsync(expiresHours, pathPrefix, contentDisposition, contentType)`                                   |
| `DELETE` | `files/{fileId:guid}`                            | `DeleteFileAsync`                                                                                                       |
| `POST`   | `files/migrate-deks`                             | `MigrateDeksAsync` (rotate every DEK that uses a source key)                                                            |
| `POST`   | `files/rotate-deks`                              | `RotateDeksAsync(fileIds, targetKey)`                                                                                   |
| `GET`    | `files/search?searchText&keyId&keyVersion&take`  | EF query against `FileMetadataEntity` filtered by ILike on filename/source/path, plus key id/version                    |
| `POST`   | `direct-upload/begin`                            | `BeginDirectUploadAsync` (presigned/PUT contract)                                                                       |
| `PUT`    | `direct-upload/{fileId:guid}/put`                | Only when keyed service is `LocalFileStorageService` — `ReceiveWorkbenchDirectPutAsync`; otherwise 501                  |
| `POST`   | `direct-upload/{fileId:guid}/complete`           | `CompleteDirectUploadAsync`                                                                                             |
| `POST`   | `stage/begin`                                    | `IStagedFileUploadService.BeginAsync` (presigned PUT to staging key)                                                    |
| `PUT`    | `stage/{stageId:guid}/put`                       | Local only — `ReceiveWorkbenchStagePutAsync`; otherwise 501                                                             |
| `POST`   | `stage/{stageId:guid}/complete`                  | `CompleteAsync` (verify/hash staged object)                                                                             |
| `POST`   | `stage/{stageId:guid}/commit`                    | `CommitAsync` (compress/encrypt → `file_metadata`)                                                                      |
| `POST`   | `stage/{stageId:guid}/abort`                     | `AbortAsync`                                                                                                            |
| `GET`    | `stage/{stageId:guid}`                           | `GetAsync`                                                                                                              |
| `POST`   | `multipart/begin`                                | `IMultipartUploadService.BeginAsync` from `BeginMultipartWorkbenchRequest`                                              |
| `GET`    | `multipart/{sessionId:guid}/part-url?partNumber` | `GetPresignedPartUploadAsync`                                                                                           |
| `POST`   | `multipart/complete`                             | `CompleteAsync(SessionId, Parts[])`                                                                                     |
| `POST`   | `multipart/{sessionId:guid}/abort`               | `AbortAsync`                                                                                                            |
| `GET`    | `diagnostics/keys?prefix&maxKeys`                | `IFileStorageDiagnosticsService.ListStorageKeysAsync` clamped to 1–10 000; 501 if backend doesn't implement diagnostics |
| `GET`    | `keys/search?searchText&take`                    | Distinct `(KeyId, Version)` pairs that have files, with `IsCurrent`, `KeyMetadata`, `FileCount`                         |
| `GET`    | `keys/available`                                 | `IKeyInventoryStore.GetAvailableKeyIdsAsync` (empty if store isn't `IKeyInventoryStore`)                                |
| `GET`    | `keys/{keyId}/versions`                          | `GetAvailableVersionsAsync`                                                                                             |
| `GET`    | `keys/{keyId}/raw?version`                       | `IKeyStore.GetKeyAsync` (raw bytes; intentionally exposed for the workbench)                                            |
| `GET`    | `keys/{keyId}/exists?version`                    | `HasKeyAsync`                                                                                                           |
| `GET`    | `keys/{keyId}/current-version`                   | `GetCurrentVersionAsync`                                                                                                |
| `GET`    | `keys/{keyId}/metadata/{version}`                | `GetKeyMetadataAsync`                                                                                                   |
| `PUT`    | `keys/{keyId}/metadata/{version}`                | `SetKeyMetadataAsync(KeyMetadata)`                                                                                      |
| `GET`    | `keys/{keyId}/salt/{version}`                    | `GetSaltForVersionAsync`                                                                                                |
| `POST`   | `keys/add` / `keys/add-string`                   | `AddKeyAsync` / `AddKeyFromStringAsync`                                                                                 |
| `POST`   | `keys/update` / `keys/update-string`             | `UpdateKeyAsync` / `UpdateKeyFromStringAsync`                                                                           |
| `POST`   | `keys/set-current`                               | `SetCurrentVersionAsync(KeyId, Version)`                                                                                |

`access-links` returns relative paths (`Workbench/FileStorage/files/access/{token}/download` and `…/presigned-read`); both routes call
`IFileDownloadAccessService.ValidateAndConsumeDownloadAsync(token, user, remoteIp)` and translate `FileDownloadAccessConsumeFailureReason` to 400/403/404/410/429 via
`MapFailureStatusCode`.

After every mutating call (`save`, `save-stream`, `direct-upload/complete`, `stage/commit`, `multipart/complete`, `copy`, `DELETE files/{id}`) the handler calls
`cache.InvalidateQueryCacheAsync<FileMetadataEntity>()` so the read-only QueryProject endpoint stays consistent. `FileMetadataQueryCacheInvalidationHandler` performs the same
invalidation when audit events flow in from the file storage layer.

## Direct file upload

`BuildDirectFileUploadEndpoint` adds `POST upload/file` (`Constants.DirectFileUpload.FilePath`, tagged `DirectFileUpload`) which shares `SaveStreamFromFormAsync` with
`Workbench/FileStorage/files/save-stream`. Same query string contract (`originalFileName`, `compress`, `encrypt`, `keyId`, `pathPrefix`, `chunkSize`, `contentType`, `tenantId`),
anti-forgery disabled, useful when callers don't want to nest under the workbench prefix.

## Staged file upload

Two-phase uploads live under `Workbench/FileStorage/stage/*` (see endpoint table above). State is stored in **`staged_file_upload`**, not **`file_metadata`**, until *
*`stage/{id}/commit`**. Local backends accept **`PUT stage/{stageId}/put`** on this host; S3/Blob return presigned URLs from **`stage/begin`**. Register **`IStagedFileUploadStore`
** via Postgres/Sqlite metadata builders (or in-memory for dev). Hook **`IStagedFileUploadEventHandler`** to enqueue async commit workers after **`UploadCompleted`**.

## FileMetadata Query/QueryProject

`BuildFileStorageWorkbenchFileMetadataQuery` registers a read-only Lyo.Api builder over `FileMetadataStoreDbContext` / `FileMetadataEntity` at
`Constants.FileStorageWorkbench.FileMetadata` (`Workbench/FileStorage/FileMetadata`). The standard `/Query` and `/QueryProject` routes are produced by `WithReadOnlyEndpoints()`,
allowing anonymous access — the Gateway's `Lyo.Query.Web.Components` grids POST against this route directly.

## Configuration sections

| Section                                                   | Used by                                                                                 |
|-----------------------------------------------------------|-----------------------------------------------------------------------------------------|
| `ConnectionStrings:Postgres`                              | Every Postgres `AddXxxDbContextFactory` (default: `Host=localhost;Port=5437;…`)         |
| `Redis:ConnectionString` *(or `ConnectionStrings:Redis`)* | Switches `IDistributedLock` to Redis; falls back to `AddLocalLock()`                    |
| `AwsKeyStore`                                             | KEK / two-key encryption for `gateway-filestorage`                                      |
| `S3FileStorageOptions`                                    | `AddS3FileStorageServiceKeyed("gateway-filestorage").ConfigureS3FileStorage()`          |
| `PostgresFileMetadataStore`                               | File metadata DB (`ConnectionString`, `EnableAutoMigrations`)                           |
| `QueryOptions`                                            | `Lyo.Api` query cache + split-query toggle                                              |
| `JobScheduler`                                            | Job dashboard wiring (scheduler itself is opt-in via the commented `AddJobScheduler()`) |
| `CacheOptions`                                            | `AddLocalCacheFromConfiguration` (query cache granularity, payload compression)         |

## Related projects

- [`Lyo.Api`](../../Integration/Api/Lyo.Api/README.md)
- [`Lyo.Audit.Postgres`](../../Core/Audit/Lyo.Audit.Postgres/README.md)
- [`Lyo.Comic.Postgres`](../../Features/Comic/Lyo.Comic.Postgres/README.md)
- [`Lyo.Comment.Postgres`](../../Features/Comment/Lyo.Comment.Postgres/README.md)
- [`Lyo.Compression`](../../Data/Compression/Lyo.Compression/README.md)
- [`Lyo.Config.Postgres`](../../Features/Config/Lyo.Config.Postgres/README.md)
- [`Lyo.ContactUs.Postgres`](../../Features/ContactUs/Lyo.ContactUs.Postgres/README.md)
- [`Lyo.Discord.Postgres`](../../Integration/Discord/Lyo.Discord.Postgres/README.md)
- [`Lyo.Email.Postgres`](../../Communication/Email/Lyo.Email.Postgres/README.md)
- [`Lyo.Endato.Postgres`](../../Integration/Endato/Lyo.Endato.Postgres/README.md)
- [`Lyo.FileMetadataStore.Postgres`](../../Data/FileMetadataStore/Lyo.FileMetadataStore.Postgres/README.md)
- [`Lyo.FileStorage.S3`](../../Data/FileStorage/Lyo.FileStorage.S3/README.md)
- [`Lyo.FileStorage`](../../Data/FileStorage/Lyo.FileStorage/README.md)
- [`Lyo.Job.Postgres`](../../Integration/Job/Lyo.Job.Postgres/README.md)
- [`Lyo.Job.Scheduler`](../../Integration/Job/Lyo.Job.Scheduler/README.md)
- [`Lyo.Keystore.Aws`](../../Security/Encryption/Lyo.Keystore.Aws/README.md)
- [`Lyo.Keystore`](../../Security/Encryption/Lyo.Keystore/README.md)
- [`Lyo.MessageQueue.RabbitMq`](../../Communication/MessageQueue/Lyo.MessageQueue.RabbitMq/README.md)
- [`Lyo.Note.Postgres`](../../Features/Note/Lyo.Note.Postgres/README.md)
- [`Lyo.People.Postgres`](../../Core/People/Lyo.People.Postgres/README.md)
- [`Lyo.Rating.Postgres`](../../Features/Rating/Lyo.Rating.Postgres/README.md)
- [`Lyo.ShortUrl.Postgres`](../../Features/ShortUrl/Lyo.ShortUrl.Postgres/README.md)
- [`Lyo.Sms.Twilio.Postgres`](../../Communication/Sms/Lyo.Sms.Twilio.Postgres/README.md)
- [`Lyo.Tag.Postgres`](../../Features/Tag/Lyo.Tag.Postgres/README.md)
