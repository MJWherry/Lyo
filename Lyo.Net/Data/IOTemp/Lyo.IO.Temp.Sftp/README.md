# Lyo.IO.Temp.Sftp

`IIOTempStorageProvider` on `Lyo.Sftp.Client` using `PathStyle.Posix`. Call `AddIOTempSftpStorageProvider` ahead of `AddIOTempService`.

## Features

- **SFTP temp storage.** The remote root jail holds session and service directories.
- **PathStyle.Posix.** `Lyo.Common.Core.Pathing` does the portable path math.
- **DI.** `AddIOTempSftpStorageProvider` or `FromConfiguration`.

## Examples

### DI setup

```csharp
services.AddIOTempSftpStorageProvider(o =>
{
    o.Host = "sftp.example.com";
    o.Username = "lyo";
    o.Password = secret;
    o.RootRemoteDirectory = "/tmp/lyo";
    o.HostKeyPolicy = SftpHostKeyPolicy.FingerprintAllowList;
    o.AllowedHostKeyFingerprints.Add("SHA256:...");
});
services.AddIOTempService();
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.IO.Temp` (direct, lyo)
- `Lyo.Sftp.Client` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Configuration` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `SSH.NET` `2025.1.0` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)