using System.IO.Compression;
using System.Text;
using Lyo.IO.Temp.Models;
using Lyo.IO.FileSystem;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.IO.Temp.Tests;

/// <summary>Tests for <see cref="MemoryFileSystem" />, including direct provider operations, full session integration, and DI registration.</summary>
public sealed class InMemoryStorageProviderTests
{
    private static MemoryFileSystem NewProvider() => new();

    private static IOTempSession NewSession(MemoryFileSystem storage)
    {
        var options = new IOTempSessionOptions { RootDirectory = storage.RootPath };
        return new(options, storageProvider: storage);
    }

    private static IOTempService NewService(MemoryFileSystem storage)
    {
        var options = new IOTempServiceOptions { TempRoot = "/mem-svc", DirectoryName = Guid.NewGuid().ToString("N") };
        return new(options, storageProvider: storage);
    }

    private static string ReadAllText(MemoryFileSystem storage, string path)
    {
        using var stream = storage.OpenRead(path);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static byte[] ReadAllBytes(MemoryFileSystem storage, string path)
    {
        using var stream = storage.OpenRead(path);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    [Fact]
    public void RootPath_Is_UniquePerInstance()
    {
        var a = NewProvider();
        var b = NewProvider();
        Assert.NotEqual(a.RootPath, b.RootPath);
    }

    [Fact]
    public void RootPath_Directory_ExistsAfterConstruction()
    {
        var storage = NewProvider();
        Assert.True(storage.DirectoryExists(storage.RootPath));
    }

    [Fact]
    public void CreateDirectory_AndDirectoryExistsRound_Trip()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/sub/nested";
        storage.CreateDirectory(path);
        Assert.True(storage.DirectoryExists(path));
    }

    [Fact]
    public void DirectoryExists_Returns_FalseForUnknownPath()
    {
        var storage = NewProvider();
        Assert.False(storage.DirectoryExists(storage.RootPath + "/nope"));
    }

    [Fact]
    public void DirectoryExists_Returns_FalseForAFile()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/afile.txt";
        storage.TouchFile(path);
        Assert.False(storage.DirectoryExists(path));
    }

    [Fact]
    public void DeleteDirectory_Removes_DirectoryAndAllContents()
    {
        var storage = NewProvider();
        var dir = storage.RootPath + "/to-delete";
        var file = dir + "/child.txt";
        storage.CreateDirectory(dir);
        storage.WriteAllText(file, "data", Encoding.UTF8);
        storage.DeleteDirectory(dir);
        Assert.False(storage.DirectoryExists(dir));
        Assert.False(storage.FileExists(file));
    }

    [Fact]
    public void DeleteDirectory_Is_NoopForNonexistentPath()
    {
        var storage = NewProvider();
        storage.DeleteDirectory(storage.RootPath + "/ghost"); // must not throw
    }

    [Fact]
    public void ListDirectory_Returns_OnlyImmediateChildren()
    {
        var storage = NewProvider();
        var dir = storage.RootPath + "/parent";
        storage.CreateDirectory(dir);
        storage.WriteAllText(dir + "/file1.txt", "a", Encoding.UTF8);
        storage.WriteAllText(dir + "/file2.txt", "b", Encoding.UTF8);
        storage.CreateDirectory(dir + "/subdir");
        storage.WriteAllText(dir + "/subdir/deep.txt", "c", Encoding.UTF8); // should NOT appear
        var entries = storage.ListDirectory(dir).ToList();
        Assert.Equal(3, entries.Count); // file1, file2, subdir
        Assert.Contains(entries, e => !e.IsDirectory && e.Path.EndsWith("file1.txt"));
        Assert.Contains(entries, e => !e.IsDirectory && e.Path.EndsWith("file2.txt"));
        Assert.Contains(entries, e => e.IsDirectory && e.Path.EndsWith("subdir"));
    }

    [Fact]
    public void ListDirectory_Returns_EmptyForEmptyDirectory()
    {
        var storage = NewProvider();
        var dir = storage.RootPath + "/empty";
        storage.CreateDirectory(dir);
        Assert.Empty(storage.ListDirectory(dir));
    }

    [Fact]
    public void EnsureDirectoryAccessible_DoesNot_Throw()
    {
        var storage = NewProvider();
        storage.EnsureDirectoryAccessible(storage.RootPath); // must be a no-op
    }

    [Fact]
    public void FileExists_Returns_FalseForUnknownFile()
    {
        var storage = NewProvider();
        Assert.False(storage.FileExists(storage.RootPath + "/nope.txt"));
    }

