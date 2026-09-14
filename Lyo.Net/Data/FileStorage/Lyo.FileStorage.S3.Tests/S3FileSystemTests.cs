using Lyo.Common.Core.Pathing;
using Lyo.Exceptions.Models;
using Lyo.FileStorage.S3.Tests.Support;
using Lyo.IO.FileSystem;

namespace Lyo.FileStorage.S3.Tests;

public class S3FileSystemTests
{
    [Fact]
    public void RootPath_EmptyPrefix_IsPosixRoot()
    {
        var bucket = new InMemoryS3Bucket();
        using var fs = new S3FileSystem(bucket.CreateClient(), "bucket");
        Assert.Equal("/", fs.RootPath);
        Assert.Equal(PathStyle.Posix, fs.PathStyle);
        Assert.Null(fs.Watch(fs.RootPath));
        Assert.Throws<NotSupportedException>(() => fs.OpenAppend("/a.bin"));
    }

    [Fact]
    public void RootPath_KeyPrefix_IsJailed()
    {
        var bucket = new InMemoryS3Bucket();
        using var fs = new S3FileSystem(bucket.CreateClient(), "bucket", "app-data");
        Assert.Equal("/app-data", fs.RootPath);
        Assert.Throws<InvalidFormatException>(() => fs.DirectoryExists("/outside"));
    }

    [Fact]
    public void ListDirectory_Delimiter_SplitsFilesAndCommonPrefixes()
    {
        var bucket = new InMemoryS3Bucket();
        using var fs = new S3FileSystem(bucket.CreateClient(), "bucket");
        fs.CreateDirectory("/reports");
        fs.WriteAllBytes("/reports/a.bin", "x"u8.ToArray());
        fs.CreateDirectory("/reports/2024");
        fs.WriteAllBytes("/reports/2024/deep.bin", "y"u8.ToArray());
        var listed = fs.ListDirectory("/reports");
        Assert.Contains(listed, e => e is { IsDirectory: false, Name: "a.bin" });
        Assert.Contains(listed, e => e is { IsDirectory: true, Name: "2024" });
        Assert.DoesNotContain(listed, e => e.Name == "deep.bin");
    }

    [Fact]
    public void CreateDirectory_PutsMarker_AndEmptyFolderLists()
    {
        var bucket = new InMemoryS3Bucket();
        using var fs = new S3FileSystem(bucket.CreateClient(), "bucket");
        fs.CreateDirectory("/empty");
        Assert.True(fs.DirectoryExists("/empty"));
        Assert.Empty(fs.ListDirectory("/empty"));
    }

    [Fact]
    public void DeleteDirectory_Recursive_RemovesPrefix()
    {
        var bucket = new InMemoryS3Bucket();
        using var fs = new S3FileSystem(bucket.CreateClient(), "bucket");
        fs.CreateDirectory("/tree");
        fs.WriteAllBytes("/tree/child.bin", "z"u8.ToArray());
        fs.DeleteDirectory("/tree");
        Assert.False(fs.DirectoryExists("/tree"));
        Assert.False(fs.FileExists("/tree/child.bin"));
    }

    [Fact]
    public void WriteReadCopyMove_AndJail()
    {
        var bucket = new InMemoryS3Bucket();
        using var fs = new S3FileSystem(bucket.CreateClient(), "bucket", "app-data");
        fs.WriteAllBytes("/app-data/note.txt", "hello"u8.ToArray());
        Assert.True(fs.FileExists("/app-data/note.txt"));
        using (var stream = fs.OpenRead("/app-data/note.txt")) {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            Assert.Equal("hello"u8.ToArray(), ms.ToArray());
        }

        fs.CopyFile("/app-data/note.txt", "/app-data/copy.txt");
        fs.Move("/app-data/copy.txt", "/app-data/moved.txt");
        Assert.False(fs.FileExists("/app-data/copy.txt"));
        Assert.True(fs.FileExists("/app-data/moved.txt"));
        Assert.Throws<InvalidFormatException>(() => fs.FileExists("/other/note.txt"));
    }
}
