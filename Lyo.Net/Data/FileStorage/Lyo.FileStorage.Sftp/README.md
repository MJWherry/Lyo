# Lyo.FileStorage.Sftp

Lyo.FileStorage physical-IO backend for SFTP. Save, read, delete, copy, move, and DEK header rotation work. v1 does not implement presigned URLs or multipart (the base throws NotSupportedException).

## Features

- **Shared remote storage core.** `SFTPFileStorageService` subclasses `RemoteFileStorageServiceBase` in `Lyo.FileStorage`, which already covers streaming, encryption-header handling, copy/move, and health. What this package adds is `SftpRemoteFileTransport` (`IRemoteFileTransport` over `Lyo.Sftp.Client`), plus options and DI.
- **SFTP physical IO.** Commit-on-close upload and a leased read stream.
- **Path jail.** POSIX paths rooted at `Sftp.RootRemoteDirectory`.
- **DI.** `AddSftpFileStorageService` and `AddSftpFileStorageServiceFromConfiguration`, matching the Blob extensions.

## Examples

### DI setup

```csharp
services.AddSftpFileStorageService(o =>
{
    o.Sftp.Host = "sftp.example.com";
    o.Sftp.Username = "lyo";
    o.Sftp.Password = secret;
    o.Sftp.RootRemoteDirectory = "/files";
    o.Sftp.HostKeyPolicy = SftpHostKeyPolicy.FingerprintAllowList;
    o.Sftp.AllowedHostKeyFingerprints.Add("SHA256:...");
});
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Compression` (direct, lyo)
- `Lyo.Encryption` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.FileMetadataStore` (direct, lyo)
- `Lyo.FileStorage` (direct, lyo)
- `Lyo.Sftp.Client` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.IO.FileSystem` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.DataAnnotations` `10.0.5` (transitive, microsoft)
- `SSH.NET` `2025.1.0` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)