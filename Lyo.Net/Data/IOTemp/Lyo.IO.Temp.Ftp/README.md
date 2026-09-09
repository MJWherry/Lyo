# Lyo.IO.Temp.Ftp

`IIOTempStorageProvider` on `Lyo.Ftp.Client` using `PathStyle.Posix`. Call `AddIOTempFtpStorageProvider` ahead of `AddIOTempService`.

## Features

- **FTP temp storage.** The remote root jail holds session and service directories.
- **PathStyle.Posix.** `Lyo.Common.Core.Pathing` does the portable path math.
- **DI.** `AddIOTempFtpStorageProvider` or `FromConfiguration`.

## Examples

### DI setup

```csharp
services.AddIOTempFtpStorageProvider(o =>
{
    o.Host = "ftp.example.com";
    o.Username = "lyo";
    o.Password = secret;
    o.RootRemoteDirectory = "/tmp/lyo";
});
services.AddIOTempService();
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Ftp.Client` (direct, lyo)
- `Lyo.IO.Temp` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Configuration` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `FluentFTP` `54.2.0` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)