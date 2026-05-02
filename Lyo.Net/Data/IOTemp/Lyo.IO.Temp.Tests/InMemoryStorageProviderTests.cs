using System.IO.Compression;
using System.Text;
using Lyo.IO.Temp.Models;
using Lyo.IO.Temp.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.IO.Temp.Tests;

/// <summary>Tests for <see cref="InMemoryIOTempStorageProvider" />, including direct provider operations, full session integration, and DI registration.</summary>
public sealed class InMemoryStorageProviderTests
{
    private static InMemoryIOTempStorageProvider NewProvider() => new();

    private static IOTempSession NewSession(InMemoryIOTempStorageProvider storage)
    {
        var options = new IOTempSessionOptions { RootDirectory = storage.RootPath };
        return new(options, storageProvider: storage);
    }

    private static IOTempService NewService(InMemoryIOTempStorageProvider storage)
    {
        var options = new IOTempServiceOptions { TempRoot = "/mem-svc", DirectoryName = Guid.NewGuid().ToString("N") };
        return new(options, storageProvider: storage);
    }

    private static string ReadAllText(InMemoryIOTempStorageProvider storage, string path)
    {
        using var stream = storage.OpenRead(path);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static byte[] ReadAllBytes(InMemoryIOTempStorageProvider storage, string path)
    {
        using var stream = storage.OpenRead(path);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    [Fact]
    public void RootPath_is_unique_per_instance()
    {
        var a = NewProvider();
        var b = NewProvider();
        Assert.NotEqual(a.RootPath, b.RootPath);
    }

    [Fact]
    public void RootPath_directory_exists_after_construction()
    {
        var storage = NewProvider();
        Assert.True(storage.DirectoryExists(storage.RootPath));
    }

    [Fact]
    public void CreateDirectory_and_DirectoryExists_round_trip()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/sub/nested";
        storage.CreateDirectory(path);
        Assert.True(storage.DirectoryExists(path));
    }

    [Fact]
    public void DirectoryExists_returns_false_for_unknown_path()
    {
        var storage = NewProvider();
        Assert.False(storage.DirectoryExists(storage.RootPath + "/nope"));
    }

    [Fact]
    public void DirectoryExists_returns_false_for_a_file()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/afile.txt";
        storage.TouchFile(path);
        Assert.False(storage.DirectoryExists(path));
    }

    [Fact]
    public void DeleteDirectory_removes_directory_and_all_contents()
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
    public void DeleteDirectory_is_noop_for_nonexistent_path()
    {
        var storage = NewProvider();
        storage.DeleteDirectory(storage.RootPath + "/ghost"); // must not throw
    }

    [Fact]
    public void EnumerateEntries_returns_only_immediate_children()
    {
        var storage = NewProvider();
        var dir = storage.RootPath + "/parent";
        storage.CreateDirectory(dir);
        storage.WriteAllText(dir + "/file1.txt", "a", Encoding.UTF8);
        storage.WriteAllText(dir + "/file2.txt", "b", Encoding.UTF8);
        storage.CreateDirectory(dir + "/subdir");
        storage.WriteAllText(dir + "/subdir/deep.txt", "c", Encoding.UTF8); // should NOT appear
        var entries = storage.EnumerateEntries(dir).ToList();
        Assert.Equal(3, entries.Count); // file1, file2, subdir
        Assert.Contains(entries, e => !e.IsDirectory && e.FullPath.EndsWith("file1.txt"));
        Assert.Contains(entries, e => !e.IsDirectory && e.FullPath.EndsWith("file2.txt"));
        Assert.Contains(entries, e => e.IsDirectory && e.FullPath.EndsWith("subdir"));
    }

    [Fact]
    public void EnumerateEntries_returns_empty_for_empty_directory()
    {
        var storage = NewProvider();
        var dir = storage.RootPath + "/empty";
        storage.CreateDirectory(dir);
        Assert.Empty(storage.EnumerateEntries(dir));
    }

    [Fact]
    public void EnsureDirectoryAccessible_does_not_throw()
    {
        var storage = NewProvider();
        storage.EnsureDirectoryAccessible(storage.RootPath); // must be a no-op
    }

    [Fact]
    public void FileExists_returns_false_for_unknown_file()
    {
        var storage = NewProvider();
        Assert.False(storage.FileExists(storage.RootPath + "/nope.txt"));
    }

    [Fact]
    public void FileExists_returns_false_for_a_directory()
    {
        var storage = NewProvider();
        Assert.False(storage.FileExists(storage.RootPath));
    }

