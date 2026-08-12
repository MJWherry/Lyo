# Lyo.Sftp.Client

Thin SFTP wrapper over SSH.NET for Lyo hosts and storage adapters (`Lyo.IO.Temp.Sftp`, `Lyo.FileStorage.Sftp`). Canonical APIs are `*Async` (`Task` + `CancellationToken`) backed by
SSH.NET async methods and await-safe pool/per-client gates. Sync methods remain as thin blockers. Provides connection pooling with leases, POSIX path jail via
`Lyo.Common.Pathing.PathHelpers`, host-key allow lists, optional password/private-key auth, `ILogger` diagnostics, and `sftp.*` metrics. Thread-safe for concurrent callers up to
`MaxPooledClients`; do not share one leased `Stream` across threads.

## Features

- **Async-first** — prefer `*Async` from hosts/adapters; sync wrappers block on the async implementation (no `Task.Run`).
- **Pooled leases** — `MaxPooledClients` concurrent SSH.NET clients; per-client `SemaphoreSlim` serializes ops across awaits.
- **Thread-safe callers** — concurrent use is supported and capped by the pool; one leased stream is not multi-thread safe.
- **Path jail** — all paths resolve under `RootRemoteDirectory` with `PathStyle.Posix`.
- **Auth** — password and/or PEM / key-file private key; host-key fingerprint allow list (or `AcceptAny` for tests).
- **Observability** — `ILogger` + `IMetrics` (`sftp.connect`, `sftp.operation`, `sftp.bytes`, `sftp.pool`, `sftp.errors`); `EnableMetrics` opt-out uses `NullMetrics`.
- **DI** — `AddSftpClient` / `AddSftpClientFromConfiguration`.

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

- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Metrics` — (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `SSH.NET` `2025.1.0` — (direct, third-party)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)