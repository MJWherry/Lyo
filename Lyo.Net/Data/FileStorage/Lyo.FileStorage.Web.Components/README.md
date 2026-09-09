# Lyo.FileStorage.Web.Components

Blazor Server / Interactive UI for Lyo.FileStorage. Trees, grids, and dialogs for file metadata, expected storage keys, download access links, and DEK migrate/rotate.

Files, Tree, and Browser talk only to a backend host (typically Lyo.Gateway.Api, or Lyo.TestApi for kitchen-sink) over Lyo.Api.Client.IApiClient. The UI never sees raw storage credentials and does not resolve IFileStorageService or IKeyStore. Encryption key ids come from GET {prefix}/key-ids. Keystore CRUD is not part of this package.

## Components

| Component | Role |
| ----------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| FileStorageManagement (FileStorageManagement.razor / .razor.cs) | Top-level UI with Files / Tree / Browser tabs plus an API health check. |
| FileStorageManagementHeader | Title bar. |
| FileStorageRegistrationAlerts | Shows the IApiClient base URL and GET {prefix}/health. |
| FileStoreFilesTab (.razor, .razor.cs, .razor.css) | Stream, direct PUT, and multipart upload plus DEK/KEK migrate and rotate via IApiClient. File listing lives on the Tree and Browser tabs. Key id fields load identifiers from GET {prefix}/key-ids. |
| FileStoreKeyIdField | MudSelect over GET key-ids (text field fallback when the list is empty). Used by upload, migrate, rotate, and protocol flows. |
| FileStoreProtocolUploads | Direct and multipart kitchen-sink flows: begin, PUT to the returned URL, then complete. |
| FileStorageTreeBrowser (.razor, .razor.cs, .razor.css) | Tree tab: MudGrid split with a PathPrefix folder tree on the left and a path inspector on the right. Builds the full folder/file tree from FileMetadata QueryProject rows. Subscribes to FilesChanged. |
| FileStoragePathTree | MudTreeView of virtual folders and file leaves. Quick search, edit-mode multi-select with bulk Move / Rotate DEKs / Delete / Download zip (folders include descendant files), drag-and-drop onto a folder to move. Soft-deleted files are omitted. |
| FileStoragePathInspector | Directory: breadcrumb, new folder, multi-file browse with selectable chips, per-file original name / compress / encrypt / upload, contents list. File: metadata table plus view / access link / download / move / copy / rename / rotate DEK / delete. |
| FileStoragePathTreeNode / FileStoragePathTreeBuilder | Non-UI tree model: split PathPrefix segments, build the full folder/file tree, collect descendant file ids / drag-and-drop destinations, and keep pending folders until a file is uploaded. |
| FileStorageBrowser (.razor, .razor.cs, .razor.css) | Two LyoDataGridProjected views over QueryProject file metadata (active files only): operator columns (type / encryption / compression as color-coded chips), and expected storage keys from metadata (same shard layout as the storage engine). Row and bulk actions (view, access link, download, move, copy, rename, rotate DEK, delete). Optional Exists chip from GET diagnostics/storage-keys. |
| FileStorageBrowserActions | Shared handlers and dialogs used by Browser grids and the Tree inspector (Guid overloads plus projected-row wrappers). Tree bulk selection and drag-and-drop reuse the same move / rotate / delete / archive-download APIs. All mutations go through IApiClient. |
| FileMetadataGrid | Host-agnostic QueryProject grid over active file metadata. View (CSV/XLSX/HTML preview, else new tab), view metadata, download, zip, create access link. Soft-deleted files are omitted. Parameters: ApiRoutePrefix, FileMetadataQueryRoute, PublicBaseUrl, ShowAdvancedAccessLink. |
| FileAccessLinkDialog (`Lyo.FileStorage.Web.Components.FileAccessLink`) | Create-link dialog (POST {ApiRoutePrefix}/files/{id}/access-links) that shows copyable public URLs. |
| FileStoreMetadataTable | Read-only metadata fields (encryption header, hashes, audit fields, deletion state). Shared by the dialog and the Tree inspector. |
| FileStoreMetadataDialog | Dialog that wraps FileStoreMetadataTable. |
| FileStoreMoveDialog, FileStoreCopyDialog, FileStoreRenameDialog, FileStoreRotateDekDialog | Path-prefix, display-name, and DEK rotation prompts for Browser row and bulk actions. |
| FileStorageGridRowHelper | Projected-row helpers (file id, active-only QueryProject filter, expected storage key) used by the Browser grids. |
| FileStorageColorHelper | MudBlazor chip colors for file-type, encryption-algorithm, and compression-algorithm cells. |

## Services

| Type | Purpose |
| --------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| FileStorageWebOptions | Binds the FileStorage configuration section (API route prefix / stream-upload path). Files and Browser always use IApiClient. |

## Access link API models

Access-link HTTP shapes live in Lyo.Api.FileStorage.Models (`CreateDownloadAccessLinkRequest` / `DownloadAccessLinkResponse`). The Browser action and FileMetadataGrid open FileAccessLinkDialog against ApiRoutePrefix.

## FileStorageWebOptions

Default section: `FileStorage`.

| Property | Default | Notes |
| -------------------------- | ------------- | ----------------------------------------------------------------------------------------------------------- |
| `ApiRoutePrefix` | `FileStorage` | API route prefix used for the file-storage endpoints. |
| `StreamUploadRelativePath` | `upload/file` | Endpoint for multipart streaming uploads; leave empty to fall back to `{ApiRoutePrefix}/files/save-stream`. |

## Host integration

- Binds FileStorageWebOptions from configuration.
- The UI requires an IApiClient. Keystore UI is a separate package (Lyo.KeyStore.Web.Components) that takes an in-process IKeyStore.
- Adds MudBlazor (AddMudServices) plus the Lyo.Web.Components dialog/snackbar plumbing.
- Mounts <FileStorageManagement /> on a route.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` (direct, lyo)
- `Lyo.Api.FileStorage.Models` (direct, lyo)
- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Csv` (direct, lyo)
- `Lyo.Csv.Models` (direct, lyo)
- `Lyo.DataTable.Models` (direct, lyo)
- `Lyo.FileMetadataStore` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `Lyo.IO.Temp` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Lyo.Web.Components` (direct, lyo)
- `Lyo.Web.Components.Export` (direct, lyo)
- `Lyo.Web.Components.Export.Csv` (direct, lyo)
- `Lyo.Web.Components.Export.Xlsx` (direct, lyo)
- `Lyo.Xlsx` (direct, lyo)
- `Lyo.Xlsx.Models` (direct, lyo)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.Configuration` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `Lyo.Web.Primitives` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Blazored.LocalStorage` `4.5.0` (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `ClosedXML` `0.105.0` (transitive, third-party)
- `DocumentFormat.OpenXml` `3.1.1` (transitive, third-party)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `ExcelDataReader` `3.9.0` (transitive, third-party)
- `ExcelDataReader.DataSet` `3.9.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.DataAnnotations` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)