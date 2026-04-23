using System.Text;
using Lyo.IO.Temp;
using Lyo.IO.Temp.Models;

namespace Lyo.IO.Temp.Tests;

public sealed class IOTempSessionTests : IDisposable
{
    private static IOTempServiceOptions TestOptions() => new() {
        TempRoot = Path.Combine(Path.GetTempPath(), "lyo-io-session-tests"),
        DirectoryName = Guid.NewGuid().ToString("N")
    };

    private readonly IOTempService _service = new(TestOptions());

    public void Dispose() => _service.Dispose();

    [Fact]
    public void Dispose_cleans_up_directory()
    {
        string sessionDir;
        using (var session = _service.CreateSession()) {
            sessionDir = session.SessionDirectory;
            session.TouchFile("test.txt");
            Assert.True(Directory.Exists(sessionDir));
        }
        Assert.False(Directory.Exists(sessionDir));
    }

    [Fact]
    public void Disposed_session_throws_on_TouchFile()
    {
        var session = _service.CreateSession();
        session.Dispose();
        Assert.Throws<ObjectDisposedException>(() => session.TouchFile());
    }

    [Fact]
    public void GetFilePath_returns_path_without_creating_file()
    {
        using var session = _service.CreateSession();
        var path = session.GetFilePath("planned.txt");
        Assert.False(File.Exists(path));
        Assert.EndsWith("planned.txt", path);
    }

    [Fact]
    public void TouchFile_creates_empty_file()
    {
        using var session = _service.CreateSession();
        var path = session.TouchFile("empty.tmp");
        Assert.True(File.Exists(path));
        Assert.Equal(0, new FileInfo(path).Length);
        Assert.Single(session.Files);
        Assert.Contains(path, session.Files);
    }

