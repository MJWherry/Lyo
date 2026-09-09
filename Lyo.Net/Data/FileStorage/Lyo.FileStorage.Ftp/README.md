# Lyo.FileStorage.Ftp

Lyo.FileStorage physical-IO backend for FTP/FTPS. Save, read, delete, copy, move, and DEK header rotation work. v1 does not implement presigned URLs or multipart (the base throws NotSupportedException).

## Features

- **Shared remote storage core.** `FTPFileStorageService` subclasses `RemoteFileStorageServiceBase` in `Lyo.FileStorage`, which already covers streaming, encryption-header handling, copy/move, and health. What this package adds is `FtpRemoteFileTransport` (`IRemoteFileTransport` over `Lyo.Ftp.Client`), plus options and DI.
- **FTP physical IO.** Commit-on-close upload and a leased read stream.
- **Path jail.** POSIX paths rooted at `Ftp.RootRemoteDirectory`.
- **DI.** `AddFtpFileStorageService` and `AddFtpFileStorageServiceFromConfiguration`, matching the Blob extensions.

## Examples

### DI setup

```csharp
services.AddFtpFileStorageService(o =>
{
    o.Ftp.Host = "ftp.example.com";
    o.Ftp.Username = "lyo";
    o.Ftp.Password = secret;
    o.Ftp.RootRemoteDirectory = "/files";
});
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Compression` (direct, lyo)
- `Lyo.Configuration` (direct, lyo)
- `Lyo.Encryption` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.FileMetadataStore` (direct, lyo)
- `Lyo.FileStorage` (direct, lyo)
- `Lyo.Ftp.Client` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `FluentFTP` `54.2.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.DataAnnotations` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)