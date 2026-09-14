using Lyo.Common.Core.Pathing;
using Lyo.IO.FileSystem;

namespace Lyo.Ftp.Client.Tests;

public class FtpFileSystemTests
{
    [Fact]
    public void PathStyle_IsPosix_AndWatchIsNull()
    {
        using var client = new FtpClient(
            new() {
                Host = "localhost",
                Username = "u",
                Password = "p",
                RootRemoteDirectory = "/upload",
                EncryptionMode = FtpEncryptionMode.None
            });

        using var fs = new FtpFileSystem(client);
        Assert.Equal(PathStyle.Posix, fs.PathStyle);
        Assert.Equal("/upload", fs.RootPath);
        Assert.Null(fs.Watch(fs.RootPath));
        Assert.False(fs.Capabilities.HasFlag(FileSystemCapabilities.Watch));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ListWriteReadDelete_RoundTrip()
    {
        var host = await FtpTestHost.TryStartAsync(TestContext.Current.CancellationToken);
        if (host is null) {
            Assert.Skip("Docker/Testcontainers unavailable");
            return;
        }

        await using var container = host.Value.Container;
        using var client = new FtpClient(host.Value.Options);
        await client.HealthPingAsync(TestContext.Current.CancellationToken);
        using var fs = new FtpFileSystem(client);
        var dir = PathHelpers.Combine(PathStyle.Posix, fs.RootPath, "vfs-dir");
        var file = PathHelpers.Combine(PathStyle.Posix, dir, "note.txt");
        await fs.CreateDirectoryAsync(dir, TestContext.Current.CancellationToken);
        fs.WriteAllBytes(file, [.. "ftp-vfs"u8]);
        Assert.True(await fs.FileExistsAsync(file, TestContext.Current.CancellationToken));
        var listed = await fs.ListDirectoryAsync(dir, TestContext.Current.CancellationToken);
        Assert.Contains(listed, e => e is { IsDirectory: false, Name: "note.txt" });
        await using (var stream = await fs.OpenReadAsync(file, TestContext.Current.CancellationToken)) {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, TestContext.Current.CancellationToken);
            Assert.Equal("ftp-vfs"u8.ToArray(), ms.ToArray());
        }

        var copied = PathHelpers.Combine(PathStyle.Posix, dir, "copy.txt");
        await fs.CopyFileAsync(file, copied, TestContext.Current.CancellationToken);
        await fs.MoveAsync(copied, PathHelpers.Combine(PathStyle.Posix, dir, "moved.txt"), TestContext.Current.CancellationToken);
        await fs.DeleteFileAsync(file, TestContext.Current.CancellationToken);
        Assert.False(await fs.FileExistsAsync(file, TestContext.Current.CancellationToken));
        await fs.DeleteDirectoryAsync(dir, ct: TestContext.Current.CancellationToken);
        Assert.False(await fs.DirectoryExistsAsync(dir, TestContext.Current.CancellationToken));
    }
}