    [Fact]
    public void CreateFile_text_writes_content()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("Hello from session");
        Assert.True(File.Exists(path));
        Assert.Equal("Hello from session", File.ReadAllText(path));
    }

    [Fact]
    public void CreateFile_bytes_writes_content()
    {
        using var session = _service.CreateSession();
        var data = "Binary data"u8.ToArray();
        var path = session.CreateFile(new ReadOnlyMemory<byte>(data), "data.bin");
        Assert.True(File.Exists(path));
        Assert.Equal("Binary data", Encoding.UTF8.GetString(File.ReadAllBytes(path)));
    }

    [Fact]
    public void CreateDirectory_creates_directory()
    {
        using var session = _service.CreateSession();
        var path = session.CreateDirectory("assets");
        Assert.True(Directory.Exists(path));
        Assert.Single(session.Directories);
        Assert.Contains(path, session.Directories);
    }

    [Fact]
    public async Task CreateFileAsync_writes_content()
    {
        await using var session = _service.CreateSession();
        var path = await session.CreateFileAsync("Async content", TestContext.Current.CancellationToken);
        Assert.True(File.Exists(path));
        Assert.Equal("Async content", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void GetTotalBytesUsed_accumulates_across_files()
    {
        using var session = _service.CreateSession();
        session.Generator.CreateRandomFile(512);
        session.Generator.CreateRandomFile(256);
        Assert.Equal(768, session.GetTotalBytesUsed());
    }

    [Fact]
    public void GetTotalBytesUsed_includes_generator_and_session_CreateFile()
    {
        using var session = _service.CreateSession();
        session.Generator.CreateRandomFile(100);
        session.CreateFile(new ReadOnlyMemory<byte>(new byte[50]));
        Assert.Equal(150, session.GetTotalBytesUsed());
    }

    [Fact]
    public void GetSnapshot_captures_current_state()
    {
        using var session = _service.CreateSession();
        session.Generator.CreateRandomFile(128);
        session.CreateDirectory("sub");
        var snap = session.GetSnapshot();
        Assert.Equal(session.SessionDirectory, snap.SessionDirectory);
        Assert.Single(snap.Files);
        Assert.Single(snap.Directories);
        Assert.Equal(128, snap.TotalBytesUsed);
        Assert.True(snap.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void GetSnapshot_is_a_copy_not_a_live_view()
    {
        using var session = _service.CreateSession();
        var snap = session.GetSnapshot();
        session.Generator.CreateRandomFile(64);
        Assert.Empty(snap.Files);
    }

    [Fact]
    public void CreateSubSession_creates_directory_under_parent()
    {
        using var session = _service.CreateSession();
        using var sub = session.CreateSubSession();
        Assert.True(Directory.Exists(sub.SessionDirectory));
        Assert.StartsWith(session.SessionDirectory, sub.SessionDirectory);
    }

    [Fact]
    public void CreateSubSession_is_tracked_in_parent_Directories()
    {
        using var session = _service.CreateSession();
        using var sub = session.CreateSubSession();
        Assert.Contains(sub.SessionDirectory, session.Directories);
    }

    [Fact]
    public void CreateSubSession_has_independent_file_tracking()
    {
        using var session = _service.CreateSession();
        using var sub = session.CreateSubSession();
        sub.Generator.CreateRandomFile(100);
        Assert.Empty(session.Files);
        Assert.Single(sub.Files);
    }

    [Fact]
    public void EnumerateFiles_finds_all_files_including_untracked()
    {
        using var session = _service.CreateSession();
        session.Generator.CreateRandomFile(64);
        File.WriteAllText(Path.Combine(session.SessionDirectory, "untracked.txt"), "hello");
        var all = session.EnumerateFiles().ToList();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void EnumerateFiles_with_pattern_filters_results()
    {
        using var session = _service.CreateSession();
        session.Generator.CreateRandomFile(32, "a.tmp");
        File.WriteAllText(Path.Combine(session.SessionDirectory, "b.log"), "log");
        var tmpFiles = session.EnumerateFiles("*.tmp").ToList();
        Assert.Single(tmpFiles);
        Assert.EndsWith(".tmp", tmpFiles[0]);
    }

    [Fact]
    public void EnumerateDirectories_finds_all_subdirectories()
    {
        using var session = _service.CreateSession();
        session.Generator.SimulateDirectory(2, 32);
        session.CreateDirectory("manual-dir");
        var dirs = session.EnumerateDirectories().ToList();
        Assert.Equal(2, dirs.Count);
    }

    [Fact]
    public void Clear_removes_all_tracked_files()
    {
        using var session = _service.CreateSession();
        session.Generator.CreateRandomFile(512);
        session.Generator.CreateRandomFile(512);
        Assert.Equal(2, session.Files.Count);

        session.Clear();

        Assert.Empty(session.Files);
        Assert.Empty(session.Directories);
    }

    [Fact]
    public void Clear_resets_byte_count()
    {
        using var session = _service.CreateSession();
        session.Generator.CreateRandomFile(1024);
        Assert.True(session.GetTotalBytesUsed() > 0);

        session.Clear();

        Assert.Equal(0, session.GetTotalBytesUsed());
    }

    [Fact]
    public void Clear_deletes_files_from_disk()
    {
        using var session = _service.CreateSession();
        var path = session.Generator.CreateRandomFile(256);
        Assert.True(File.Exists(path));

        session.Clear();

        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Clear_session_remains_usable_afterwards()
    {
        using var session = _service.CreateSession();
        session.Generator.CreateRandomFile(256);

        session.Clear();

        var path = session.TouchFile();
        Assert.True(File.Exists(path));
        Assert.Single(session.Files);
    }

    [Fact]
    public void CopyFrom_file_copies_into_session()
    {
        using var session = _service.CreateSession();
        var src = Path.GetTempFileName();
        try {
            File.WriteAllText(src, "hello copy");
            var dest = session.CopyFrom(src);
            Assert.True(File.Exists(dest));
            Assert.Equal("hello copy", File.ReadAllText(dest));
            Assert.Contains(dest, session.Files);
        }
        finally { File.Delete(src); }
    }

    [Fact]
    public void CopyFrom_file_updates_byte_count()
    {
        using var session = _service.CreateSession();
        var src = Path.GetTempFileName();
        try {
            File.WriteAllBytes(src, new byte[2048]);
            session.CopyFrom(src);
            Assert.Equal(2048, session.GetTotalBytesUsed());
        }
        finally { File.Delete(src); }
    }

    [Fact]
    public void CopyFrom_directory_copies_all_files_into_session()
    {
        using var session = _service.CreateSession();
        var srcDir = Path.Combine(Path.GetTempPath(), "lyo-copy-src-" + Guid.NewGuid().ToString("N"));
        try {
            Directory.CreateDirectory(srcDir);
            File.WriteAllText(Path.Combine(srcDir, "a.txt"), "aaa");
            File.WriteAllText(Path.Combine(srcDir, "b.txt"), "bbb");
            var dest = session.CopyFrom(srcDir);
            Assert.True(Directory.Exists(dest));
            Assert.Contains(dest, session.Directories);
            Assert.Equal(2, session.Files.Count);
        }
        finally { Directory.Delete(srcDir, true); }
    }

    [Fact]
    public void CopyFrom_throws_when_source_does_not_exist()
    {
        using var session = _service.CreateSession();
        Assert.Throws<FileNotFoundException>(() => session.CopyFrom("/nonexistent/path/file.txt"));
    }

    [Fact]
    public void AppendToFile_bytes_appends_data()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("hello");
        session.AppendToFile(path, Encoding.UTF8.GetBytes(" world").AsMemory());
        Assert.Equal("hello world", File.ReadAllText(path));
    }

    [Fact]
    public void AppendToFile_text_appends_text()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("line1\n");
        session.AppendToFile(path, "line2\n");
        Assert.Equal("line1\nline2\n", File.ReadAllText(path));
    }

    [Fact]
    public void AppendToFile_updates_byte_count()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("abc");
        var initialBytes = session.GetTotalBytesUsed();
        session.AppendToFile(path, "xyz");
        Assert.True(session.GetTotalBytesUsed() > initialBytes);
    }

    [Fact]
    public void AppendToFile_throws_when_file_not_found()
    {
        using var session = _service.CreateSession();
        var fakePath = Path.Combine(session.SessionDirectory, "nonexistent.tmp");
        Assert.Throws<FileNotFoundException>(() => session.AppendToFile(fakePath, "data"));
    }

    [Fact]
    public async Task AppendToFileAsync_bytes_appends_data()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("hello");
        await session.AppendToFileAsync(path, Encoding.UTF8.GetBytes(" world").AsMemory(), TestContext.Current.CancellationToken);
        Assert.Equal("hello world", File.ReadAllText(path));
    }

    [Fact]
    public async Task AppendToFileAsync_text_appends_text()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("line1\n");
        await session.AppendToFileAsync(path, "line2\n", TestContext.Current.CancellationToken);
        Assert.Equal("line1\nline2\n", File.ReadAllText(path));
    }

    [Fact]
    public async Task CopyFromAsync_file_copies_into_session()
    {
        using var session = _service.CreateSession();
        var src = Path.GetTempFileName();
        try {
            File.WriteAllText(src, "async copy content");
            var dest = await session.CopyFromAsync(src, TestContext.Current.CancellationToken);
            Assert.True(File.Exists(dest));
            Assert.Equal("async copy content", File.ReadAllText(dest));
        }
        finally { File.Delete(src); }
    }

    [Fact]
    public async Task CopyFromAsync_directory_copies_all_files()
    {
        using var session = _service.CreateSession();
        var srcDir = Path.Combine(Path.GetTempPath(), "copy-async-src-" + Guid.NewGuid().ToString("N"));
        try {
            Directory.CreateDirectory(srcDir);
            File.WriteAllText(Path.Combine(srcDir, "a.txt"), "aaa");
            File.WriteAllText(Path.Combine(srcDir, "b.txt"), "bbb");
            var dest = await session.CopyFromAsync(srcDir, TestContext.Current.CancellationToken);
            Assert.True(Directory.Exists(dest));
            Assert.Equal(2, Directory.GetFiles(dest).Length);
        }
        finally { Directory.Delete(srcDir, true); }
    }

    [Fact]
    public void MoveFrom_file_moves_into_session()
    {
        using var session = _service.CreateSession();
        var src = Path.GetTempFileName();
        File.WriteAllText(src, "move content");
        var dest = session.MoveFrom(src);
        Assert.True(File.Exists(dest));
        Assert.Equal("move content", File.ReadAllText(dest));
        Assert.False(File.Exists(src));
    }

    [Fact]
    public void MoveFrom_file_is_tracked()
    {
        using var session = _service.CreateSession();
        var src = Path.GetTempFileName();
        File.WriteAllText(src, "tracked");
        var dest = session.MoveFrom(src);
        Assert.Contains(dest, session.Files);
    }

    [Fact]
    public void MoveFrom_file_updates_byte_count()
    {
        using var session = _service.CreateSession();
        var src = Path.GetTempFileName();
        File.WriteAllText(src, "abc");
        session.MoveFrom(src);
        Assert.True(session.GetTotalBytesUsed() > 0);
    }

    [Fact]
    public void MoveFrom_throws_when_source_does_not_exist()
    {
        using var session = _service.CreateSession();
        Assert.Throws<FileNotFoundException>(() => session.MoveFrom("/nonexistent/file.txt"));
    }

    [Fact]
    public async Task MoveFromAsync_file_moves_into_session()
    {
        using var session = _service.CreateSession();
        var src = Path.GetTempFileName();
        File.WriteAllText(src, "async move");
        var dest = await session.MoveFromAsync(src, TestContext.Current.CancellationToken);
        Assert.True(File.Exists(dest));
        Assert.False(File.Exists(src));
    }

    [Fact]
    public void WriteFile_text_overwrites_content()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("original");
        session.WriteFile(path, "replaced");
        Assert.Equal("replaced", File.ReadAllText(path, Encoding.UTF8));
    }

    [Fact]
    public void WriteFile_bytes_overwrites_content()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("original");
        session.WriteFile(path, Encoding.UTF8.GetBytes("bytes").AsMemory());
        Assert.Equal("bytes", File.ReadAllText(path, Encoding.UTF8));
    }

    [Fact]
    public void WriteFile_updates_byte_count_when_growing()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("hi");
        var before = session.GetTotalBytesUsed();
        session.WriteFile(path, "much longer content here");
        Assert.True(session.GetTotalBytesUsed() > before);
    }

    [Fact]
    public void WriteFile_updates_byte_count_when_shrinking()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("much longer content here");
        var before = session.GetTotalBytesUsed();
        session.WriteFile(path, "hi");
        Assert.True(session.GetTotalBytesUsed() < before);
    }

    [Fact]
    public void WriteFile_throws_when_file_does_not_exist()
    {
        using var session = _service.CreateSession();
        var path = Path.Combine(session.SessionDirectory, "ghost.tmp");
        Assert.Throws<FileNotFoundException>(() => session.WriteFile(path, "content"));
    }

    [Fact]
    public async Task WriteFileAsync_text_overwrites_content()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("original");
        await session.WriteFileAsync(path, "replaced", TestContext.Current.CancellationToken);
        Assert.Equal("replaced", File.ReadAllText(path, Encoding.UTF8));
    }

    [Fact]
    public async Task WriteFileAsync_bytes_overwrites_content()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("original");
        await session.WriteFileAsync(path, Encoding.UTF8.GetBytes("bytes").AsMemory(), TestContext.Current.CancellationToken);
        Assert.Equal("bytes", File.ReadAllText(path, Encoding.UTF8));
    }

    [Fact]
    public void DeleteFile_removes_file_from_disk()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("delete me");
        Assert.True(session.DeleteFile(path));
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void DeleteFile_removes_from_tracking()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("delete me");
        session.DeleteFile(path);
        Assert.DoesNotContain(path, session.Files);
    }

    [Fact]
    public void DeleteFile_updates_byte_count()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("delete me");
        var before = session.GetTotalBytesUsed();
        session.DeleteFile(path);
        Assert.True(session.GetTotalBytesUsed() < before);
    }

    [Fact]
    public void DeleteFile_returns_false_when_already_absent()
    {
        using var session = _service.CreateSession();
        var path = Path.Combine(session.SessionDirectory, "ghost.tmp");
        Assert.False(session.DeleteFile(path));
    }

    [Fact]
    public void DeleteDirectory_removes_directory_from_disk()
    {
        using var session = _service.CreateSession();
        var dir = session.CreateDirectory();
        Assert.True(session.DeleteDirectory(dir));
        Assert.False(Directory.Exists(dir));
    }

    [Fact]
    public void DeleteDirectory_removes_tracked_files_inside()
    {
        using var session = _service.CreateSession();
        var dir = session.CreateDirectory();
        var file = session.CreateFile(ReadOnlyMemory<byte>.Empty, Path.Combine(Path.GetFileName(dir), "inner.tmp"));
        session.DeleteDirectory(dir);
        Assert.DoesNotContain(file, session.Files);
    }

    [Fact]
    public void DeleteDirectory_returns_false_when_already_absent()
    {
        using var session = _service.CreateSession();
        var path = Path.Combine(session.SessionDirectory, "no-such-dir");
        Assert.False(session.DeleteDirectory(path));
    }

    [Fact]
    public void DeleteDirectory_throws_when_given_session_root()
    {
        using var session = _service.CreateSession();
        Assert.Throws<InvalidOperationException>(() => session.DeleteDirectory(session.SessionDirectory));
    }

    [Fact]
    public void FileCreated_fires_on_TouchFile()
    {
        using var session = _service.CreateSession();
        string? raised = null;
        session.FileCreated += p => raised = p;
        var path = session.TouchFile();
        Assert.Equal(path, raised);
    }

    [Fact]
    public void FileCreated_fires_on_CreateFile()
    {
        using var session = _service.CreateSession();
        var events = new List<string>();
        session.FileCreated += p => events.Add(p);
        session.CreateFile("test content");
        Assert.Single(events);
    }

    [Fact]
    public void DirectoryCreated_fires_on_CreateDirectory()
    {
        using var session = _service.CreateSession();
        string? raised = null;
        session.DirectoryCreated += p => raised = p;
        var path = session.CreateDirectory();
        Assert.Equal(path, raised);
    }

    [Fact]
    public void DirectoryCreated_fires_on_CreateSubSession()
    {
        using var session = _service.CreateSession();
        string? raised = null;
        session.DirectoryCreated += p => raised = p;
        using var sub = session.CreateSubSession();
        Assert.Equal(sub.SessionDirectory, raised);
    }

    [Fact]
    public void FileCreated_fires_on_Generator_CreateRandomFile()
    {
        using var session = _service.CreateSession();
        string? raised = null;
        session.FileCreated += p => raised = p;
        var path = session.Generator.CreateRandomFile(256);
        Assert.Equal(path, raised);
    }

    [Fact]
    public void DirectoryCreated_fires_on_Generator_SimulateDirectory()
    {
        using var session = _service.CreateSession();
        var dirEvents = new List<string>();
        session.DirectoryCreated += p => dirEvents.Add(p);
        session.Generator.SimulateDirectory(TempDirectorySpec.Flat(1, 128));
        Assert.Single(dirEvents);
    }

    [Fact]
    public void CopyFrom_file_fires_FileCreated_event()
    {
        using var session = _service.CreateSession();
        var raised = new List<string>();
        session.FileCreated += p => raised.Add(p);
        var src = Path.GetTempFileName();
        try {
            File.WriteAllText(src, "event test");
            session.CopyFrom(src);
        }
        finally { File.Delete(src); }
        Assert.Single(raised);
    }

    [Fact]
    public void DebuggerDisplay_attribute_is_present()
    {
        var attrs = typeof(IOTempSession).GetCustomAttributes(typeof(System.Diagnostics.DebuggerDisplayAttribute), false);
        Assert.NotEmpty(attrs);
    }

    [Fact]
    public void AssertFilesExist_passes_when_all_files_present()
    {
        using var session = _service.CreateSession();
        session.Generator.CreateRandomFile(64);
        session.AssertFilesExist();
    }

    [Fact]
    public void AssertFilesExist_throws_when_file_deleted_externally()
    {
        using var session = _service.CreateSession();
        var path = session.Generator.CreateRandomFile(64);
        File.Delete(path);
        Assert.Throws<InvalidOperationException>(() => session.AssertFilesExist());
    }

    [Fact]
    public void AssertTotalSize_passes_within_tolerance()
    {
        using var session = _service.CreateSession();
        session.Generator.CreateRandomFile(1000);
        session.AssertTotalSize(1000);
        session.AssertTotalSize(990, toleranceBytes: 20);
    }

    [Fact]
    public void AssertTotalSize_throws_when_outside_tolerance()
    {
        using var session = _service.CreateSession();
        session.Generator.CreateRandomFile(500);
        Assert.Throws<InvalidOperationException>(() => session.AssertTotalSize(600, toleranceBytes: 10));
    }
}
