# Lyo.FileManager

`FileManagerSession` keeps a server list, the open connections, and the file, folder, transfer, and key actions. Several connections stay open. The selected one is the tree and the actions. A remote server uses `FileStorageHttpApi` (`Lyo.Http.Client` against `Lyo.Api.FileStorage.Models`). An in-process server uses `FileStorageLocalHost`: local disk, S3, Azure Blob, FTP, or SFTP, plus SQLite or Postgres for the catalog. A local SQLite file defaults to beside the folder (`/data/photos.file-metadata.db`). A path inside that folder is rejected. S3, Azure Blob, FTP, and SFTP ask for a catalog path because they have no local folder to sit beside. The session talks to `IFileManagerApi`. Transfer uses `IFileManagerTransfer`, which only an in-process store exposes. Settings, including bearer tokens, API keys, and the dock layout, persist at `$XDG_CONFIG_HOME/lyo/file-manager.json` or `%AppData%\Lyo\file-manager.json`. The log is `file-manager.log` beside that file and is also written to the console. The desktop window is `lyo-files` in `Lyo.FileManager.App`.

## Features

- **Servers.** Add, rename, and remove a remote API, a local folder, or an in-process S3, Azure Blob, FTP, or SFTP store. Each successful connect stays open, once per key store for an in-process server. Selecting a connection shows that folder. Closing it disposes the client and keeps the saved server. Authenticate stores a bearer token or an `X-Api-Key` value on a remote server. Connect calls `GET {route}/health`, or migrates the metadata database and opens the store with the key store named by `KeyStoreId`. A remote server ignores that id. A failed connect sets that connection's error and leaves its tree.
- **Catalog.** SQLite or a Postgres connection string. The default SQLite file for a local folder sits beside it (`/data/photos.file-metadata.db`). A path inside the folder is rejected. S3, Azure Blob, FTP, and SFTP require a catalog path. The selected key store supplies the passphrase or AWS Secrets Manager settings. With no key store, upload stays plaintext and encrypt or rotate reports that no key is configured. A server that still has a passphrase or AWS key fields and no `KeyStoreId` becomes a named key store on load. Query filters the metadata store by name and path prefix and returns the same row shape as remote `QueryProject`.
- **Tree and query.** List a prefix, expand a directory, and keep a new folder as a pending prefix until refresh. Entering that folder and going back up still shows it. A truncated list stays visible. `FolderTreeFilter` hides loaded branches that do not match. Remote query posts `ProjectionQueryReq` to `{FileMetadataRoute}/QueryProject`.
- **Files and folders.** Metadata, rename, move, copy, delete, access links, and presigned read for catalog files. Physical-only objects use the physical routes. A folder command lists catalog descendants and calls the file route once per id, rewriting nested prefixes (`docs/a` becomes `archive/a`). Upload can compress, encrypt, and set a key id. Files larger than 8 MiB use multipart on a local folder. Download one file, or zip many through `files/archive`.
- **Transfer.** `TransferAsync` copies or moves selected catalog files from the active connection onto another open in-process store with `FileStorageTransfer`. Move tombstones the source only after the destination hashes match. Key mode defaults to `PreserveKeys`. `NewKey` and `ExistingKey` re-save, which FTP and SFTP need because they are not a byte copy. A remote API connection throws before any write. An optional destination prefix places the copies in that folder; blank keeps each file's current prefix. Same-server prefix move and copy stay on that connection.
- **Keys.** Saved key stores live beside the servers. Each one is local keys or AWS Secrets Manager. Connect opens an in-process server with the one selected in the toolbar. `ReadLocal` loads a key file whose `key` values are raw key bytes and whose metadata keeps the salt. `GET key-ids` fills the key picker. Rotate the selection with `files/rotate-deks`. Migrate every file on a source key with `files/migrate-deks`. A partial failure reports how many succeeded and how many failed, then reloads when anything succeeded.

## Examples

### Open a local folder

```csharp
var session = new FileManagerSession();
session.AddLocal("Photos", "/data/photos", FileManagerMetadataKind.Sqlite);
await session.ConnectAsync(cancellationToken);
await session.UploadAsync(path, new FileManagerUploadOptions { Compress = true }, cancellationToken);
```

### Connect to a remote API

```csharp
var session = new FileManagerSession();
session.AddRemote("Workbench", "http://localhost:5000", "Workbench/FileStorage");
session.SetAuth(FileManagerAuthKind.ApiKey, apiKey);
await session.ConnectAsync(cancellationToken);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.FileStorage.Models` (direct, lyo)
- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Compression` (direct, lyo)
- `Lyo.Desktop` (direct, lyo)
- `Lyo.Encryption.AesSiv` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.FileMetadataStore` (direct, lyo)
- `Lyo.FileMetadataStore.Postgres` (direct, lyo)
- `Lyo.FileMetadataStore.Sqlite` (direct, lyo)
- `Lyo.FileStorage` (direct, lyo)
- `Lyo.FileStorage.AzureBlob` (direct, lyo)
- `Lyo.FileStorage.Ftp` (direct, lyo)
- `Lyo.FileStorage.S3` (direct, lyo)
- `Lyo.FileStorage.Sftp` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `Lyo.IO.Temp` (direct, lyo)
- `Lyo.KeyStore` (direct, lyo)
- `Lyo.KeyStore.Aws` (direct, lyo)
- `Lyo.Lock` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `AWSSDK.SecretsManager` `4.0.100.3` (direct, third-party)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Ftp.Client` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.IO.FileSystem` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Postgres` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Sftp.Client` (transitive, lyo)
- `Lyo.Sqlite` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `AWSSDK.Core` `4.0.100.4` (transitive, third-party)
- `AWSSDK.S3` `4.0.101` (transitive, third-party)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Azure.Storage.Blobs` `12.29.1` (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `Dorssel.Security.Cryptography.AesExtra` `2.0.0` (transitive, third-party)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `FluentFTP` `54.2.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Sqlite` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.DataAnnotations` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `SSH.NET` `2025.1.0` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)