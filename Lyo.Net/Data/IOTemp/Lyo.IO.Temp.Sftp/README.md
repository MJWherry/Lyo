# Lyo.IO.Temp.Sftp

Implements `IIOTempStorageProvider` over `Lyo.Sftp.Client` with `PathStyle.Posix`. Register with `AddIOTempSftpStorageProvider` before `AddIOTempService`.

## Features

- **SFTP temp storage** — sessions and service dirs live under the remote root jail.
- **PathStyle.Posix** — portable path math via `Lyo.Common.Pathing`.
- **DI** — `AddIOTempSftpStorageProvider` / `FromConfiguration`.

## Examples

### Registration

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

- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.IO.Temp` — (direct, lyo)
- `Lyo.Sftp.Client` — (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `SSH.NET` `2025.1.0` — (transitive, third-party)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)