    [Fact]
    public void TouchFile_creates_empty_file()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/empty.txt";
        storage.TouchFile(path);
        Assert.True(storage.FileExists(path));
        Assert.Equal(0, storage.GetFileLength(path));
    }

    [Fact]
    public void WriteAllBytes_stores_and_reads_back()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/data.bin";
        var data = new byte[] { 1, 2, 3, 4, 5 };
        storage.WriteAllBytes(path, data);
        Assert.True(storage.FileExists(path));
        Assert.Equal(data, ReadAllBytes(storage, path));
    }

    [Fact]
    public void WriteAllText_stores_and_reads_back()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/hello.txt";
        storage.WriteAllText(path, "Hello, World!", Encoding.UTF8);
        Assert.True(storage.FileExists(path));
        Assert.Equal("Hello, World!", ReadAllText(storage, path));
    }

    [Fact]
    public void WriteAllBytes_overwrites_existing_content()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/overwrite.bin";
        storage.WriteAllBytes(path, new byte[] { 1, 2, 3 });
        storage.WriteAllBytes(path, new byte[] { 9, 8 });
        Assert.Equal(new byte[] { 9, 8 }, ReadAllBytes(storage, path));
    }

    [Fact]
    public void AppendAllText_appends_to_existing_file()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/append.txt";
        storage.WriteAllText(path, "Hello", Encoding.UTF8);
        storage.AppendAllText(path, " World", Encoding.UTF8);
        Assert.Equal("Hello World", ReadAllText(storage, path));
    }

    [Fact]
    public void AppendAllText_creates_file_when_not_exists()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/new-append.txt";
        storage.AppendAllText(path, "created", Encoding.UTF8);
        Assert.True(storage.FileExists(path));
        Assert.Equal("created", ReadAllText(storage, path));
    }

    [Fact]
    public void DeleteFile_removes_file()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/del.txt";
        storage.WriteAllText(path, "x", Encoding.UTF8);
        storage.DeleteFile(path);
        Assert.False(storage.FileExists(path));
    }

    [Fact]
    public void MoveFile_transfers_content_and_removes_source()
    {
        var storage = NewProvider();
        var src = storage.RootPath + "/src.txt";
        var dst = storage.RootPath + "/dst.txt";
        storage.WriteAllText(src, "moved", Encoding.UTF8);
        storage.MoveFile(src, dst);
        Assert.False(storage.FileExists(src));
        Assert.True(storage.FileExists(dst));
        Assert.Equal("moved", ReadAllText(storage, dst));
    }

    [Fact]
    public void CopyFile_duplicates_content_and_keeps_source()
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
    public void GetFileLength_returns_correct_byte_count()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/sized.txt";
        var data = Encoding.UTF8.GetBytes("12345");
        storage.WriteAllBytes(path, data);
        Assert.Equal(5, storage.GetFileLength(path));
    }

    [Fact]
    public void GetFileCreationTimeUtc_is_close_to_now()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var storage = NewProvider();
        var path = storage.RootPath + "/ts.txt";
        storage.TouchFile(path);
        var created = storage.GetFileCreationTimeUtc(path);
        Assert.True(created >= before && created <= DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void OpenCreate_stream_commits_bytes_on_dispose()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/streamed.bin";
        using (var stream = storage.OpenCreate(path))
            stream.Write(new byte[] { 10, 20, 30 }, 0, 3);

        Assert.Equal(new byte[] { 10, 20, 30 }, ReadAllBytes(storage, path));
    }

    [Fact]
    public void OpenCreate_replaces_existing_content()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/replace.bin";
        storage.WriteAllBytes(path, new byte[] { 1, 2, 3, 4, 5 });
        using (var stream = storage.OpenCreate(path))
            stream.Write(new byte[] { 99 }, 0, 1);

        Assert.Equal(new byte[] { 99 }, ReadAllBytes(storage, path));
    }

    [Fact]
    public void OpenAppend_stream_appends_bytes_on_dispose()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/append.bin";
        storage.WriteAllBytes(path, new byte[] { 1, 2 });
        using (var stream = storage.OpenAppend(path))
            stream.Write(new byte[] { 3, 4 }, 0, 2);

        Assert.Equal(new byte[] { 1, 2, 3, 4 }, ReadAllBytes(storage, path));
    }

    [Fact]
    public void OpenRead_returns_correct_content()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/readable.txt";
        storage.WriteAllText(path, "readable", Encoding.UTF8);
        using var stream = storage.OpenRead(path);
        using var reader = new StreamReader(stream);
        Assert.Equal("readable", reader.ReadToEnd());
    }

    [Fact]
    public void OpenRead_throws_for_nonexistent_file()
    {
        var storage = NewProvider();
        Assert.Throws<FileNotFoundException>(() => storage.OpenRead(storage.RootPath + "/ghost.txt"));
    }

    [Fact]
    public async Task WriteAllBytesAsync_stores_data()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/async-bytes.bin";
        var data = new byte[] { 7, 8, 9 };
        await storage.WriteAllBytesAsync(path, data, TestContext.Current.CancellationToken);
        Assert.Equal(data, ReadAllBytes(storage, path));
    }

    [Fact]
    public async Task WriteAllTextAsync_stores_text()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/async-text.txt";
        await storage.WriteAllTextAsync(path, "async", Encoding.UTF8, TestContext.Current.CancellationToken);
        Assert.Equal("async", ReadAllText(storage, path));
    }

    [Fact]
    public async Task AppendAllTextAsync_appends_text()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/async-append.txt";
        storage.WriteAllText(path, "A", Encoding.UTF8);
        await storage.AppendAllTextAsync(path, "B", Encoding.UTF8, TestContext.Current.CancellationToken);
        Assert.Equal("AB", ReadAllText(storage, path));
    }

    [Fact]
    public async Task CopyStreamToFileAsync_writes_stream_content()
    {
        var storage = NewProvider();
        var path = storage.RootPath + "/from-stream.bin";
        var source = new MemoryStream(new byte[] { 1, 2, 3 });
        await storage.CopyStreamToFileAsync(source, path, TestContext.Current.CancellationToken);
        Assert.Equal(new byte[] { 1, 2, 3 }, ReadAllBytes(storage, path));
    }

    [Fact]
    public async Task CopyFileAsync_duplicates_file()
    {
        var storage = NewProvider();
        var src = storage.RootPath + "/src-async.txt";
        var dst = storage.RootPath + "/dst-async.txt";
        storage.WriteAllText(src, "hello", Encoding.UTF8);
        await storage.CopyFileAsync(src, dst, TestContext.Current.CancellationToken);
        Assert.Equal("hello", ReadAllText(storage, dst));
    }

    [Fact]
    public void Session_InMemory_CreateFile_from_text_is_tracked()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.CreateFile("hello session");
        Assert.Single(session.Files);
        Assert.True(storage.FileExists(path));
        Assert.Equal("hello session", ReadAllText(storage, path));
    }

    [Fact]
    public void Session_InMemory_CreateFile_from_bytes_is_tracked()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var data = Encoding.UTF8.GetBytes("bytes");
        var path = session.CreateFile(new ReadOnlyMemory<byte>(data));
        Assert.Single(session.Files);
        Assert.Equal("bytes", ReadAllText(storage, path));
    }

    [Fact]
    public void Session_InMemory_CreateFile_from_stream_is_tracked()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes("stream content"));
        var path = session.CreateFile(ms);
        Assert.Single(session.Files);
        Assert.Equal("stream content", ReadAllText(storage, path));
    }

    [Fact]
    public void Session_InMemory_TouchFile_creates_empty_tracked_file()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.TouchFile();
        Assert.Single(session.Files);
        Assert.True(storage.FileExists(path));
        Assert.Equal(0, storage.GetFileLength(path));
    }

    [Fact]
    public void Session_InMemory_CreateDirectory_is_tracked()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var dir = session.CreateDirectory();
        Assert.Contains(dir, session.Directories);
        Assert.True(storage.DirectoryExists(dir));
    }

    [Fact]
    public void Session_InMemory_WriteFile_overwrites_content()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.CreateFile("original");
        session.WriteFile(path, "overwritten");
        Assert.Equal("overwritten", ReadAllText(storage, path));
    }

    [Fact]
    public void Session_InMemory_WriteFile_bytes_overwrites_content()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.CreateFile(new ReadOnlyMemory<byte>(new byte[] { 1, 2 }));
        session.WriteFile(path, new ReadOnlyMemory<byte>(new byte[] { 9 }));
        Assert.Equal(new byte[] { 9 }, ReadAllBytes(storage, path));
    }

    [Fact]
    public void Session_InMemory_AppendToFile_text_appends()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.CreateFile("start");
        session.AppendToFile(path, "-end");
        Assert.Equal("start-end", ReadAllText(storage, path));
    }

    [Fact]
    public void Session_InMemory_AppendToFile_bytes_appends()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.CreateFile(new ReadOnlyMemory<byte>(new byte[] { 1, 2 }));
        session.AppendToFile(path, new ReadOnlyMemory<byte>(new byte[] { 3, 4 }));
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, ReadAllBytes(storage, path));
    }

    [Fact]
    public void Session_InMemory_DeleteFile_removes_from_tracking_and_store()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.CreateFile("to delete");
        session.DeleteFile(path);
        Assert.Empty(session.Files);
        Assert.False(storage.FileExists(path));
    }

    [Fact]
    public void Session_InMemory_DeleteDirectory_removes_from_tracking_and_store()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var dir = session.CreateDirectory();
        session.DeleteDirectory(dir);
        Assert.DoesNotContain(dir, session.Directories);
        Assert.False(storage.DirectoryExists(dir));
    }

    [Fact]
    public void Session_InMemory_CopyFrom_file_adds_to_session()
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
    public void Session_InMemory_MoveFrom_file_adds_to_session_and_removes_source()
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
    public void Session_InMemory_Clear_removes_all_files_and_dirs()
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
    public void Session_InMemory_CreateSubSession_is_rooted_under_parent()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        using var sub = session.CreateSubSession();
        Assert.StartsWith(session.SessionDirectory, sub.SessionDirectory);
    }

    [Fact]
    public void Session_InMemory_EnumerateFiles_finds_all_created_files()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        session.CreateFile("file one");
        session.CreateFile("file two");
        var enumerated = session.EnumerateFiles().ToList();
        Assert.Equal(2, enumerated.Count);
    }

    [Fact]
    public void Session_InMemory_EnumerateFiles_with_pattern_filters()
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
    public void Session_InMemory_GetTotalBytesUsed_tracks_correctly()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        session.CreateFile(new ReadOnlyMemory<byte>(new byte[100]));
        session.CreateFile(new ReadOnlyMemory<byte>(new byte[200]));
        Assert.Equal(300, session.GetTotalBytesUsed());
    }

    [Fact]
    public void Session_InMemory_Dispose_removes_session_directory_from_store()
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
    public void Session_InMemory_Generator_CreateRandomFile_is_tracked_with_correct_size()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.Generator.CreateRandomFile(512);
        Assert.Single(session.Files);
        Assert.Equal(512, storage.GetFileLength(path));
        Assert.Equal(512, session.GetTotalBytesUsed());
    }

    [Fact]
    public void Session_InMemory_Generator_CreateTextFile_has_correct_line_count()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.Generator.CreateTextFile(5, 10);
        var content = ReadAllText(storage, path);
        Assert.Equal(5, content.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    public void Session_InMemory_Generator_CreateCsvFile_has_header_and_rows()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.Generator.CreateCsvFile(3, 2);
        var lines = ReadAllText(storage, path).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length); // 1 header + 3 rows
        Assert.Equal("col_0,col_1", lines[0]);
    }

    [Fact]
    public void Session_InMemory_Generator_CreateJsonFile_produces_valid_json_object()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.Generator.CreateJsonFile(0, 2);
        var text = ReadAllText(storage, path).Trim();
        Assert.StartsWith("{", text);
        Assert.EndsWith("}", text);
    }

    [Fact]
    public void Session_InMemory_Generator_CreateXmlFile_produces_valid_xml()
    {
        var storage = NewProvider();
        using var session = NewSession(storage);
        var path = session.Generator.CreateXmlFile(1, 2);
        var content = ReadAllText(storage, path);
        Assert.StartsWith("<root>", content);
        Assert.Contains("</root>", content);
    }

    [Fact]
    public void Session_InMemory_Generator_CreateZipFile_produces_valid_archive()
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
    public void Session_InMemory_Generator_ExtractZipFile_extracts_all_entries()
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
    public void Session_InMemory_Generator_SimulateDirectory_creates_tree_in_store()
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
    public void Service_InMemory_CreateSession_and_CreateFile_work_end_to_end()
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
    public void Service_InMemory_CreateFile_oneoff_stores_data()
    {
        var storage = NewProvider();
        using var service = NewService(storage);
        var path = service.CreateFile(new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 }));
        Assert.True(storage.FileExists(path));
    }

    [Fact]
    public void Service_InMemory_Dispose_removes_service_directory()
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
    public void Service_InMemory_Cleanup_removes_files_older_than_zero()
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
    public void AddIOTempService_uses_registered_IIOTempStorageProvider()
    {
        var storage = new InMemoryIOTempStorageProvider();
        var services = new ServiceCollection();
        services.AddSingleton<IIOTempStorageProvider>(storage);
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
    public void AddIOTempService_without_registered_provider_uses_filesystem()
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