    [Fact]
    public void FileExists_Returns_FalseForADirectory()
    {
        var storage = NewProvider();
        Assert.False(storage.FileExists(storage.RootPath));
    }

    [Fact]
    public void TouchFile_Creates_EmptyFile()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/empty.txt";
        storage.TouchFile(path);
        Assert.True(storage.FileExists(path));
        Assert.Equal(0, storage.GetLength(path));
    }

    [Fact]
    public void WriteAllBytes_StoresAnd_ReadsBack()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/data.bin";
        var data = new byte[] { 1, 2, 3, 4, 5 };
        storage.WriteAllBytes(path, data);
        Assert.True(storage.FileExists(path));
        Assert.Equal(data, ReadAllBytes(storage, path));
    }

    [Fact]
    public void WriteAllText_StoresAnd_ReadsBack()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/hello.txt";
        storage.WriteAllText(path, "Hello, World!", Encoding.UTF8);
        Assert.True(storage.FileExists(path));
        Assert.Equal("Hello, World!", ReadAllText(storage, path));
    }

    [Fact]
    public void WriteAllBytes_Overwrites_ExistingContent()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/overwrite.bin";
        storage.WriteAllBytes(path, new byte[] { 1, 2, 3 });
        storage.WriteAllBytes(path, new byte[] { 9, 8 });
        Assert.Equal(new byte[] { 9, 8 }, ReadAllBytes(storage, path));
    }

    [Fact]
    public void AppendAllText_Appends_ToExistingFile()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/append.txt";
        storage.WriteAllText(path, "Hello", Encoding.UTF8);
        storage.AppendAllText(path, " World", Encoding.UTF8);
        Assert.Equal("Hello World", ReadAllText(storage, path));
    }

    [Fact]
    public void AppendAllText_CreatesFileWhenNot_Exists()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/new-append.txt";
        storage.AppendAllText(path, "created", Encoding.UTF8);
        Assert.True(storage.FileExists(path));
        Assert.Equal("created", ReadAllText(storage, path));
    }

    [Fact]
    public void DeleteFile_Removes_File()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/del.txt";
        storage.WriteAllText(path, "x", Encoding.UTF8);
        storage.DeleteFile(path);
        Assert.False(storage.FileExists(path));
    }

    [Fact]
    public void MoveFile_TransfersContentAnd_RemovesSource()
    {
        var storage = NewProvider();
        var src = storage.RootPath + "/src.txt";
        var dst = storage.RootPath + "/dst.txt";
        storage.WriteAllText(src, "moved", Encoding.UTF8);
        storage.Move(src, dst);
        Assert.False(storage.FileExists(src));
        Assert.True(storage.FileExists(dst));
        Assert.Equal("moved", ReadAllText(storage, dst));
    }

    [Fact]
    public void CopyFile_Duplicates_ContentAndKeepsSource()
    {
        var storage = NewProvider();
        var src = storage.RootPath + "/src.txt";
        var dst = storage.RootPath + "/dst.txt";
        storage.WriteAllText(src, "copied", Encoding.UTF8);
        storage.CopyFile(src, dst);
        Assert.True(storage.FileExists(src));
        Assert.True(storage.FileExists(dst));
        Assert.Equal("copied", ReadAllText(storage, dst));
    }

    [Fact]
    public void GetLength_Returns_CorrectByteCount()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/sized.txt";
        var data = Encoding.UTF8.GetBytes("12345");
        storage.WriteAllBytes(path, data);
        Assert.Equal(5, storage.GetLength(path));
    }

    [Fact]
    public void GetCreationTimeUtc_Is_CloseToNow()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var storage = NewProvider();
        var path = storage.RootPath + "/ts.txt";
        storage.TouchFile(path);
        var created = storage.GetCreationTimeUtc(path);
        Assert.True(created >= before && created <= DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void OpenCreate_Stream_CommitsBytesOnDispose()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/streamed.bin";
        using (var stream = storage.OpenCreate(path))
            stream.Write(new byte[] { 10, 20, 30 }, 0, 3);

        Assert.Equal(new byte[] { 10, 20, 30 }, ReadAllBytes(storage, path));
    }

    [Fact]
    public void OpenCreate_Replaces_ExistingContent()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/replace.bin";
        storage.WriteAllBytes(path, new byte[] { 1, 2, 3, 4, 5 });
        using (var stream = storage.OpenCreate(path))
            stream.Write(new byte[] { 99 }, 0, 1);

        Assert.Equal(new byte[] { 99 }, ReadAllBytes(storage, path));
    }

    [Fact]
    public void OpenAppend_Stream_AppendsBytesOnDispose()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/append.bin";
        storage.WriteAllBytes(path, new byte[] { 1, 2 });
        using (var stream = storage.OpenAppend(path))
            stream.Write(new byte[] { 3, 4 }, 0, 2);

        Assert.Equal(new byte[] { 1, 2, 3, 4 }, ReadAllBytes(storage, path));
    }

    [Fact]
    public void OpenRead_Returns_CorrectContent()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/readable.txt";
        storage.WriteAllText(path, "readable", Encoding.UTF8);
        using var stream = storage.OpenRead(path);
        using var reader = new StreamReader(stream);
        Assert.Equal("readable", reader.ReadToEnd());
    }

    [Fact]
    public void OpenRead_Throws_ForNonexistentFile()
    {
        var storage = NewProvider();
        Assert.Throws<FileNotFoundException>(() => storage.OpenRead(storage.RootPath + "/ghost.txt"));
    }

    [Fact]
    public async Task WriteAllBytesAsync_Stores_Data()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/async-bytes.bin";
        var data = new byte[] { 7, 8, 9 };
        await storage.WriteAllBytesAsync(path, data, TestContext.Current.CancellationToken);
        Assert.Equal(data, ReadAllBytes(storage, path));
    }

    [Fact]
    public async Task WriteAllTextAsync_Stores_Text()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/async-text.txt";
        await storage.WriteAllTextAsync(path, "async", Encoding.UTF8, TestContext.Current.CancellationToken);
        Assert.Equal("async", ReadAllText(storage, path));
    }

    [Fact]
    public async Task AppendAllTextAsync_Appends_Text()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/async-append.txt";
        storage.WriteAllText(path, "A", Encoding.UTF8);
        await storage.AppendAllTextAsync(path, "B", Encoding.UTF8, TestContext.Current.CancellationToken);
        Assert.Equal("AB", ReadAllText(storage, path));
    }

    [Fact]
    public async Task CopyStreamToFileAsync_Writes_StreamContent()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/from-stream.bin";
        var source = new MemoryStream(new byte[] { 1, 2, 3 });
        await storage.CopyStreamToFileAsync(source, path, TestContext.Current.CancellationToken);
        Assert.Equal(new byte[] { 1, 2, 3 }, ReadAllBytes(storage, path));
    }

    [Fact]
    public async Task CopyFileAsync_Duplicates_File()
    {
        var storage = NewProvider();
        var src = storage.RootPath + "/src-async.txt";
        var dst = storage.RootPath + "/dst-async.txt";
        storage.WriteAllText(src, "hello", Encoding.UTF8);
        await storage.CopyFileAsync(src, dst, TestContext.Current.CancellationToken);
        Assert.Equal("hello", ReadAllText(storage, dst));
    }

    [Fact]
    public void CreateFile_FromText_IsTracked()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.CreateFile("hello session");
        Assert.Single(session.Files);
        Assert.True(storage.FileExists(path));
        Assert.Equal("hello session", ReadAllText(storage, path));
    }

    [Fact]
    public void CreateFile_FromBytes_IsTracked()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var data = Encoding.UTF8.GetBytes("bytes");
        var path = session.CreateFile(new ReadOnlyMemory<byte>(data));
        Assert.Single(session.Files);
        Assert.Equal("bytes", ReadAllText(storage, path));
    }

    [Fact]
    public void CreateFile_FromStream_IsTracked()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes("stream content"));
        var path = session.CreateFile(ms);
        Assert.Single(session.Files);
        Assert.Equal("stream content", ReadAllText(storage, path));
    }

    [Fact]
    public void TouchFile_Creates_EmptyTrackedFile()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.TouchFile();
        Assert.Single(session.Files);
        Assert.True(storage.FileExists(path));
        Assert.Equal(0, storage.GetLength(path));
    }

    [Fact]
    public void CreateDirectory_Is_Tracked()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var dir = session.CreateDirectory();
        Assert.Contains(dir, session.Directories);
        Assert.True(storage.DirectoryExists(dir));
    }

    [Fact]
    public void WriteFile_Overwrites_Content()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.CreateFile("original");
        session.WriteFile(path, "overwritten");
        Assert.Equal("overwritten", ReadAllText(storage, path));
    }

    [Fact]
    public void WriteFile_Bytes_OverwritesContent()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.CreateFile(new ReadOnlyMemory<byte>(new byte[] { 1, 2 }));
        session.WriteFile(path, new ReadOnlyMemory<byte>(new byte[] { 9 }));
        Assert.Equal(new byte[] { 9 }, ReadAllBytes(storage, path));
    }

    [Fact]
    public void AppendToFile_Text_Appends()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.CreateFile("start");
        session.AppendToFile(path, "-end");
        Assert.Equal("start-end", ReadAllText(storage, path));
    }

    [Fact]
    public void AppendToFile_Bytes_Appends()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.CreateFile(new ReadOnlyMemory<byte>(new byte[] { 1, 2 }));
        session.AppendToFile(path, new ReadOnlyMemory<byte>(new byte[] { 3, 4 }));
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, ReadAllBytes(storage, path));
    }

    [Fact]
    public void DeleteFile_Removes_FromTrackingAndStore()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.CreateFile("to delete");
        session.DeleteFile(path);
        Assert.Empty(session.Files);
        Assert.False(storage.FileExists(path));
    }

    [Fact]
    public void DeleteDirectory_Removes_FromTrackingAndStore()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var dir = session.CreateDirectory();
        session.DeleteDirectory(dir);
        Assert.DoesNotContain(dir, session.Directories);
        Assert.False(storage.DirectoryExists(dir));
    }

    [Fact]
    public void CopyFrom_File_AddsToSession()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        // pre-seed an external file in the same provider
        var external = storage.RootPath + "/external.txt";
        storage.WriteAllText(external, "external content", Encoding.UTF8);
        var dest = session.CopyFrom(external);
        Assert.Contains(dest, session.Files);
        Assert.Equal("external content", ReadAllText(storage, dest));
        Assert.True(storage.FileExists(external)); // original untouched
    }

    [Fact]
    public void MoveFrom_FileAddsToSessionAnd_RemovesSource()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var external = storage.RootPath + "/tomove.txt";
        storage.WriteAllText(external, "move me", Encoding.UTF8);
        var dest = session.MoveFrom(external);
        Assert.Contains(dest, session.Files);
        Assert.Equal("move me", ReadAllText(storage, dest));
        Assert.False(storage.FileExists(external));
    }

    [Fact]
    public void Clear_Removes_AllFilesAndDirs()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        session.CreateFile("a");
        session.CreateFile("b");
        session.CreateDirectory();
        session.Clear();
        Assert.Empty(session.Files);
        Assert.Empty(session.Directories);
        Assert.Equal(0, session.GetTotalBytesUsed());
    }

    [Fact]
    public void CreateSubSession_Is_RootedUnderParent()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        using var sub = session.CreateSubSession();
        Assert.StartsWith(session.SessionDirectory, sub.SessionDirectory);
    }

    [Fact]
    public void EnumerateFiles_Finds_AllCreatedFiles()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        session.CreateFile("file one");
        session.CreateFile("file two");
        var enumerated = session.EnumerateFiles().ToList();
        Assert.Equal(2, enumerated.Count);
    }

    [Fact]
    public void EnumerateFiles_WithPattern_Filters()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        session.CreateFile(new ReadOnlyMemory<byte>(new byte[1]), "a.csv");
        session.CreateFile(new ReadOnlyMemory<byte>(new byte[1]), "b.tmp");
        var csvFiles = session.EnumerateFiles("*.csv").ToList();
        Assert.Single(csvFiles);
        Assert.EndsWith(".csv", csvFiles[0]);
    }

    [Fact]
    public void GetTotalBytesUsed_Tracks_Correctly()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        session.CreateFile(new ReadOnlyMemory<byte>(new byte[100]));
        session.CreateFile(new ReadOnlyMemory<byte>(new byte[200]));
        Assert.Equal(300, session.GetTotalBytesUsed());
    }

    [Fact]
    public void Dispose_Removes_SessionDirectoryFromStore()
    {
        var storage = NewProvider();
        string sessionDir;
        using (var session = NewSession(storage)) {
            sessionDir = session.SessionDirectory;
            session.CreateFile("content");
        }

        Assert.False(storage.DirectoryExists(sessionDir));
    }

    [Fact]
    public void Generator_CreateRandomFile_IsTrackedWithCorrectSize()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.Generator.CreateRandomFile(512);
        Assert.Single(session.Files);
        Assert.Equal(512, storage.GetLength(path));
        Assert.Equal(512, session.GetTotalBytesUsed());
    }

    [Fact]
    public void Generator_CreateTextFile_HasCorrectLineCount()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.Generator.CreateTextFile(5, 10);
        var content = ReadAllText(storage, path);
        Assert.Equal(5, content.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    public void Generator_CreateCsvFile_HasHeaderAndRows()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.Generator.CreateCsvFile(3, 2);
        var lines = ReadAllText(storage, path).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length); // 1 header + 3 rows
        Assert.Equal("col_0,col_1", lines[0]);
    }

    [Fact]
    public void Generator_CreateJsonFile_ProducesValidJsonObject()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.Generator.CreateJsonFile(0, 2);
        var text = ReadAllText(storage, path).Trim();
        Assert.StartsWith("{", text);
        Assert.EndsWith("}", text);
    }

    [Fact]
    public void Generator_CreateXmlFile_ProducesValidXml()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.Generator.CreateXmlFile(1, 2);
        var content = ReadAllText(storage, path);
        Assert.StartsWith("<root>", content);
        Assert.Contains("</root>", content);
    }

    [Fact]
    public void Generator_CreateZipFile_ProducesValidArchive()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var zipPath = session.Generator.CreateZipFile(TempDirectorySpec.Flat(3, 128));
        Assert.True(storage.FileExists(zipPath));
        using var zipStream = storage.OpenRead(zipPath);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        Assert.Equal(3, archive.Entries.Count);
    }

    [Fact]
    public void Generator_ExtractZipFile_ExtractsAllEntries()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var zipPath = session.Generator.CreateZipFile(TempDirectorySpec.Flat(2, 64));
        var extractDir = session.Generator.ExtractZipFile(zipPath);
        Assert.True(storage.DirectoryExists(extractDir));
        // All extracted files are tracked
        Assert.True(session.Files.Count(f => f.StartsWith(extractDir)) >= 2);
    }

    [Fact]
    public void Generator_SimulateDirectory_CreatesTreeInStore()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var spec = new TempDirectorySpec { FileCount = 2, FileSizeBytes = 32, Subdirectories = [TempDirectorySpec.Flat(1, 16)] };
        var dir = session.Generator.SimulateDirectory(spec);
        Assert.True(storage.DirectoryExists(dir));
        // 2 root files + 1 sub file = 3 tracked files
        Assert.Equal(3, session.Files.Count);
    }

    [Fact]
    public void CreateSession_AndCreateFile_WorkEndToEnd()
    {
        var storage = NewProvider();
        using var service = NewService(storage);
        using var session = service.CreateSession();
        var path = session.CreateFile("service content");
        Assert.Equal(1, service.ActiveSessionCount);
        Assert.True(storage.FileExists(path));
        Assert.Equal("service content", ReadAllText(storage, path));
    }

    [Fact]
    public void CreateFile_Oneoff_StoresData()
    {
        var storage = NewProvider();
        using var service = NewService(storage);
        var path = service.CreateFile(new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 }));
        Assert.True(storage.FileExists(path));
    }

    [Fact]
    public void Dispose_Removes_ServiceDirectory()
    {
        var storage = NewProvider();
        string serviceDir;
        using (var service = NewService(storage)) {
            serviceDir = service.ServiceDirectory;
            service.CreateSession().Dispose();
        }

        Assert.False(storage.DirectoryExists(serviceDir));
    }

    [Fact]
    public void Cleanup_Removes_FilesOlderThanZero()
    {
        var storage = NewProvider();
        using var service = NewService(storage);
        // Create a session and dispose it — it's not active anymore but files may linger in service dir
        var path = service.CreateFile(new ReadOnlyMemory<byte>(new byte[10]));
        Assert.True(storage.FileExists(path));
        service.Cleanup(); // age=0 → all non-active files eligible
        Assert.False(storage.FileExists(path));
    }

    [Fact]
    public void AddIOTempService_Uses_RegisteredIFileSystem()
    {
        var storage = new MemoryFileSystem();
        var services = new ServiceCollection();
        services.AddSingleton<IFileSystem>(storage);
        services.AddIOTempService(o => {
            o.TempRoot = "/di-test";
            o.DirectoryName = "lyo-di-temp";
        });

        using var sp = services.BuildServiceProvider();
        var svc = sp.GetRequiredService<IIOTempService>();
        using var session = svc.CreateSession();
        var path = session.CreateFile("DI test");

        // File should be in-memory, not on real disk
        Assert.False(File.Exists(path));
        Assert.True(storage.FileExists(path));
    }

    [Fact]
    public void AddIOTempService_WithoutRegisteredProvider_UsesFilesystem()
    {
        var services = new ServiceCollection();
        services.AddIOTempService(o => {
            o.TempRoot = Path.Combine(Path.GetTempPath(), "lyo-di-fs-test");
            o.DirectoryName = Guid.NewGuid().ToString("N");
        });

        using var sp = services.BuildServiceProvider();
        var svc = sp.GetRequiredService<IIOTempService>();
        try {
            using var session = svc.CreateSession();
            var path = session.CreateFile("fs content");
            // On real filesystem
            Assert.True(File.Exists(path));
        }
        finally {
            svc.Dispose();
        }
    }
}