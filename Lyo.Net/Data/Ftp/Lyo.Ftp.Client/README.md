# Lyo.Ftp.Client

FTP/FTPS wrapper over FluentFTP for Lyo hosts and storage adapters (`Lyo.IO.Temp.Ftp`, `Lyo.FileStorage.Ftp`). Hosts should call `*Async` (`Task` + `CancellationToken`). Those methods use FluentFTP's async API and await-safe pool/per-client gates. Sync methods block on the async path. The client leases pooled connections, jails POSIX paths under `RootRemoteDirectory` via `Lyo.Common.Pathing.PathHelpers`, and can encrypt with FTPS. It logs through `ILogger` and records `ftp.*` metrics. Concurrent callers are fine up to `MaxPooledClients`. Do not share one leased `Stream` across threads.

## Features

- **Async.** Prefer `*Async` from hosts and adapters. Sync wrappers block on the async implementation. No `Task.Run`.
- **Pooled leases.** `MaxPooledClients` concurrent FluentFTP clients. A per-client `SemaphoreSlim` serializes ops across awaits.
- **Concurrent callers.** The pool caps parallelism. One leased stream is not safe across threads.
- **Path jail.** Paths resolve under `RootRemoteDirectory` with `PathStyle.Posix`.
- **FTPS.** `FtpEncryptionMode` (`None` / `Explicit` / `Implicit`) with `FtpTlsPolicy` (`ValidateCertificate` / `AcceptAny`).
- **Logging and metrics.** `ILogger` plus `IMetrics` (`ftp.connect`, `ftp.operation`, `ftp.bytes`, `ftp.pool`, `ftp.errors`). `EnableMetrics` opt-out uses `NullMetrics`.
- **DI.** `AddFtpClient` / `AddFtpClientFromConfiguration`.

## Examples

### Registration

```csharp
services.AddFtpClient(o =>
{
    o.Host = "ftp.example.com";
    o.Username = "lyo";
    o.Password = secret;
    o.RootRemoteDirectory = "/data/lyo";
    o.Port = PortInfo.Ftp;
    o.EncryptionMode = FtpEncryptionMode.None;
});
```

### Async upload / download

```csharp
await client.UploadAsync("report.bin", bytes, ct);
var copy = await client.DownloadBytesAsync("report.bin", ct);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `FluentFTP` `54.2.0` (direct, third-party)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)