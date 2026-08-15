# Lyo.FileStorage.Web.Components

Blazor (Server / Interactive) UI for **`Lyo.FileStorage`** — workbench grids and dialogs for exploring file metadata, generating download access links, and managing two-key encryption keys.

Two integration modes are supported, controlled by **`FileStorageWorkbenchOptions`**:

1. **Proxy mode** — talks to a backend host (typically **`Lyo.Gateway.Api`**, or **`Lyo.TestApi`** for kitchen-sink) over **`Lyo.Api.Client.IApiClient`**. The workbench never sees raw storage credentials. 2. **In-process mode** — host auto-registers a keyed file-storage stack (S3 + AWS Secrets Manager keystore + Postgres metadata) directly inside the Blazor app.

## Components

| Component | Role |
| ----------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`FileStorageWorkbench`** (`FileStorageWorkbench.razor` / `.razor.cs`) | Top-level workbench surface with Files / Keys tabs and registration sanity checks. |
| **`FileStorageWorkbenchHeader`** | Title bar + tab selector. |
| **`FileStorageRegistrationAlerts`** | Warns when required services (`IFileStorageService`, `IKeyStore`, `IFileStorageWorkbenchQueryService`, `IApiClient`) are missing or misconfigured. |
| **`FileStoreFilesTab`** (`.razor`, `.razor.cs`, `.razor.css`) | Searchable file metadata grid with row actions (download via access link, view metadata dialog, copy, delete). |
| **`FileStoreKeysTab`** (`.razor`, `.razor.cs`) | Keystore listing with current-version highlighting, file count per key, and **DEK migrate / rotate** controls. |
| **`FileStoreAccessLinkDialog`** | Confirms link parameters and shows the issued token + expiry. |
| **`FileStoreMetadataDialog`** | Read-only metadata viewer (encryption header, hashes, audit fields, deletion state). |
| **`FileStorageGridRowHelper`** | Shared row formatter for both tabs. |

## Services

| Type | Purpose |
| ----------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`IFileStorageWorkbenchQueryService`** | Searches files (`FileStorageWorkbenchFileQuery`) and keys (`FileStorageWorkbenchKeyQuery`) and projects to `FileStoreResult` / `FileStorageWorkbenchKeyRecord`. The Test Gateway ships an HTTP-backed implementation in **`Lyo.TestGateway`**. |
| **`FileStorageWorkbenchServiceResolver`** | Centralises lookup of `IFileStorageService` and `IKeyStore` against the configured key names so the rest of the UI is decoupled from keyed-DI mechanics. |
| **`FileStorageWorkbenchOptions`** | Binds the **`FileStorageWorkbench`** configuration section. |

## Access link API models

`FileStorageWorkbenchAccessLinkApiModels.cs` defines the request/response shapes the workbench uses to call the host's access-link endpoint (implemented by * *`Lyo.FileMetadataStore.Postgres.IFileDownloadAccessService`** in proxy mode): - **`FileStorageWorkbenchCreateAccessLinkRequest`** — `FileId`, optional `ExpiresIn`, audit fields (`Actor`, `Reason`). - **`FileStorageWorkbenchAccessLinkResponse`** — opaque token, absolute download URL, UTC expiry.

## Options — `FileStorageWorkbenchOptions`

Default section: `FileStorageWorkbench`.

| Property | Default | Notes |
| ---------------------------- | ------------------------------ | --------------------------------------------------------------------------------------------------------------------------------- |
| `UseRemoteApiServices` | `true` | Resolve the workbench through HTTP calls to a remote API via **`Lyo.Api.Client.IApiClient`**. Legacy alias: `UseTestApiServices`. |
| `AutoRegisterS3Services` | `false` | Host auto-registers AWS Secrets Manager keystore + S3 file storage stack from configuration. |
| `ApiRoutePrefix` | `Workbench/FileStorage` | Route prefix on the API used for the workbench endpoints. |
| `StreamUploadRelativePath` | `upload/file` | Endpoint for multipart streaming uploads; set empty to fall back to `{ApiRoutePrefix}/files/save-stream`. |
| `FileStorageServiceKey` | `gateway-filestorage` | Keyed service name used when resolving `IFileStorageService`. |
| `KeyStoreServiceKey` | `gateway-filestorage` | Keyed service name used when resolving `IKeyStore`. |
| `MetadataStoreKey` | `gateway-filestorage-metadata` | Keyed metadata store name used when auto-registering the S3 stack. |
| `AwsKeyStoreConfigSection` | `AwsKeyStore` | Configuration section for AWS Secrets Manager keystore settings. |
| `S3FileStorageConfigSection` | `S3FileStorageOptions` | Configuration section that binds `S3FileStorageOptions`. |
| `MetadataStoreConfigSection` | `PostgresFileMetadataStore` | Configuration section for the backing metadata store. |

## Host integration

- Binds **`FileStorageWorkbenchOptions`** from configuration.
- Registers an `IApiClient` (proxy mode) **or** registers keyed `IFileStorageService` + `IKeyStore` + `IFileMetadataStore` (in-process mode).
- Registers an implementation of **`IFileStorageWorkbenchQueryService`** — the Gateway provides `TestApiFileStorageWorkbenchQueryService` for proxy mode.
- Adds MudBlazor (`AddMudServices`) and the **`Lyo.Web.Components`** dialog/snackbar plumbing.
- Mounts **`<FileStorageWorkbench />`** on a route.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` — (direct, lyo)
- `Lyo.Common` — (direct, lyo)
- `Lyo.FileMetadataStore` — (direct, lyo)
- `Lyo.FileStorage` — (direct, lyo)
- `Lyo.Hashing` — (direct, lyo)
- `Lyo.IO.Temp` — (direct, lyo)
- `Lyo.KeyStore` — (direct, lyo)
- `Lyo.Web.Components` — (direct, lyo)
- `MudBlazor` `9.3` — (direct, third-party)
- `Lyo.Api.Models` — (transitive, lyo)
- `Lyo.Compression` — (transitive, lyo)
- `Lyo.ContentThreatScan` — (transitive, lyo)
- `Lyo.DataTable.Models` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Diagnostic` — (transitive, lyo)
- `Lyo.Encryption` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Health` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.PackageMetadata` — (transitive, lyo)
- `Lyo.Query.Models` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `Lyo.Validation` — (transitive, lyo)
- `Blazored.LocalStorage` `4.5.0` — (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` — (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.DataAnnotations` `10.0.5` — (transitive, microsoft)
- `System.Buffers` `4.6.1` — (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` — (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)