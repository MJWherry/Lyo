using System.Text;
using Lyo.Common.Core.Pathing;

namespace Lyo.IO.FileSystem.Tests;

public sealed class FileSystemExtensionsTests
{
    [Fact]
    public async Task ReadAllTextAsync_RoundTripsWriteAllTextAsync()
    {
        using var fs = new MemoryFileSystem();
        var path = Combine(fs, "note.txt");
        await fs.WriteAllTextAsync(path, "round-trip", Encoding.UTF8, TestCt);
        Assert.Equal("round-trip", await fs.ReadAllTextAsync(path, Encoding.UTF8, TestCt));
    }

    [Fact]
    public void ReadAllText_RoundTripsWriteAllText()
    {
        using var fs = new MemoryFileSystem();
        var path = Combine(fs, "sync-note.txt");
        fs.WriteAllText(path, "sync-round-trip", Encoding.UTF8);
        Assert.Equal("sync-round-trip", fs.ReadAllText(path, Encoding.UTF8));
    }

    [Fact]
    public async Task ReadAllBytesAsync_RoundTripsWriteAllBytesAsync()
    {
        using var fs = new MemoryFileSystem();
        var path = Combine(fs, "payload.bin");
        var payload = "bytes-round-trip"u8.ToArray();
        await fs.WriteAllBytesAsync(path, payload, TestCt);
        Assert.Equal(payload, await fs.ReadAllBytesAsync(path, TestCt));
    }

    [Fact]
    public async Task ReadAllBytesAsync_MissingFile_ThrowsFileNotFound()
    {
        using var fs = new MemoryFileSystem();
        await Assert.ThrowsAsync<FileNotFoundException>(() => fs.ReadAllBytesAsync(Combine(fs, "absent.bin"), TestCt));
    }

    [Fact]
    public async Task WriteAllBytesAsync_WithoutExplicitToken_Writes()
    {
        using var fs = new MemoryFileSystem();
        var path = Combine(fs, "default-token.bin");
        await fs.WriteAllBytesAsync(path, "defaulted"u8.ToArray());
        Assert.Equal("defaulted"u8.ToArray(), await fs.ReadAllBytesAsync(path, TestCt));
    }

    [Fact]
    public void WriteAllBytes_ReadOnlyMemoryOverload_Writes()
    {
        using var fs = new MemoryFileSystem();
        var path = Combine(fs, "memory-overload.bin");
        ReadOnlyMemory<byte> payload = "memory"u8.ToArray();
        fs.WriteAllBytes(path, payload);
        Assert.Equal("memory"u8.ToArray(), fs.ReadAllBytes(path));
    }

    private static string Combine(IFileSystem fs, string name) => PathHelpers.Combine(fs.PathStyle, fs.RootPath, name);
}