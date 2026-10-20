using System.IO.Compression;
using System.Text;
using Lyo.Common.Core.Archives;
using Lyo.Exceptions.Models;
using Lyo.IO.Temp.Models;

namespace Lyo.Common.Tests;

public class ZipFileBuilderTests
{
    [Fact]
    public void Build_BytesAndText_RoundTripsEntryBytes()
    {
        var bytes = ZipFileBuilder.Create()
            .AddFile("file1.bin", [1, 2, 3])
            .AddFile("file2.txt", "Content 2")
            .Build();

        using var archive = Open(bytes);
        Assert.Equal([1, 2, 3], Read(archive, "file1.bin"));
        Assert.Equal("Content 2"u8.ToArray(), Read(archive, "file2.txt"));
    }

    [Fact]
    public void WithCompressionLevel_NoCompression_StoresUncompressedEntries()
    {
        var payload = new byte[400];
        Array.Fill(payload, (byte)'a');
        var bytes = ZipFileBuilder.Create()
            .WithCompressionLevel(CompressionLevel.NoCompression)
            .AddFile("a.txt", payload)
            .Build();

        using var archive = Open(bytes);
        var entry = Assert.Single(archive.Entries);
        Assert.Equal(entry.Length, entry.CompressedLength);
    }

    [Fact]
    public void WithComment_RoundTripsOnArchive()
    {
        var bytes = ZipFileBuilder.Create()
            .WithComment("bundle")
            .AddFile("a.txt", [1])
            .Build();

        using var archive = Open(bytes);
        Assert.Equal("bundle", archive.Comment);
    }

