# Lyo.Sftp.Client

SFTP wrapper over SSH.NET for Lyo hosts and storage adapters (`Lyo.IO.Temp.Sftp`, `Lyo.FileStorage.Sftp`). Hosts should call `*Async` (`Task` + `CancellationToken`). Those methods use SSH.NET's async API and await-safe pool/per-client gates. Sync methods block on the async path. The client leases pooled connections, jails POSIX paths under `RootRemoteDirectory` via `Lyo.Common.Pathing.PathHelpers`, checks host-key allow lists, and accepts password or private-key auth. It logs through `ILogger` and records `sftp.*` metrics. Concurrent callers are fine up to `MaxPooledClients`. Do not share one leased `Stream` across threads.

## Features

- **Async.** Prefer `*Async` from hosts and adapters. Sync wrappers block on the async implementation. No `Task.Run`.
- **Pooled leases.** `MaxPooledClients` concurrent SSH.NET clients. A per-client `SemaphoreSlim` serializes ops across awaits.
- **Concurrent callers.** The pool caps parallelism. One leased stream is not safe across threads.
- **Path jail.** Paths resolve under `RootRemoteDirectory` with `PathStyle.Posix`.
- **Auth.** Password and/or PEM or key-file private key. Host-key fingerprint allow list, or `AcceptAny` for tests.
- **Logging and metrics.** `ILogger` plus `IMetrics` (`sftp.connect`, `sftp.operation`, `sftp.bytes`, `sftp.pool`, `sftp.errors`). `EnableMetrics` opt-out uses `NullMetrics`.
- **DI.** `AddSftpClient` / `AddSftpClientFromConfiguration`.

## Examples

### Registration

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
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `SSH.NET` `2025.1.0` (direct, third-party)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)