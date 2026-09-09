using System.Diagnostics;
using System.Text;
using Lyo.IO.Temp.Enums;
using Lyo.IO.Temp.Models;

namespace Lyo.IO.Temp.Tests;

public sealed class IOTempSessionTests : IDisposable
{
    private readonly IOTempService _service = new(TestOptions());

    public void Dispose() => _service.Dispose();

    private static IOTempServiceOptions TestOptions()
        => new() { TempRoot = Path.Combine(Path.GetTempPath(), "lyo-io-session-tests"), DirectoryName = Guid.NewGuid().ToString("N") };

    [Fact]
    public void Dispose_Cleans_UpDirectory()
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
    public void Disposed_Session_ThrowsOnTouchFile()
    {
        var session = _service.CreateSession();
        session.Dispose();
        Assert.Throws<ObjectDisposedException>(() => session.TouchFile());
    }

    [Fact]
    public void GetFilePath_Returns_PathWithoutCreatingFile()
    {
        using var session = _service.CreateSession();
        var path = session.GetFilePath("planned.txt");
        Assert.False(File.Exists(path));
        Assert.EndsWith("planned.txt", path);
    }

    [Fact]
    public void TouchFile_Creates_EmptyFile()
    {
        using var session = _service.CreateSession();
        var path = session.TouchFile("empty.tmp");
        Assert.True(File.Exists(path));
        Assert.Equal(0, new FileInfo(path).Length);
        Assert.Single(session.Files);
        Assert.Contains(path, session.Files);
    }