    [Fact]
    public void AddFile_EntryOptions_SetsLevelTimeAndComment()
    {
        var stamp = new DateTimeOffset(2024, 3, 4, 5, 6, 8, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2024, 3, 4, 5, 6, 8)));
        var payload = new byte[400];
        Array.Fill(payload, (byte)'b');
        var bytes = ZipFileBuilder.Create()
            .AddFile("note.txt", payload, new ZipEntryOptions { CompressionLevel = CompressionLevel.NoCompression, LastWriteTime = stamp, Comment = "note" })
            .Build();

        using var archive = Open(bytes);
        var entry = Assert.Single(archive.Entries);
        Assert.Equal(entry.Length, entry.CompressedLength);
        Assert.Equal("note", entry.Comment);
        AssertZipTime(stamp, entry.LastWriteTime);
    }

    [Fact]
    public void AddFileFromPath_PreservesLastWriteTime()
    {
        using var session = new IOTempSession();
        var path = session.CreateFile(new byte[] { 9, 8, 7 }, "stamp.bin");
        var local = new DateTime(2020, 6, 15, 12, 30, 0, DateTimeKind.Local);
        File.SetLastWriteTime(path, local);

        var bytes = ZipFileBuilder.Create().AddFileFromPath(path, "stamp.bin").Build();
        using var archive = Open(bytes);
        var entry = archive.GetEntry("stamp.bin");
        Assert.NotNull(entry);
        AssertZipTime(new(local), entry.LastWriteTime);
    }

    [Fact]
    public void AddDirectory_PrefixesPaths_SkipsExcluded_IncludesEmptyDir()
    {
        using var session = new IOTempSession();
        session.CreateDirectory("tree");
        session.CreateDirectory("tree/empty");
        session.CreateFile("keep"u8.ToArray(), "tree/keep.txt");
        session.CreateFile("skip"u8.ToArray(), "tree/skip.txt");
        var root = session.GetDirectoryPath("tree");

        var bytes = ZipFileBuilder.Create()
            .AddDirectory(root, "docs", relative => relative != "skip.txt")
            .Build();

        using var archive = Open(bytes);
        var names = archive.Entries.Select(entry => entry.FullName).ToHashSet();
        Assert.Contains("docs/keep.txt", names);
        Assert.DoesNotContain("docs/skip.txt", names);
        Assert.Contains(names, name => name is "docs/empty" or "docs/empty/");
        Assert.Equal("keep"u8.ToArray(), Read(archive, "docs/keep.txt"));
    }

    [Fact]
    public void AddFile_DuplicateEntryName_Throws()
    {
        var builder = ZipFileBuilder.Create().AddFile("a.txt", [1]);
        Assert.Throws<ArgumentException>(() => builder.AddFile("a.txt", [2]));
    }

    [Theory]
    [InlineData("../secret.txt")]
    [InlineData("/abs.txt")]
    public void AddFile_ParentOrAbsoluteEntryName_Throws(string name)
    {
        var builder = ZipFileBuilder.Create();
        Assert.Throws<InvalidFormatException>(() => builder.AddFile(name, [1]));
    }

    [Fact]
    public void AddFile_AfterBuild_Throws()
    {
        var builder = ZipFileBuilder.Create().AddFile("a.txt", [1]);
        builder.Build();
        Assert.Throws<InvalidOperationException>(() => builder.AddFile("b.txt", [2]));
    }

    [Fact]
    public async Task BuildAsync_StreamEntry_RoundTripsBytes()
    {
        await using var stream = new MemoryStream([4, 5, 6]);
        var bytes = await ZipFileBuilder.Create().AddFile("s.bin", stream).BuildAsync(TestCt);
        using var archive = Open(bytes);
        Assert.Equal([4, 5, 6], Read(archive, "s.bin"));
    }

    [Fact]
    public void BuildTo_LargeStoredEntry_WritesInChunksSmallerThanArchive()
    {
        var payload = new byte[1024 * 1024];
        var sink = new MaxWriteStream();
        ZipFileBuilder.Create()
            .WithCompressionLevel(CompressionLevel.NoCompression)
            .AddFile("blob.bin", payload)
            .BuildTo(sink);

        Assert.True(sink.Length > payload.Length);
        Assert.True(sink.MaxWrite < sink.Length);
    }

    [Fact]
    public void BuildToFile_RoundTripsEntryBytes()
    {
        using var session = new IOTempSession();
        var zipPath = session.GetFilePath("out.zip");
        ZipFileBuilder.Create().AddFile("a.txt", [1, 2, 3]).BuildToFile(zipPath);

        using var archive = new ZipArchive(File.OpenRead(zipPath), ZipArchiveMode.Read);
        Assert.Equal([1, 2, 3], Read(archive, "a.txt"));
    }

    [Fact]
    public void AddFile_UnixMode_RoundTripsPermissionBits()
    {
        var bytes = ZipFileBuilder.Create()
            .AddFile("run.sh", [1], new() { UnixMode = 0x1ED })
            .Build();

        using var archive = Open(bytes);
        var entry = Assert.Single(archive.Entries);
        Assert.Equal(0x1ED, entry.ExternalAttributes >> 16);
    }

    [Fact]
    public void AddFileFromPath_PreservesUnixMode()
    {
        if (OperatingSystem.IsWindows())
            return;
        using var session = new IOTempSession();
        var path = session.CreateFile(new byte[] { 1 }, "tool.sh");
        var mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
        File.SetUnixFileMode(path, mode);

        var bytes = ZipFileBuilder.Create().AddFileFromPath(path).Build();
        using var archive = Open(bytes);
        var entry = Assert.Single(archive.Entries);
        Assert.Equal((int)mode, entry.ExternalAttributes >> 16);
    }

    [Fact]
    public void Open_RemoveAndAdd_KeepsUntouchedEntryBytes()
    {
        var original = ZipFileBuilder.Create()
            .AddFile("a.txt", [1, 2, 3])
            .AddFile("b.txt", [4])
            .Build();
        using var session = new IOTempSession();
        var path = session.CreateFile(original, "base.zip");

        var updated = ZipFileBuilder.Open(path).Remove("b.txt").AddFile("c.txt", [9]).Build();
        using var archive = Open(updated);
        Assert.Equal([1, 2, 3], Read(archive, "a.txt"));
        Assert.Null(archive.GetEntry("b.txt"));
        Assert.Equal([9], Read(archive, "c.txt"));
    }

    [Fact]
    public void Remove_UnknownEntry_Throws()
    {
        var builder = ZipFileBuilder.Create().AddFile("a.txt", [1]);
        Assert.Throws<ArgumentException>(() => builder.Remove("missing.txt"));
    }

    [Fact]
    public void AddFile_EntryComment_RoundTripsWhenArchiveIsZip64()
    {
        var builder = ZipFileBuilder.Create();
        for (var i = 0; i < 65535; i++)
            builder.AddFile($"e{i}.txt", [1]);

        builder.AddFile("noted.txt", [2], new() { Comment = "zip64" });
        var bytes = builder.Build();
        using var archive = Open(bytes);
        var entry = archive.GetEntry("noted.txt");
        Assert.NotNull(entry);
        Assert.Equal("zip64", entry.Comment);
        Assert.True(ContainsZip64(bytes));
    }

    private static ZipArchive Open(byte[] bytes) => new(new MemoryStream(bytes), ZipArchiveMode.Read);

    private static byte[] Read(ZipArchive archive, string name)
    {
        var entry = archive.GetEntry(name);
        Assert.NotNull(entry);
        using var stream = entry.Open();
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static void AssertZipTime(DateTimeOffset expected, DateTimeOffset actual)
    {
        var delta = (expected.UtcDateTime - actual.UtcDateTime).Duration();
        Assert.True(delta <= TimeSpan.FromSeconds(2), $"{expected:o} vs {actual:o}");
    }

    private static bool ContainsZip64(byte[] zip)
    {
        for (var i = 0; i < zip.Length - 3; i++) {
            if (zip[i] == 0x50 && zip[i + 1] == 0x4b && zip[i + 2] == 0x06 && zip[i + 3] == 0x06)
                return true;
        }

        return false;
    }

    private sealed class MaxWriteStream : MemoryStream
    {
        public int MaxWrite { get; private set; }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Note(count);
            base.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            Note(buffer.Length);
            base.Write(buffer);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Note(buffer.Length);
            return base.WriteAsync(buffer, cancellationToken);
        }

        private void Note(int count)
        {
            if (count > MaxWrite)
                MaxWrite = count;
        }
    }
}
