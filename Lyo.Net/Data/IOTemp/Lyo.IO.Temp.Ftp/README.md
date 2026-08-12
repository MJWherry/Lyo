# Lyo.IO.Temp.Ftp

Implements `IIOTempStorageProvider` over `Lyo.Ftp.Client` with `PathStyle.Posix`. Register with `AddIOTempFtpStorageProvider` before `AddIOTempService`.

## Features

- **FTP temp storage** — sessions and service dirs live under the remote root jail.
- **PathStyle.Posix** — portable path math via `Lyo.Common.Pathing`.
- **DI** — `AddIOTempFtpStorageProvider` / `FromConfiguration`.

## Examples

### Registration

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

- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Ftp.Client` — (direct, lyo)
- `Lyo.IO.Temp` — (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `FluentFTP` `54.2.0` — (transitive, third-party)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)