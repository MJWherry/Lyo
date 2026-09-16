# Lyo.FileManager.App

Avalonia window named `lyo-files`. Menus hold the server, file, edit, and key actions. A tab is a key store and a file store. The toolbar is the key store picker, the server picker, Connect, Up, a breadcrumb, and the frequent buttons. Connect opens that pair. The file list stays in the center and follows the selected connection. Tree, metadata, and log dock to the left, right, or bottom, resize, and hide. Drag a title bar to another edge. The log starts on the bottom. Quick search filters the loaded tree, and another box filters the open folder. The file grid sorts and resizes columns. Query uses that grid with a prefix column. Drag a file onto the list to upload it. Double-click a file to open it, or a folder to enter it. Copy and move open a dialog with a local prefix or another open connection. Servers, key stores, and the layout persist under `$XDG_CONFIG_HOME/lyo/file-manager.json` or `%AppData%\Lyo\file-manager.json`. Session logic lives in `Lyo.FileManager`.

## Features

- **Add a server.** A blank name, file folder, or base URL stays in the dialog with the error. Local asks for a file folder, then the catalog folder and filename. Those default to a SQLite file beside the folder. Postgres asks for a connection string instead. S3 asks for a bucket and a catalog. Azure Blob asks for a storage connection string, a container, and a catalog. FTP asks for host, username, password, and a catalog. SFTP also asks for a host-key fingerprint, or a private key instead of a password. The key store is chosen on the toolbar.
- **Key stores.** Key stores opens a window for the saved list. Add, rename, and remove stores. Load reads a key file: `{ "keys": [ { "keyId", "version", "current", "key", "metadata" } ] }`. `key` is the key bytes, including the salt in metadata. Each store is local keys, with a passphrase and key versions, or AWS Secrets Manager. The toolbar lists `(None)` and every saved name, in front of the server picker. Connect uses that choice. A remote server ignores it. Each open pair is a tab named for the key store and the server. Choosing one shows that pair and does not reconnect. The same server can stay open with another key store.
- **Dock.** Tree starts on the left, metadata on the right, and the log along the bottom. Each can move to another edge, resize, or hide. Reset layout restores the default. Sizes and edges are saved with the servers.
- **Tree, list, and query.** The tree loads a folder when it is expanded. Search filters loaded names. The grid supports extended selection. Query runs name and prefix filters in the same grid. The metadata pane lists the selected file.
- **Connections.** The tab strip lists open connections. Choosing a tab shows that store. Closing a tab disconnects it. The server list stays in the picker.
- **Copy and move.** Copy and move open a dialog with two tabs. Local asks for a destination prefix on this connection. Other connection copies or moves the selection onto another open in-process store, into the target folder you type. Typing suggests existing directories. Dropping the selection on another connection tab opens that move dialog with the tab selected. Copy leaves the source. Move removes it after the hashes match. Preserve keys is the default. New key and existing key are for a store that cannot copy ciphertext, such as FTP or SFTP.
- **Transfers.** Upload can compress, encrypt, and pick a key id from the connected key store. Encrypt with no key store and no key id is rejected. Dropped files upload into the current prefix.

## Examples

### Run the window

```text
dotnet run --project Lyo.Net/Apps/FileManager/Lyo.FileManager.App
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Avalonia.Components` (direct, lyo)
- `Lyo.FileManager` (direct, lyo)
- `Avalonia` `12.1.3` (direct, third-party)
- `Avalonia.Controls.DataGrid` `12.1.2` (direct, third-party)
- `Avalonia.Desktop` `12.1.3` (direct, third-party)
- `Avalonia.Fonts.Inter` `12.1.3` (direct, third-party)
- `Avalonia.Themes.Fluent` `12.1.3` (direct, third-party)
- `Lyo.Api.FileStorage.Models` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.Desktop` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Encryption.AesSiv` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.FileMetadataStore` (transitive, lyo)
- `Lyo.FileMetadataStore.Postgres` (transitive, lyo)
- `Lyo.FileMetadataStore.Sqlite` (transitive, lyo)
- `Lyo.FileStorage` (transitive, lyo)
- `Lyo.FileStorage.AzureBlob` (transitive, lyo)
- `Lyo.FileStorage.Ftp` (transitive, lyo)
- `Lyo.FileStorage.S3` (transitive, lyo)
- `Lyo.FileStorage.Sftp` (transitive, lyo)
- `Lyo.Ftp.Client` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.Http.Client` (transitive, lyo)
- `Lyo.IO.FileSystem` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.KeyStore.Aws` (transitive, lyo)
- `Lyo.Lock` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Postgres` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Sftp.Client` (transitive, lyo)
- `Lyo.Sqlite` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `AWSSDK.Core` `4.0.100.4` (transitive, third-party)
- `AWSSDK.S3` `4.0.101` (transitive, third-party)
- `AWSSDK.SecretsManager` `4.0.100.3` (transitive, third-party)
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
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
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