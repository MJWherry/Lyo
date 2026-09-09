# Lyo.Sftp.Client

SSH.NET wrapper for SFTP used by Lyo hosts and storage adapters (`Lyo.IO.Temp.Sftp`, `Lyo.FileStorage.Sftp`). Call `*Async` from hosts (`Task` + `CancellationToken`). Those methods hit SSH.NET's async API and the await-safe pool/per-client gates. Sync methods wait on that async path. Pooled connections are leased, POSIX paths stay under `RootRemoteDirectory` through `Lyo.Common.Core.Pathing.PathHelpers`, host-key allow lists are checked, and auth is password or private key. `ILogger` and `sftp.*` metrics are recorded. Callers may run concurrently up to `MaxPooledClients`. Do not share one leased `Stream` across threads.

## Features

- **Async.** Hosts and adapters should call `*Async`. Sync wrappers wait on the async implementation. No `Task.Run`.
- **Pooled leases.** Up to `MaxPooledClients` SSH.NET clients at once. Each client serializes work across awaits with a `SemaphoreSlim`.
- **Concurrent callers.** Parallelism stops at the pool size. A single leased stream is not thread-safe.
- **Path jail.** Paths stay under `RootRemoteDirectory` with `PathStyle.Posix`.
- **Auth.** Password and/or a PEM or key-file private key. Host-key fingerprint allow list, or `AcceptAny` in tests.
- **Logging and metrics.** `ILogger` and `IMetrics` (`sftp.connect`, `sftp.operation`, `sftp.bytes`, `sftp.pool`, `sftp.errors`). Turning `EnableMetrics` off uses `NullMetrics`.
- **DI.** `AddSftpClient` / `AddSftpClientFromConfiguration`.

## Examples

### DI setup

```csharp
services.AddSftpClient(o =>
{
    o.Host = "sftp.example.com";
    o.Username = "lyo";
    o.Password = secret;
    o.RootRemoteDirectory = "/data/lyo";
    o.HostKeyPolicy = SftpHostKeyPolicy.FingerprintAllowList;
    o.AllowedHostKeyFingerprints.Add("SHA256:...");
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
- `Lyo.Configuration` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `SSH.NET` `2025.1.0` (direct, third-party)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)