    [Fact]
    public void CreateFile_Text_WritesContent()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("Hello from session");
        Assert.True(File.Exists(path));
        Assert.Equal("Hello from session", File.ReadAllText(path));
    }

    [Fact]
    public void CreateFile_Bytes_WritesContent()
    {
        using var session = _service.CreateSession();
        var data = "Binary data"u8.ToArray();
        var path = session.CreateFile(new ReadOnlyMemory<byte>(data), "data.bin");
        Assert.True(File.Exists(path));
        Assert.Equal("Binary data", Encoding.UTF8.GetString(File.ReadAllBytes(path)));
    }

    [Fact]
    public void CreateDirectory_Creates_Directory()
    {
        using var session = _service.CreateSession();
        var path = session.CreateDirectory("assets");
        Assert.True(Directory.Exists(path));
        Assert.Single(session.Directories);
        Assert.Contains(path, session.Directories);
    }

    [Fact]
    public async Task CreateFileAsync_Writes_Content()
    {
        await using var session = _service.CreateSession();
        var path = await session.CreateFileAsync("Async content", TestContext.Current.CancellationToken);
        Assert.True(File.Exists(path));
        Assert.Equal("Async content", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void GetTotalBytesUsed_Accumulates_AcrossFiles()
    {
        using var session = _service.CreateSession();
        session.Generator.CreateRandomFile(512);
        session.Generator.CreateRandomFile(256);
        Assert.Equal(768, session.GetTotalBytesUsed());
    }

    [Fact]
    public void GetTotalBytesUsed_Includes_GeneratorAndSessionCreateFile()
    {
        using var session = _service.CreateSession();
        session.Generator.CreateRandomFile(100);
        session.CreateFile(new ReadOnlyMemory<byte>(new byte[50]));
        Assert.Equal(150, session.GetTotalBytesUsed());
    }

    [Fact]
    public void GetSnapshot_Captures_CurrentState()
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
    public void GetSnapshot_Is_ACopyNotALiveView()
    {
        using var session = _service.CreateSession();
        var snap = session.GetSnapshot();
        session.Generator.CreateRandomFile(64);
        Assert.Empty(snap.Files);
    }

    [Fact]
    public void CreateSubSession_Creates_DirectoryUnderParent()
    {
        using var session = _service.CreateSession();
        using var sub = session.CreateSubSession();
        Assert.True(Directory.Exists(sub.SessionDirectory));
        Assert.StartsWith(session.SessionDirectory, sub.SessionDirectory);
    }

    [Fact]
    public void CreateSubSession_Is_TrackedInParentDirectories()
    {
        using var session = _service.CreateSession();
        using var sub = session.CreateSubSession();
        Assert.Contains(sub.SessionDirectory, session.Directories);
    }

    [Fact]
    public void CreateSubSession_Has_IndependentFileTracking()
    {
        using var session = _service.CreateSession();
        using var sub = session.CreateSubSession();
        sub.Generator.CreateRandomFile(100);
        Assert.Empty(session.Files);
        Assert.Single(sub.Files);
    }

    [Fact]
    public void EnumerateFiles_Finds_AllFilesIncludingUntracked()
    {
        using var session = _service.CreateSession();
        session.Generator.CreateRandomFile(64);
        File.WriteAllText(Path.Combine(session.SessionDirectory, "untracked.txt"), "hello");
        var all = session.EnumerateFiles().ToList();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void EnumerateFiles_WithPattern_FiltersResults()
    {
        using var session = _service.CreateSession();
        session.Generator.CreateRandomFile(32, "a.tmp");
        File.WriteAllText(Path.Combine(session.SessionDirectory, "b.log"), "log");
        var tmpFiles = session.EnumerateFiles("*.tmp").ToList();
        Assert.Single(tmpFiles);
        Assert.EndsWith(".tmp", tmpFiles[0]);
    }

    [Fact]
    public void EnumerateDirectories_Finds_AllSubdirectories()
    {
        using var session = _service.CreateSession();
        session.Generator.SimulateDirectory(2, 32);
        session.CreateDirectory("manual-dir");
        var dirs = session.EnumerateDirectories().ToList();
        Assert.Equal(2, dirs.Count);
    }

    [Fact]
    public void Clear_Removes_AllTrackedFiles()
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
    public void Clear_ResetsByte_Count()
    {
        using var session = _service.CreateSession();
        session.Generator.CreateRandomFile(1024);
        Assert.True(session.GetTotalBytesUsed() > 0);
        session.Clear();
        Assert.Equal(0, session.GetTotalBytesUsed());
    }

    [Fact]
    public void Clear_DeletesFilesFrom_Disk()
    {
        using var session = _service.CreateSession();
        var path = session.Generator.CreateRandomFile(256);
        Assert.True(File.Exists(path));
        session.Clear();
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Clear_SessionRemainsUsable_Afterwards()
    {
        using var session = _service.CreateSession();
        session.Generator.CreateRandomFile(256);
        session.Clear();
        var path = session.TouchFile();
        Assert.True(File.Exists(path));
        Assert.Single(session.Files);
    }

    [Fact]
    public void CopyFrom_File_CopiesIntoSession()
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
        finally {
            File.Delete(src);
        }
    }

    [Fact]
    public void CopyFrom_File_UpdatesByteCount()
    {
        using var session = _service.CreateSession();
        var src = Path.GetTempFileName();
        try {
            File.WriteAllBytes(src, new byte[2048]);
            session.CopyFrom(src);
            Assert.Equal(2048, session.GetTotalBytesUsed());
        }
        finally {
            File.Delete(src);
        }
    }

    [Fact]
    public void CopyFrom_Directory_CopiesAllFilesIntoSession()
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
        finally {
            Directory.Delete(srcDir, true);
        }
    }

    [Fact]
    public void CopyFrom_ThrowsWhenSource_DoesNotExist()
    {
        using var session = _service.CreateSession();
        Assert.Throws<FileNotFoundException>(() => session.CopyFrom("/nonexistent/path/file.txt"));
    }

    [Fact]
    public void AppendToFile_Bytes_AppendsData()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("hello");
        session.AppendToFile(path, Encoding.UTF8.GetBytes(" world").AsMemory());
        Assert.Equal("hello world", File.ReadAllText(path));
    }

    [Fact]
    public void AppendToFile_Text_AppendsText()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("line1\n");
        session.AppendToFile(path, "line2\n");
        Assert.Equal("line1\nline2\n", File.ReadAllText(path));
    }

    [Fact]
    public void AppendToFile_Updates_ByteCount()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("abc");
        var initialBytes = session.GetTotalBytesUsed();
        session.AppendToFile(path, "xyz");
        Assert.True(session.GetTotalBytesUsed() > initialBytes);
    }

    [Fact]
    public void AppendToFile_Throws_WhenFileNotFound()
    {
        using var session = _service.CreateSession();
        var fakePath = Path.Combine(session.SessionDirectory, "nonexistent.tmp");
        Assert.Throws<FileNotFoundException>(() => session.AppendToFile(fakePath, "data"));
    }

    [Fact]
    public async Task AppendToFileAsync_Bytes_AppendsData()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("hello");
        await session.AppendToFileAsync(path, Encoding.UTF8.GetBytes(" world").AsMemory(), TestContext.Current.CancellationToken);
        Assert.Equal("hello world", File.ReadAllText(path));
    }

    [Fact]
    public async Task AppendToFileAsync_Text_AppendsText()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("line1\n");
        await session.AppendToFileAsync(path, "line2\n", TestContext.Current.CancellationToken);
        Assert.Equal("line1\nline2\n", File.ReadAllText(path));
    }

    [Fact]
    public async Task CopyFromAsync_File_CopiesIntoSession()
    {
        using var session = _service.CreateSession();
        var src = Path.GetTempFileName();
        try {
            File.WriteAllText(src, "async copy content");
            var dest = await session.CopyFromAsync(src, TestContext.Current.CancellationToken);
            Assert.True(File.Exists(dest));
            Assert.Equal("async copy content", File.ReadAllText(dest));
        }
        finally {
            File.Delete(src);
        }
    }

    [Fact]
    public async Task CopyFromAsync_Directory_CopiesAllFiles()
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
        finally {
            Directory.Delete(srcDir, true);
        }
    }

    [Fact]
    public void MoveFrom_File_MovesIntoSession()
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
    public void MoveFrom_File_IsTracked()
    {
        using var session = _service.CreateSession();
        var src = Path.GetTempFileName();
        File.WriteAllText(src, "tracked");
        var dest = session.MoveFrom(src);
        Assert.Contains(dest, session.Files);
    }

    [Fact]
    public void MoveFrom_File_UpdatesByteCount()
    {
        using var session = _service.CreateSession();
        var src = Path.GetTempFileName();
        File.WriteAllText(src, "abc");
        session.MoveFrom(src);
        Assert.True(session.GetTotalBytesUsed() > 0);
    }

    [Fact]
    public void MoveFrom_ThrowsWhenSource_DoesNotExist()
    {
        using var session = _service.CreateSession();
        Assert.Throws<FileNotFoundException>(() => session.MoveFrom("/nonexistent/file.txt"));
    }

    [Fact]
    public async Task MoveFromAsync_File_MovesIntoSession()
    {
        using var session = _service.CreateSession();
        var src = Path.GetTempFileName();
        File.WriteAllText(src, "async move");
        var dest = await session.MoveFromAsync(src, TestContext.Current.CancellationToken);
        Assert.True(File.Exists(dest));
        Assert.False(File.Exists(src));
    }

    [Fact]
    public void WriteFile_Text_OverwritesContent()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("original");
        session.WriteFile(path, "replaced");
        Assert.Equal("replaced", File.ReadAllText(path, Encoding.UTF8));
    }

    [Fact]
    public void WriteFile_Bytes_OverwritesContent()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("original");
        session.WriteFile(path, Encoding.UTF8.GetBytes("bytes").AsMemory());
        Assert.Equal("bytes", File.ReadAllText(path, Encoding.UTF8));
    }

    [Fact]
    public void WriteFile_Updates_ByteCountWhenGrowing()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("hi");
        var before = session.GetTotalBytesUsed();
        session.WriteFile(path, "much longer content here");
        Assert.True(session.GetTotalBytesUsed() > before);
    }

    [Fact]
    public void WriteFile_Updates_ByteCountWhenShrinking()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("much longer content here");
        var before = session.GetTotalBytesUsed();
        session.WriteFile(path, "hi");
        Assert.True(session.GetTotalBytesUsed() < before);
    }

    [Fact]
    public void WriteFile_ThrowsWhenFile_DoesNotExist()
    {
        using var session = _service.CreateSession();
        var path = Path.Combine(session.SessionDirectory, "ghost.tmp");
        Assert.Throws<FileNotFoundException>(() => session.WriteFile(path, "content"));
    }

    [Fact]
    public async Task WriteFileAsync_Text_OverwritesContent()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("original");
        await session.WriteFileAsync(path, "replaced", TestContext.Current.CancellationToken);
        Assert.Equal("replaced", File.ReadAllText(path, Encoding.UTF8));
    }

    [Fact]
    public async Task WriteFileAsync_Bytes_OverwritesContent()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("original");
        await session.WriteFileAsync(path, Encoding.UTF8.GetBytes("bytes").AsMemory(), TestContext.Current.CancellationToken);
        Assert.Equal("bytes", File.ReadAllText(path, Encoding.UTF8));
    }

    [Fact]
    public void DeleteFile_Removes_FileFromDisk()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("delete me");
        Assert.True(session.DeleteFile(path));
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void DeleteFile_Removes_FromTracking()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("delete me");
        session.DeleteFile(path);
        Assert.DoesNotContain(path, session.Files);
    }

    [Fact]
    public void DeleteFile_Updates_ByteCount()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("delete me");
        var before = session.GetTotalBytesUsed();
        session.DeleteFile(path);
        Assert.True(session.GetTotalBytesUsed() < before);
    }

    [Fact]
    public void DeleteFile_Returns_FalseWhenAlreadyAbsent()
    {
        using var session = _service.CreateSession();
        var path = Path.Combine(session.SessionDirectory, "ghost.tmp");
        Assert.False(session.DeleteFile(path));
    }

    [Fact]
    public void DeleteDirectory_Removes_DirectoryFromDisk()
    {
        using var session = _service.CreateSession();
        var dir = session.CreateDirectory();
        Assert.True(session.DeleteDirectory(dir));
        Assert.False(Directory.Exists(dir));
    }

    [Fact]
    public void DeleteDirectory_Removes_TrackedFilesInside()
    {
        using var session = _service.CreateSession();
        var dir = session.CreateDirectory();
        var file = session.CreateFile(ReadOnlyMemory<byte>.Empty, Path.Combine(Path.GetFileName(dir), "inner.tmp"));
        session.DeleteDirectory(dir);
        Assert.DoesNotContain(file, session.Files);
    }

    [Fact]
    public void DeleteDirectory_Returns_FalseWhenAlreadyAbsent()
    {
        using var session = _service.CreateSession();
        var path = Path.Combine(session.SessionDirectory, "no-such-dir");
        Assert.False(session.DeleteDirectory(path));
    }

    [Fact]
    public void DeleteDirectory_Throws_WhenGivenSessionRoot()
    {
        using var session = _service.CreateSession();
        Assert.Throws<InvalidOperationException>(() => session.DeleteDirectory(session.SessionDirectory));
    }

    [Fact]
    public void FileCreated_Fires_OnTouchFile()
    {
        using var session = _service.CreateSession();
        string? raised = null;
        session.FileCreated += p => raised = p;
        var path = session.TouchFile();
        Assert.Equal(path, raised);
    }

    [Fact]
    public void FileCreated_Fires_OnCreateFile()
    {
        using var session = _service.CreateSession();
        var events = new List<string>();
        session.FileCreated += p => events.Add(p);
        session.CreateFile("test content");
        Assert.Single(events);
    }

    [Fact]
    public void DirectoryCreated_Fires_OnCreateDirectory()
    {
        using var session = _service.CreateSession();
        string? raised = null;
        session.DirectoryCreated += p => raised = p;
        var path = session.CreateDirectory();
        Assert.Equal(path, raised);
    }

    [Fact]
    public void DirectoryCreated_Fires_OnCreateSubSession()
    {
        using var session = _service.CreateSession();
        string? raised = null;
        session.DirectoryCreated += p => raised = p;
        using var sub = session.CreateSubSession();
        Assert.Equal(sub.SessionDirectory, raised);
    }

    [Fact]
    public void FileCreated_Fires_OnGeneratorCreateRandomFile()
    {
        using var session = _service.CreateSession();
        string? raised = null;
        session.FileCreated += p => raised = p;
        var path = session.Generator.CreateRandomFile(256);
        Assert.Equal(path, raised);
    }

    [Fact]
    public void DirectoryCreated_Fires_OnGeneratorSimulateDirectory()
    {
        using var session = _service.CreateSession();
        var dirEvents = new List<string>();
        session.DirectoryCreated += p => dirEvents.Add(p);
        session.Generator.SimulateDirectory(TempDirectorySpec.Flat(1, 128));
        Assert.Single(dirEvents);
    }

    [Fact]
    public void CopyFrom_File_FiresFileCreatedEvent()
    {
        using var session = _service.CreateSession();
        var raised = new List<string>();
        session.FileCreated += p => raised.Add(p);
        var src = Path.GetTempFileName();
        try {
            File.WriteAllText(src, "event test");
            session.CopyFrom(src);
        }
        finally {
            File.Delete(src);
        }

        Assert.Single(raised);
    }

    [Fact]
    public void FileWritten_Fires_OnWriteFile()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("orig");
        string? raised = null;
        session.FileWritten += p => raised = p;
        session.WriteFile(path, "replaced");
        Assert.Equal(path, raised);
    }

    [Fact]
    public async Task FileWritten_Fires_OnWriteFileAsync()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("orig");
        string? raised = null;
        session.FileWritten += p => raised = p;
        await session.WriteFileAsync(path, "replaced", TestContext.Current.CancellationToken);
        Assert.Equal(path, raised);
    }

    [Fact]
    public void FileAppended_Fires_OnAppendToFile()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("start");
        string? raised = null;
        session.FileAppended += p => raised = p;
        session.AppendToFile(path, "-end");
        Assert.Equal(path, raised);
    }

    [Fact]
    public async Task FileAppended_Fires_OnAppendToFileAsync()
    {
        using var session = _service.CreateSession();
        var path = session.CreateFile("start");
        string? raised = null;
        session.FileAppended += p => raised = p;
        await session.AppendToFileAsync(path, "-end", TestContext.Current.CancellationToken);
        Assert.Equal(path, raised);
    }

    [Fact]
    public void FileDeleted_Fires_OnDeleteFile()
    {
        using var session = _service.CreateSession();
        var path = session.TouchFile();
        string? raised = null;
        session.FileDeleted += p => raised = p;
        Assert.True(session.DeleteFile(path));
        Assert.Equal(path, raised);
    }

    [Fact]
    public void FileDeleted_Does_NotFireWhenAlreadyAbsent()
    {
        using var session = _service.CreateSession();
        var path = Path.Combine(session.SessionDirectory, "ghost.tmp");
        var raised = false;
        session.FileDeleted += _ => raised = true;
        Assert.False(session.DeleteFile(path));
        Assert.False(raised);
    }

    [Fact]
    public void DirectoryDeleted_Fires_OnDeleteDirectory()
    {
        using var session = _service.CreateSession();
        var dir = session.CreateDirectory();
        string? raised = null;
        session.DirectoryDeleted += p => raised = p;
        Assert.True(session.DeleteDirectory(dir));
        Assert.Equal(dir, raised);
    }

    [Fact]
    public void DirectoryDeleted_Does_NotFireWhenAlreadyAbsent()
    {
        using var session = _service.CreateSession();
        var path = Path.Combine(session.SessionDirectory, "no-such-dir");
        var raised = false;
        session.DirectoryDeleted += _ => raised = true;
        Assert.False(session.DeleteDirectory(path));
        Assert.False(raised);
    }

    [Fact]
    public void DeleteDirectory_Fires_EventsForNestedFilesAndDirs()
    {
        using var session = _service.CreateSession();
        var dir = session.CreateDirectory();
        var nested = session.CreateDirectory(Path.Combine(Path.GetFileName(dir), "nested"));
        var file = session.CreateFile(ReadOnlyMemory<byte>.Empty, Path.Combine(Path.GetFileName(dir), "inner.tmp"));
        var deletedFiles = new List<string>();
        var deletedDirs = new List<string>();
        session.FileDeleted += deletedFiles.Add;
        session.DirectoryDeleted += deletedDirs.Add;
        session.DeleteDirectory(dir);
        Assert.Equal([file], deletedFiles);
        Assert.Contains(nested, deletedDirs);
        Assert.Contains(dir, deletedDirs);
    }

    [Fact]
    public void Clear_Fires_FileDeletedDirectoryDeletedAndCleared()
    {
        using var session = _service.CreateSession();
        var file = session.CreateFile("content");
        var dir = session.CreateDirectory();
        var deletedFiles = new List<string>();
        var deletedDirs = new List<string>();
        string? cleared = null;
        session.FileDeleted += deletedFiles.Add;
        session.DirectoryDeleted += deletedDirs.Add;
        session.Cleared += p => cleared = p;
        session.Clear();
        Assert.Equal([file], deletedFiles);
        Assert.Equal([dir], deletedDirs);
        Assert.Equal(session.SessionDirectory, cleared);
    }

    [Fact]
    public void Overflow_AndFileDeletedFireOnFileCount_Eviction()
    {
        using var session = _service.CreateSession(new IOTempSessionOptions { MaxFileCount = 1, OverflowStrategy = TempOverflowStrategy.DeleteOldest });
        var first = session.CreateFile("first");
        var overflow = new List<string>();
        var deleted = new List<string>();
        session.Overflow += overflow.Add;
        session.FileDeleted += deleted.Add;
        session.CreateFile("second");
        Assert.Equal([first], overflow);
        Assert.Equal([first], deleted);
    }

    [Fact]
    public void Disposed_Fires_OnDisposeWithoutPerFileDeletes()
    {
        var session = _service.CreateSession();
        session.TouchFile();
        var deleted = new List<string>();
        string? disposed = null;
        session.FileDeleted += deleted.Add;
        session.Disposed += p => disposed = p;
        var dir = session.SessionDirectory;
        session.Dispose();
        Assert.Empty(deleted);
        Assert.Equal(dir, disposed);
    }

    [Fact]
    public void DebuggerDisplay_Attribute_IsPresent()
    {
        var attrs = typeof(IOTempSession).GetCustomAttributes(typeof(DebuggerDisplayAttribute), false);
        Assert.NotEmpty(attrs);
    }

    [Fact]
    public void AssertFilesExist_Passes_WhenAllFilesPresent()
    {
        using var session = _service.CreateSession();
        session.Generator.CreateRandomFile(64);
        session.AssertFilesExist();
    }

    [Fact]
    public void AssertFilesExist_Throws_WhenFileDeletedExternally()
    {
        using var session = _service.CreateSession();
        var path = session.Generator.CreateRandomFile(64);
        File.Delete(path);
        Assert.Throws<InvalidOperationException>(() => session.AssertFilesExist());
    }

    [Fact]
    public void AssertTotalSize_Passes_WithinTolerance()
    {
        using var session = _service.CreateSession();
        session.Generator.CreateRandomFile(1000);
        session.AssertTotalSize(1000);
        session.AssertTotalSize(990, 20);
    }

    [Fact]
    public void AssertTotalSize_Throws_WhenOutsideTolerance()
    {
        using var session = _service.CreateSession();
        session.Generator.CreateRandomFile(500);
        Assert.Throws<InvalidOperationException>(() => session.AssertTotalSize(600, 10));
    }
}