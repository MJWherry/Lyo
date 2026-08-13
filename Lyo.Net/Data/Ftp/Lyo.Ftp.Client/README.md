# Lyo.Ftp.Client

Thin FTP/FTPS wrapper over FluentFTP for Lyo hosts and storage adapters (`Lyo.IO.Temp.Ftp`, `Lyo.FileStorage.Ftp`). Canonical APIs are `*Async` (`Task` + `CancellationToken`) backed by FluentFTP async methods and await-safe pool/per-client gates. Sync methods remain as thin blockers. Provides connection pooling with leases, POSIX path jail via `Lyo.Common.Pathing.PathHelpers`, optional FTPS encryption, `ILogger` diagnostics, and `ftp.*` metrics. Thread-safe for concurrent callers up to `MaxPooledClients`; do not share one leased `Stream` across threads.

## Features

- **Async-first** — prefer `*Async` from hosts/adapters; sync wrappers block on the async implementation (no `Task.Run`).
- **Pooled leases** — `MaxPooledClients` concurrent FluentFTP clients; per-client `SemaphoreSlim` serializes ops across awaits.
- **Thread-safe callers** — concurrent use is supported and capped by the pool; one leased stream is not multi-thread safe.
- **Path jail** — all paths resolve under `RootRemoteDirectory` with `PathStyle.Posix`.
- **FTPS** — `FtpEncryptionMode` (`None` / `Explicit` / `Implicit`) with `FtpTlsPolicy` (`ValidateCertificate` / `AcceptAny`).
- **Observability** — `ILogger` + `IMetrics` (`ftp.connect`, `ftp.operation`, `ftp.bytes`, `ftp.pool`, `ftp.errors`); `EnableMetrics` opt-out uses `NullMetrics`.
- **DI** — `AddFtpClient` / `AddFtpClientFromConfiguration`.

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

- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Metrics` — (direct, lyo)
- `FluentFTP` `54.2.0` — (direct, third-party)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)