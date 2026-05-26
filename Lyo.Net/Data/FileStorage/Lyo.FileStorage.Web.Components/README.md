# Lyo.FileStorage.Web.Components

Blazor (Server / Interactive) UI for **`Lyo.FileStorage`** — workbench grids and dialogs for exploring file metadata, generating download access links, and managing two-key
encryption keys.

Two integration modes are supported, controlled by **`FileStorageWorkbenchOptions`**:

1. **Proxy mode** — talks to a backend host (typically **`Lyo.TestApi`**) over **`Lyo.Api.Client.IApiClient`**. The workbench never sees raw storage credentials.
2. **In-process mode** — host auto-registers a keyed file-storage stack (S3 + AWS Secrets Manager keystore + Postgres metadata) directly inside the Blazor app.

## Components

| Component                                                               | Role                                                                                                                                               |
|-------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------|
| **`FileStorageWorkbench`** (`FileStorageWorkbench.razor` / `.razor.cs`) | Top-level workbench surface with Files / Keys tabs and registration sanity checks.                                                                 |
| **`FileStorageWorkbenchHeader`**                                        | Title bar + tab selector.                                                                                                                          |
| **`FileStorageRegistrationAlerts`**                                     | Warns when required services (`IFileStorageService`, `IKeyStore`, `IFileStorageWorkbenchQueryService`, `IApiClient`) are missing or misconfigured. |
| **`FileStoreFilesTab`** (`.razor`, `.razor.cs`, `.razor.css`)           | Searchable file metadata grid with row actions (download via access link, view metadata dialog, copy, delete).                                     |
| **`FileStoreKeysTab`** (`.razor`, `.razor.cs`)                          | Keystore listing with current-version highlighting, file count per key, and **DEK migrate / rotate** controls.                                     |
| **`FileStoreAccessLinkDialog`**                                         | Confirms link parameters and shows the issued token + expiry.                                                                                      |
| **`FileStoreMetadataDialog`**                                           | Read-only metadata viewer (encryption header, hashes, audit fields, deletion state).                                                               |
| **`FileStorageGridRowHelper`**                                          | Shared row formatter for both tabs.                                                                                                                |

## Services

| Type                                      | Purpose                                                                                                                                                                                                                                |
|-------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`IFileStorageWorkbenchQueryService`**   | Searches files (`FileStorageWorkbenchFileQuery`) and keys (`FileStorageWorkbenchKeyQuery`) and projects to `FileStoreResult` / `FileStorageWorkbenchKeyRecord`. The Test API ships an HTTP-backed implementation in **`Lyo.Gateway`**. |
| **`FileStorageWorkbenchServiceResolver`** | Centralises lookup of `IFileStorageService` and `IKeyStore` against the configured key names so the rest of the UI is decoupled from keyed-DI mechanics.                                                                               |
| **`FileStorageWorkbenchOptions`**         | Binds the **`FileStorageWorkbench`** configuration section.                                                                                                                                                                            |

## Access link API models

`FileStorageWorkbenchAccessLinkApiModels.cs` defines the request/response shapes the workbench uses to call the host's access-link endpoint (implemented by *
*`Lyo.FileMetadataStore.Postgres.IFileDownloadAccessService`** in proxy mode):

- **`FileStorageWorkbenchCreateAccessLinkRequest`** — `FileId`, optional `ExpiresIn`, audit fields (`Actor`, `Reason`).
- **`FileStorageWorkbenchAccessLinkResponse`** — opaque token, absolute download URL, UTC expiry.

## Options — `FileStorageWorkbenchOptions`

Default section: `FileStorageWorkbench`.

| Property                     | Default                        | Notes                                                                                                     |
|------------------------------|--------------------------------|-----------------------------------------------------------------------------------------------------------|
| `UseTestApiServices`         | `true`                         | Resolve the workbench through HTTP calls to a Test API via **`Lyo.Api.Client.IApiClient`**.               |
| `AutoRegisterS3Services`     | `false`                        | Host auto-registers AWS Secrets Manager keystore + S3 file storage stack from configuration.              |
| `ApiRoutePrefix`             | `Workbench/FileStorage`        | Route prefix on the API used for the workbench endpoints.                                                 |
| `StreamUploadRelativePath`   | `upload/file`                  | Endpoint for multipart streaming uploads; set empty to fall back to `{ApiRoutePrefix}/files/save-stream`. |
| `FileStorageServiceKey`      | `gateway-filestorage`          | Keyed service name used when resolving `IFileStorageService`.                                             |
| `KeyStoreServiceKey`         | `gateway-filestorage`          | Keyed service name used when resolving `IKeyStore`.                                                       |
| `MetadataStoreKey`           | `gateway-filestorage-metadata` | Keyed metadata store name used when auto-registering the S3 stack.                                        |
| `AwsKeyStoreConfigSection`   | `AwsKeyStore`                  | Configuration section for AWS Secrets Manager keystore settings.                                          |
| `S3FileStorageConfigSection` | `S3FileStorageOptions`         | Configuration section that binds `S3FileStorageOptions`.                                                  |
| `MetadataStoreConfigSection` | `PostgresFileMetadataStore`    | Configuration section for the backing metadata store.                                                     |

## Host integration

Workbench wiring lives in the host project (currently **`Lyo.Gateway`** via `AddFileStorageWorkbenchSupport`), not in this package. A host typically:

1. Binds **`FileStorageWorkbenchOptions`** from configuration.
2. Registers an `IApiClient` (proxy mode) **or** registers keyed `IFileStorageService` + `IKeyStore` + `IFileMetadataStore` (in-process mode).
3. Registers an implementation of **`IFileStorageWorkbenchQueryService`** — the Gateway provides `TestApiFileStorageWorkbenchQueryService` for proxy mode.
4. Adds MudBlazor (`AddMudServices`) and the **`Lyo.Web.Components`** dialog/snackbar plumbing.
5. Mounts **`<FileStorageWorkbench />`** on a route.

## Related projects

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.FileMetadataStore`](../../FileMetadataStore/Lyo.FileMetadataStore/README.md)
- [`Lyo.FileStorage`](../Lyo.FileStorage/README.md)
- [`Lyo.Hashing`](../../../Security/Hashing/Lyo.Hashing/README.md)
- [`Lyo.IO.Temp`](../../IOTemp/Lyo.IO.Temp/README.md)
- [`Lyo.Api.Client`](../../../Integration/Api/Lyo.Api.Client/README.md)
- [`Lyo.Keystore`](../../../Security/Encryption/Lyo.Keystore/README.md)
- [`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md)
