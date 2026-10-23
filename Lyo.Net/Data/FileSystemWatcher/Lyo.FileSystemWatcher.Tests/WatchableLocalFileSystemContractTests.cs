using Lyo.Common.Core.Identifiers;
using Lyo.IO.FileSystem;
using Lyo.IO.FileSystem.Tests;

namespace Lyo.FileSystemWatcher.Tests;

public sealed class WatchableLocalFileSystemContractTests : IFileSystemContractTests
{
    protected override ValueTask<IFileSystem?> CreateAsync()
        => new(new WatchableLocalFileSystem(Path.Combine(Path.GetTempPath(), "lyo-vfs-watchable-" + LyoGuid.CreateV7().ToString("N"))));
}