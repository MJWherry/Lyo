using Lyo.IO.FileSystem;
using Lyo.IO.Temp.Models;
using Lyo.Testing;

namespace Lyo.FileSystemWatcher.Tests;

public class WatchableLocalFileSystemTests
{
    [Fact]
    public async Task Watch_RaisesCreated_WhenFileAppears()
    {
        await using var session = IOTempSession.CreateForTests(nameof(WatchableLocalFileSystemTests));
        using var fs = new WatchableLocalFileSystem(session.SessionDirectory);
        Assert.Equal(FileSystemCapabilities.Watch, fs.Capabilities & FileSystemCapabilities.Watch);
        FileSystemChange? seen = null;
        using var watch = fs.Watch(fs.RootPath);
        Assert.NotNull(watch);
        watch!.Changed += (_, e) => {
            if (e is { Kind: FileSystemChangeKind.Changed, IsDirectory: true })
                return;

            if (e is { Kind: FileSystemChangeKind.Created, IsDirectory: false })
                seen = e;
        };
        var path = Path.Combine(fs.RootPath, "created.txt");
        await File.WriteAllTextAsync(path, "x", TestContext.Current.CancellationToken);
        await PollAssert.ThatAsync(() => seen is { Kind: FileSystemChangeKind.Created, IsDirectory: false }, TimeSpan.FromSeconds(3));
        var listed = await fs.ListDirectoryAsync(fs.RootPath, TestContext.Current.CancellationToken);
        Assert.Contains(listed, e => e.Name == "created.txt");
    }
}
