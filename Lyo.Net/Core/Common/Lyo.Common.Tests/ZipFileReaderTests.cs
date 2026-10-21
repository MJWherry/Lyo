using System.IO.Compression;
using Lyo.Common.Core.Archives;
using Lyo.Exceptions.Models;
using Lyo.IO.Temp.Models;

namespace Lyo.Common.Tests;

public class ZipFileReaderTests
{
    [Fact]
    public void Read_ReturnsEntryBytes()
    {
        var bytes = ZipFileBuilder.Create().AddFile("a.txt", [7, 8, 9]).Build();
        using var reader = ZipFileReader.Open(new MemoryStream(bytes));
        Assert.Equal([7, 8, 9], reader.Read("a.txt"));
    }

    [Fact]
    public void Entries_ExposeLengthCommentAndUnixMode()
    {
        var bytes = ZipFileBuilder.Create().WithComment("bundle").AddFile("a.txt", [1, 2], new() { Comment = "note", UnixMode = 0x1ED }).Build();
        using var reader = ZipFileReader.Open(new MemoryStream(bytes));
        Assert.Equal("bundle", reader.Comment);
        var entry = Assert.Single(reader.Entries);
        Assert.Equal("a.txt", entry.Name);
        Assert.Equal(2, entry.Length);
        Assert.Equal("note", entry.Comment);
        Assert.Equal(0x1ED, entry.UnixMode);
        Assert.False(entry.IsDirectory);
    }

    [Fact]
    public void ExtractTo_WritesFilesAndRestoresMode()
    {
        if (OperatingSystem.IsWindows())
            return;

        using var session = new IOTempSession();
        session.CreateDirectory("src");
        session.CreateDirectory("src/empty");
        var source = session.CreateFile("hello"u8.ToArray(), "src/a.txt");
        var mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead;
        File.SetUnixFileMode(source, mode);
        var local = new DateTime(2024, 3, 4, 5, 6, 8, DateTimeKind.Local);
        File.SetLastWriteTime(source, local);
        var zipPath = session.GetFilePath("files.zip");
        ZipFileBuilder.Create().AddDirectory(session.GetDirectoryPath("src")).BuildToFile(zipPath);
        var dest = session.CreateDirectory("out");
        using (var reader = ZipFileReader.Open(zipPath))
            reader.ExtractTo(dest);

        var extracted = Path.Combine(dest, "a.txt");
        Assert.Equal("hello"u8.ToArray(), File.ReadAllBytes(extracted));
        Assert.True(Directory.Exists(Path.Combine(dest, "empty")));
        Assert.Equal(mode, File.GetUnixFileMode(extracted));
        var delta = (new DateTimeOffset(local).UtcDateTime - File.GetLastWriteTimeUtc(extracted)).Duration();
        Assert.True(delta <= TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void ExtractTo_ParentEntryName_ThrowsAndWritesNothing()
    {
        using var session = new IOTempSession();
        var zipPath = session.GetFilePath("bad.zip");
        using (var file = File.Create(zipPath)) {
            using (var archive = new ZipArchive(file, ZipArchiveMode.Create, true)) {
                var entry = archive.CreateEntry("../evil.txt");
                using var output = entry.Open();
                output.Write([1]);
            }
        }

        var dest = session.CreateDirectory("safe");
        using var reader = ZipFileReader.Open(zipPath);
        Assert.Throws<InvalidFormatException>(() => reader.ExtractTo(dest));
        Assert.Empty(Directory.GetFileSystemEntries(dest));
        Assert.False(File.Exists(Path.Combine(session.SessionDirectory, "evil.txt")));
    }
}