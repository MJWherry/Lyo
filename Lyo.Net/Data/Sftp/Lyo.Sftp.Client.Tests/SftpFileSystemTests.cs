using Lyo.Common.Core.Pathing;
using Lyo.IO.FileSystem;

namespace Lyo.Sftp.Client.Tests;

public class SftpFileSystemTests
{
    [Fact]
    public void PathStyle_IsPosix_AndWatchIsNull()
    {
        using var client = new SftpClient(
            new() {
                Host = "localhost",
                Username = "u",
                Password = "p",
                RootRemoteDirectory = "/upload",
                HostKeyPolicy = SftpHostKeyPolicy.AcceptAny
            });

        using var fs = new SftpFileSystem(client);
        Assert.Equal(PathStyle.Posix, fs.PathStyle);
        Assert.Equal("/upload", fs.RootPath);
        Assert.Null(fs.Watch(fs.RootPath));
        Assert.False(fs.Capabilities.HasFlag(FileSystemCapabilities.Watch));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ListWriteReadDelete_RoundTrip()
    {
        var host = await SftpTestHost.TryStartAsync(TestCt);
        if (host is null) {
            Assert.Skip("Docker/Testcontainers unavailable");
            return;
        }

        await using var container = host.Value.Container;
        using var client = new SftpClient(host.Value.Options);
        await client.HealthPingAsync(TestCt);
        using var fs = new SftpFileSystem(client);
        var dir = PathHelpers.Combine(PathStyle.Posix, fs.RootPath, "vfs-dir");
        var file = PathHelpers.Combine(PathStyle.Posix, dir, "note.txt");
        await fs.CreateDirectoryAsync(dir, TestCt);
        fs.WriteAllBytes(file, [.. "sftp-vfs"u8]);
        Assert.True(await fs.FileExistsAsync(file, TestCt));
        var listed = await fs.ListDirectoryAsync(dir, TestCt);
        Assert.Contains(listed, e => e is { IsDirectory: false, Name: "note.txt" });
        await using (var stream = await fs.OpenReadAsync(file, TestCt)) {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, TestCt);
            Assert.Equal("sftp-vfs"u8.ToArray(), ms.ToArray());
        }

        var copied = PathHelpers.Combine(PathStyle.Posix, dir, "copy.txt");
        await fs.CopyFileAsync(file, copied, ct: TestCt);
        await fs.MoveAsync(copied, PathHelpers.Combine(PathStyle.Posix, dir, "moved.txt"), ct: TestCt);
        await fs.DeleteFileAsync(file, TestCt);
        Assert.False(await fs.FileExistsAsync(file, TestCt));
        await fs.DeleteDirectoryAsync(dir, ct: TestCt);
        Assert.False(await fs.DirectoryExistsAsync(dir, TestCt));
    }
}