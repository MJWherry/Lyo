# Lyo.Ftp.Client

FluentFTP wrapper for FTP/FTPS used by Lyo hosts and storage adapters (`Lyo.FileStorage.Ftp`). Call `*Async` from hosts (`Task` + `CancellationToken`). Those methods hit FluentFTP's async API and the await-safe pool/per-client gates. Sync methods wait on that async path. Pooled connections are leased, POSIX paths stay under `RootRemoteDirectory` through `Lyo.Common.Core.Pathing.PathHelpers`, and FTPS can encrypt the session. `ILogger` and `ftp.*` metrics are recorded. Callers may run concurrently up to `MaxPooledClients`. Do not share one leased `Stream` across threads. `FtpFileSystem` adapts `IFtpClient` onto `Lyo.IO.FileSystem.IFileSystem` (`AddFtpFileSystem`).

## Features

- **Async.** Hosts and adapters should call `*Async`. Sync wrappers wait on the async implementation. No `Task.Run`.
- **Pooled leases.** Up to `MaxPooledClients` FluentFTP clients at once. Each client serializes work across awaits with a `SemaphoreSlim`.
- **Concurrent callers.** Parallelism stops at the pool size. A single leased stream is not thread-safe.
- **IFileSystem.** `FtpFileSystem` wraps `IFtpClient`. Register with `AddFtpFileSystem`.
- **Path jail.** Paths stay under `RootRemoteDirectory` with `PathStyle.Posix`.
- **FTPS.** `FtpEncryptionMode` (`None` / `Explicit` / `Implicit`) plus `FtpTlsPolicy` (`ValidateCertificate` / `AcceptAny`).
- **Logging and metrics.** `ILogger` and `IMetrics` (`ftp.connect`, `ftp.operation`, `ftp.bytes`, `ftp.pool`, `ftp.errors`). Turning `EnableMetrics` off uses `NullMetrics`.
- **DI.** `AddFtpClient` / `AddFtpClientFromConfiguration`.

## Examples

### DI setup

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

### Upload and download asynchronously

```csharp
await client.UploadAsync("report.bin", bytes, ct);
var copy = await client.DownloadBytesAsync("report.bin", ct);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.IO.FileSystem` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `FluentFTP` `54.2.0` (direct, third-party)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)