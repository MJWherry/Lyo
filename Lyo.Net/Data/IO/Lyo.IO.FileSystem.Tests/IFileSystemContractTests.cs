using System.Text;
using Lyo.Common.Core.Pathing;
using Lyo.Exceptions.Models;

namespace Lyo.IO.FileSystem.Tests;

/// <summary>Shared <see cref="IFileSystem" /> contract tests. Provider suites supply <see cref="Create" />.</summary>
public abstract class IFileSystemContractTests
{
    /// <summary>Builds a fresh file system for one test.</summary>
    protected abstract IFileSystem Create();

    [Fact]
    public void CreateDirectory_AndDirectoryExists_RoundTrip()
    {
        using var fs = Create();
        var path = Combine(fs, "sub", "nested");
        fs.CreateDirectory(path);
        Assert.True(fs.DirectoryExists(path));
    }

    [Fact]
    public void DirectoryExists_UnknownPath_ReturnsFalse()
    {
        using var fs = Create();
        Assert.False(fs.DirectoryExists(Combine(fs, "nope")));
    }

    [Fact]
    public void WriteAndRead_RoundTrip()
    {
        using var fs = Create();
        var path = Combine(fs, "a.bin");
        var payload = "hello-vfs"u8.ToArray();
        fs.WriteAllBytes(path, payload);
        Assert.True(fs.FileExists(path));
        Assert.Equal(payload.Length, fs.GetLength(path));
        using var stream = fs.OpenRead(path);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        Assert.Equal(payload, ms.ToArray());
    }

    [Fact]
    public void ListDirectory_ReturnsImmediateChildrenOnly()
    {
        using var fs = Create();
        fs.CreateDirectory(Combine(fs, "dir"));
        fs.WriteAllBytes(Combine(fs, "dir", "file.txt"), "x"u8.ToArray());
        fs.CreateDirectory(Combine(fs, "dir", "nested"));
        fs.WriteAllBytes(Combine(fs, "dir", "nested", "deep.txt"), "y"u8.ToArray());
        var listed = fs.ListDirectory(Combine(fs, "dir"));
        Assert.Contains(listed, e => e is { IsDirectory: false, Name: "file.txt" });
        Assert.Contains(listed, e => e is { IsDirectory: true, Name: "nested" });
        Assert.DoesNotContain(listed, e => e.Name == "deep.txt");
    }

    [Fact]
    public void DeleteFile_RemovesFile()
    {
        using var fs = Create();
        var path = Combine(fs, "gone.txt");
        fs.WriteAllBytes(path, "z"u8.ToArray());
        fs.DeleteFile(path);
        Assert.False(fs.FileExists(path));
    }

    [Fact]
    public void DeleteDirectory_Recursive_RemovesTree()
    {
        using var fs = Create();
        var dir = Combine(fs, "tree");
        fs.CreateDirectory(dir);
        fs.WriteAllBytes(Combine(fs, "tree", "child.bin"), "x"u8.ToArray());
        fs.DeleteDirectory(dir);
        Assert.False(fs.DirectoryExists(dir));
        Assert.False(fs.FileExists(Combine(fs, "tree", "child.bin")));
    }

    [Fact]
    public void CopyFile_AndMove_Work()
    {
        using var fs = Create();
        var src = Combine(fs, "src.txt");
        var copied = Combine(fs, "copy.txt");
        var moved = Combine(fs, "moved.txt");
        fs.WriteAllText(path: src, "payload", Encoding.UTF8);
        fs.CopyFile(src, copied);
        Assert.True(fs.FileExists(src));
        Assert.True(fs.FileExists(copied));
        fs.Move(copied, moved);
        Assert.False(fs.FileExists(copied));
        Assert.True(fs.FileExists(moved));
    }

    [Fact]
    public void Jail_PathEscapesRoot_Throws()
    {
        using var fs = Create();
        var outside = fs.PathStyle == PathStyle.Host
            ? Path.Combine(Path.GetTempPath(), "lyo-vfs-outside-" + Guid.NewGuid().ToString("N"))
            : "/outside-" + Guid.NewGuid().ToString("N");
        Assert.Throws<InvalidFormatException>(() => fs.DirectoryExists(outside));
    }

    [Fact]
    public void OpenAppend_RoundTrip_OrNotSupported()
    {
        using var fs = Create();
        var path = Combine(fs, "append.bin");
        try {
            fs.WriteAllBytes(path, "ab"u8.ToArray());
            using (var append = fs.OpenAppend(path))
                append.Write("cd"u8.ToArray());

            using var read = fs.OpenRead(path);
            using var ms = new MemoryStream();
            read.CopyTo(ms);
            Assert.Equal("abcd"u8.ToArray(), ms.ToArray());
        }
        catch (NotSupportedException) {
            Assert.Throws<NotSupportedException>(() => fs.OpenAppend(path));
        }
    }

    [Fact]
    public void DeleteDirectory_NonRecursive_EmptySucceeds_NonEmptyThrows_MissingIsNoOp()
    {
        using var fs = Create();
        var empty = Combine(fs, "empty");
        fs.CreateDirectory(empty);
        fs.DeleteDirectory(empty, recursive: false);
        Assert.False(fs.DirectoryExists(empty));

        var filled = Combine(fs, "filled");
        fs.CreateDirectory(filled);
        fs.WriteAllBytes(Combine(fs, "filled", "child.bin"), "x"u8.ToArray());
        Assert.Throws<IOException>(() => fs.DeleteDirectory(filled, recursive: false));
        Assert.True(fs.FileExists(Combine(fs, "filled", "child.bin")));

        fs.DeleteDirectory(Combine(fs, "missing"), recursive: false);
    }

    [Fact]
    public void DeleteDirectory_OnFilePath_IsNoOp()
    {
        using var fs = Create();
        var path = Combine(fs, "keep.bin");
        fs.WriteAllBytes(path, "z"u8.ToArray());
        fs.DeleteDirectory(path);
        Assert.True(fs.FileExists(path));
    }

    [Fact]
    public void GetLength_AndTimestamps_MissingPath_Throw()
    {
        using var fs = Create();
        var missing = Combine(fs, "nope.bin");
        Assert.ThrowsAny<Exception>(() => fs.GetLength(missing));
        Assert.ThrowsAny<Exception>(() => fs.GetCreationTimeUtc(missing));
        Assert.ThrowsAny<Exception>(() => fs.GetLastWriteTimeUtc(missing));
    }

    [Fact]
    public void Watch_WithoutCapability_ReturnsNull()
    {
        using var fs = Create();
        if ((fs.Capabilities & FileSystemCapabilities.Watch) != 0)
            return;

        Assert.Null(fs.Watch(fs.RootPath));
    }

    protected static string Combine(IFileSystem fs, params string[] parts)
    {
        var segments = new string[parts.Length + 1];
        segments[0] = fs.RootPath;
        Array.Copy(parts, 0, segments, 1, parts.Length);
        return PathHelpers.Combine(fs.PathStyle, segments);
    }
}
