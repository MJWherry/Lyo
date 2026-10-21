# Lyo.IO.FileSystem

Path-rooted virtual file system. Callers use `IFileSystem`. `LocalFileSystem` and `MemoryFileSystem` ship in this package. SFTP, FTP, S3, Azure Blob, and FileSystemWatcher
implement the same contract in their existing packages. FileStorage uses `IFileSystem` only for physical keys (`IFileStoragePhysical`), not as a catalog. Paths stay under
`RootPath` through `Lyo.Common.Core.Pathing.PathHelpers`.

## Features

- **Contract.** `IFileSystem` lists, reads, writes, copies, and moves files, and resolves a relative path against `RootPath`. Async is canonical; sync methods park (same pattern as
  Lyo SFTP/FTP clients). Missing paths throw `FileNotFoundException`, a refused overwrite throws `IOException`, and an unsupported capability throws `NotSupportedException` on
  every backend.
- **Metadata in one call.** `GetEntry` / `GetEntryAsync` return a `FileSystemEntry?` (kind, length, last write) instead of chaining `FileExists`, `GetLength`, and
  `GetLastWriteTimeUtc`. `EnumerateDirectoryAsync` streams a directory, optionally recursively, without materialising the listing.
- **Capabilities.** `FileSystemCapabilities` flags (`Read`, `Write`, `CreateDirectory`, `Delete`, `Move`, `Copy`, `Watch`, `Append`, `MoveDirectory`). Object stores clear `Append`
  and `MoveDirectory`; `Watch` returns null when watching is not supported.
- **Local disk.** `LocalFileSystem` (`PathStyle.Host`) jailed to a root directory, with one stat per metadata call, a single-pass directory listing, and async-capable streams from
  the async openers. `CreateRootIfMissing` controls whether the root is created on construction.
- **Memory.** `MemoryFileSystem` (`PathStyle.Posix`) on a flat path map plus a child index, guarded by a `ReaderWriterLockSlim`. Point lookups stay O (1), listing is O (children)
  rather than O (store), and append, create, move, and subtree delete are atomic. Fits WASM and tests.
- **Helpers.** `FileSystemExtensions` for `ReadAllBytes` / `ReadAllText`, `TouchFile`, `WriteAllBytes` / `WriteAllText` / `AppendAllText` (including `ReadOnlyMemory<byte>`
  overloads), `CopyStreamToFileAsync`, and `EnsureDirectoryAccessible`. `IFileTreeSource` is a list-only seam for tree UIs (including HTTP).
- **Shared contract suite.** `Lyo.IO.FileSystem.Tests` exposes `IFileSystemContractTests`, which every in-repo backend derives from so the sync and async halves of the interface
  are held to the same behaviour.
- **DI.** `AddLocalFileSystem` / `AddMemoryFileSystem` / `AddLocalFileSystemFromConfiguration` / `AddFileTreeSource`.

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
fs.WriteAllText("/note.txt", "hello");

if (await fs.GetEntryAsync("/note.txt", ct) is { IsDirectory: false } entry)
    Console.WriteLine($"{entry.Length} bytes, written {entry.LastWriteTimeUtc:O}");
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (direct, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)