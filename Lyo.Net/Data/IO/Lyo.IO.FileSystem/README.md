# Lyo.IO.FileSystem

Path-rooted virtual file system. Callers use `IFileSystem`. `LocalFileSystem` and `MemoryFileSystem` ship in this package. SFTP, FTP, S3, Azure Blob, and FileSystemWatcher implement the same contract in their existing packages. FileStorage uses `IFileSystem` only for physical keys (`IFileStoragePhysical`), not as a catalog. Paths stay under `RootPath` through `Lyo.Common.Core.Pathing.PathHelpers`.

## Features

- **Contract.** `IFileSystem` lists, reads, writes, copies, and moves files. Async is canonical; sync methods park (same pattern as Lyo SFTP/FTP clients).
- **Capabilities.** `FileSystemCapabilities` flags (`Read`, `Write`, `CreateDirectory`, `Delete`, `Move`, `Copy`, `Watch`). `Watch` returns null when watching is not supported.
- **Local disk.** `LocalFileSystem` (`PathStyle.Host`) jailed to a root directory.
- **Memory.** `MemoryFileSystem` (`PathStyle.Posix`) on a `ConcurrentDictionary`. Fits WASM and tests.
- **Helpers.** `FileSystemExtensions` for `TouchFile`, `WriteAllBytes` / `WriteAllText` / `AppendAllText`, `CopyStreamToFileAsync`, and `EnsureDirectoryAccessible`. `IFileTreeSource` is a list-only seam for tree UIs (including HTTP).
- **DI.** `AddLocalFileSystem` / `AddMemoryFileSystem` / `AddLocalFileSystemFromConfiguration`.

## Examples

### Local disk

```csharp
services.AddLocalFileSystem(o => o.RootPath = "/var/lyo-data");

var fs = new LocalFileSystem("/tmp/lyo");
fs.CreateDirectory(Path.Combine(fs.RootPath, "reports"));
fs.WriteAllBytes(Path.Combine(fs.RootPath, "a.bin"), payload);
```

### In-memory

```csharp
services.AddMemoryFileSystem();

var fs = new MemoryFileSystem();
fs.WriteAllText(fs.RootPath + "/note.txt", "hello", Encoding.UTF8);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)