using System.Text;
using Lyo.Common.Metadata.Records;
using Lyo.IO.Temp.Models;
using Lyo.Testing;
using Microsoft.Extensions.Logging;

// ReSharper disable AccessToModifiedClosure

namespace Lyo.FileSystemWatcher.Tests;

public class FingerprintTests : IDisposable
{
    private readonly IIOTempSession _tempSession;

    public FingerprintTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _tempSession = new IOTempSession(new(), loggerFactory.CreateLogger<IOTempSession>());
    }

    public void Dispose() => _tempSession.Dispose();

    [Fact]
    public async Task Fingerprint_SmallFile_ReadsFirstAndLastBytes()
    {
        // Small file (<100MB) uses first + last bytes
        var filePath = _tempSession.GetFilePath();
        var content = "Hello World! This is a test file for fingerprinting.";
        await File.WriteAllTextAsync(filePath, content, TestContext.Current.CancellationToken);
        var fileInfo = new FileInfo(filePath);
        var fingerprint = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken);
        Assert.NotNull(fingerprint);
        Assert.NotEmpty(fingerprint);
        Assert.Equal(16, fingerprint.Length); // MD5 hash is 16 bytes

        // Fingerprint should match for the same file
        var fingerprint2 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken);
        Assert.Equal(fingerprint, fingerprint2);
    }

    [Fact]
    public async Task Fingerprint_SmallFile_DetectsEndChange()
    {
        // Fingerprint should notice changes at the end of small files
        var filePath = _tempSession.GetFilePath();
        await File.WriteAllTextAsync(filePath, "Initial content", TestContext.Current.CancellationToken);
        var fileInfo = new FileInfo(filePath);
        var fingerprint1 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken);

        // Append at the end of the file
        await File.AppendAllTextAsync(filePath, " - appended", TestContext.Current.CancellationToken);
        fileInfo.Refresh();
        var fingerprint2 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken);

        // Fingerprints should differ (size and last bytes changed)
        Assert.NotEqual(fingerprint1, fingerprint2);
    }

    [Fact]
    public async Task Fingerprint_MediumFile_UsesSparseSampling()
    {
        // Medium file (100MB - 1GB) uses beginning + middle + end
        var filePath = _tempSession.GetFilePath();
        var size = FileSizeUnitInfo.Megabyte.ConvertToBytes(150);
        CreateFileOfSize(filePath, size);
        var fileInfo = new FileInfo(filePath);
        var fingerprint = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken);
        Assert.NotNull(fingerprint);
        Assert.NotEmpty(fingerprint);
        Assert.Equal(16, fingerprint.Length); // MD5 hash is 16 bytes

        // Fingerprint should be stable
        var fingerprint2 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken);
        Assert.Equal(fingerprint, fingerprint2);
    }

    [Fact]
    public async Task Fingerprint_MediumFile_DetectsEndChange()
    {
        // Fingerprint should notice changes at the end of medium files
        var filePath = _tempSession.GetFilePath();
        var size = FileSizeUnitInfo.Megabyte.ConvertToBytes(150);
        CreateFileOfSize(filePath, size);
        var fileInfo = new FileInfo(filePath);
        var fingerprint1 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken);

        // Change the end of the file
        await using (var stream = File.OpenWrite(filePath)) {
            stream.Seek(-100, SeekOrigin.End);
            var bytes = Encoding.UTF8.GetBytes("MODIFIED");
            stream.Write(bytes, 0, bytes.Length);
        }

        fileInfo.Refresh();
        var fingerprint2 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken);

        // Fingerprints should differ (last bytes changed)
        Assert.NotEqual(fingerprint1, fingerprint2);
    }

    [Fact]
    public async Task Fingerprint_LargeFile_UsesSparseSamplingWithModTime()
    {
        // Very large file (>1GB) uses beginning + middle + modification time
        var filePath = _tempSession.GetFilePath();
        var size = FileSizeUnitInfo.Gigabyte.ConvertToBytes(1.2);
        CreateFileOfSize(filePath, size);
        var fileInfo = new FileInfo(filePath);
        var fingerprint = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken);
        Assert.NotNull(fingerprint);
        Assert.NotEmpty(fingerprint);
        Assert.Equal(16, fingerprint.Length); // MD5 hash is 16 bytes

        // Fingerprint should be stable
        var fingerprint2 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken);
        Assert.Equal(fingerprint, fingerprint2);
    }

    [Fact]
    public async Task Fingerprint_LargeFile_DetectsModificationTimeChange()
    {
        // Fingerprint should notice modification-time changes for very large files
        var filePath = _tempSession.GetFilePath();
        var size = FileSizeUnitInfo.Gigabyte.ConvertToBytes(1.2);
        CreateFileOfSize(filePath, size);
        var fileInfo = new FileInfo(filePath);
        var fingerprint1 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken);

        // Touch the file so modification time updates
        File.SetLastWriteTime(filePath, DateTime.UtcNow.AddSeconds(1));
        fileInfo.Refresh();
        var fingerprint2 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken);

        // Fingerprints should differ (modification time changed)
        Assert.NotEqual(fingerprint1, fingerprint2);
    }

    [Fact]
    public async Task Fingerprint_DifferentSizes_DifferentFingerprints()
    {
        // Files with different sizes should produce different fingerprints
        var file1 = _tempSession.GetFilePath();
        var file2 = _tempSession.GetFilePath();
        await File.WriteAllTextAsync(file1, "Short", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(file2, "Much longer content here", TestContext.Current.CancellationToken);
        var info1 = new FileInfo(file1);
        var info2 = new FileInfo(file2);
        var fingerprint1 = await Utilities.Fingerprint(file1, info1.Length, TestContext.Current.CancellationToken);
        var fingerprint2 = await Utilities.Fingerprint(file2, info2.Length, TestContext.Current.CancellationToken);
        Assert.NotEqual(fingerprint1, fingerprint2);
    }

    [Fact]
    public async Task Fingerprint_SameContent_SameFingerprint()
    {
        // Files with the same content should share a fingerprint (when size matches)
        var file1 = _tempSession.GetFilePath();
        var file2 = _tempSession.GetFilePath();
        var content = "Identical content for both files";
        await File.WriteAllTextAsync(file1, content, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(file2, content, TestContext.Current.CancellationToken);
        var info1 = new FileInfo(file1);
        var info2 = new FileInfo(file2);
        var fingerprint1 = await Utilities.Fingerprint(file1, info1.Length, TestContext.Current.CancellationToken);
        var fingerprint2 = await Utilities.Fingerprint(file2, info2.Length, TestContext.Current.CancellationToken);
        Assert.Equal(fingerprint1, fingerprint2);
    }

    [Fact]
    public async Task Fingerprint_EmptyFile_Works()
    {
        // Empty file should still yield a fingerprint
        var filePath = _tempSession.GetFilePath();
        await File.Create(filePath).DisposeAsync();
        var fileInfo = new FileInfo(filePath);
        var fingerprint = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken);
        Assert.NotNull(fingerprint);
        Assert.NotEmpty(fingerprint);
        Assert.Equal(16, fingerprint.Length);
    }

    [Fact]
    public async Task Fingerprint_TextFile_DetectsEndAppend()
    {
        // Appending to a text file should change the fingerprint
        var filePath = _tempSession.GetFilePath();
        await File.WriteAllTextAsync(filePath, "Line 1\nLine 2\n", TestContext.Current.CancellationToken);
        var fileInfo = new FileInfo(filePath);
        var fingerprint1 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken);

        // Append a new line
        await File.AppendAllTextAsync(filePath, "Line 3\n", TestContext.Current.CancellationToken);
        fileInfo.Refresh();
        var fingerprint2 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken);

        // Fingerprints should differ
        Assert.NotEqual(fingerprint1, fingerprint2);
    }

    [Fact]
    public async Task Fingerprint_CSVFile_DetectsEndChange()
    {
        // CSV file end-change detection
        var filePath = _tempSession.GetFilePath();
        await File.WriteAllTextAsync(filePath, "Name,Age,City\nJohn,30,NYC\nJane,25,LA\n", TestContext.Current.CancellationToken);
        var fileInfo = new FileInfo(filePath);
        var fingerprint1 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken);

        // Add a new row
        await File.AppendAllTextAsync(filePath, "Bob,35,Chicago\n", TestContext.Current.CancellationToken);
        fileInfo.Refresh();
        var fingerprint2 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken);

        // Fingerprints should differ
        Assert.NotEqual(fingerprint1, fingerprint2);
    }

    [Fact]
    public async Task Fingerprint_MediumFile_DetectsMiddleChange()
    {
        // Middle changes should be detected for medium files
        var filePath = _tempSession.GetFilePath();
        var size = FileSizeUnitInfo.Megabyte.ConvertToBytes(150);
        CreateFileOfSize(filePath, size);
        var fileInfo = new FileInfo(filePath);
        var fingerprint1 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken);

        // Change the middle of the file
        await using (var stream = File.OpenWrite(filePath)) {
            stream.Seek(size / 2, SeekOrigin.Begin);
            var bytes = Encoding.UTF8.GetBytes("MODIFIED_MIDDLE");
            stream.Write(bytes, 0, bytes.Length);
        }

        fileInfo.Refresh();
        var fingerprint2 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken);

        // Fingerprints should differ (middle bytes changed)
        Assert.NotEqual(fingerprint1, fingerprint2);
    }

    [Fact]
    public async Task Fingerprint_NonExistentFile_ReturnsNull()
    {
        var nonExistentPath = _tempSession.GetFilePath();
        File.Delete(nonExistentPath);
        var fingerprint = await Utilities.Fingerprint(nonExistentPath, 100, TestContext.Current.CancellationToken);
        Assert.Null(fingerprint);
    }

    [Fact]
    public async Task Fingerprint_ThresholdBoundaries_WorkCorrectly()
    {
        // Files at the threshold boundaries
        var smallFile = _tempSession.GetFilePath();
        var mediumFile = _tempSession.GetFilePath();
        var largeFile = _tempSession.GetFilePath();

        // Slightly under 100MB
        CreateFileOfSize(smallFile, FileSizeUnitInfo.Megabyte.ConvertToBytes(99.9));
        // Just above 100MB
        CreateFileOfSize(mediumFile, FileSizeUnitInfo.Megabyte.ConvertToBytes(100.1));
        // Just above 1GB
        CreateFileOfSize(largeFile, FileSizeUnitInfo.Gigabyte.ConvertToBytes(1));
        var smallInfo = new FileInfo(smallFile);
        var mediumInfo = new FileInfo(mediumFile);
        var largeInfo = new FileInfo(largeFile);
        var smallFp = await Utilities.Fingerprint(smallFile, smallInfo.Length, TestContext.Current.CancellationToken);
        var mediumFp = await Utilities.Fingerprint(mediumFile, mediumInfo.Length, TestContext.Current.CancellationToken);
        var largeFp = await Utilities.Fingerprint(largeFile, largeInfo.Length, TestContext.Current.CancellationToken);
        Assert.NotNull(smallFp);
        Assert.NotNull(mediumFp);
        Assert.NotNull(largeFp);
        Assert.NotEmpty(smallFp);
        Assert.NotEmpty(mediumFp);
        Assert.NotEmpty(largeFp);

        // All should differ
        Assert.NotEqual(smallFp, mediumFp);
        Assert.NotEqual(mediumFp, largeFp);
        Assert.NotEqual(smallFp, largeFp);
    }

    [Fact]
    public async Task LargeFile_Created_EventFiresQuickly()
    {
        // Creating a large file (>1GB) should fire quickly because of fingerprinting
        var options = new FileSystemWatcherOptions { EnableFileHashing = true };
        var largeFilePath = _tempSession.GetFilePath();
        var size = FileSizeUnitInfo.Gigabyte.ConvertToBytes(1.2);
        var createdFired = false;
        var eventTime = TimeSpan.Zero;
        var startTime = DateTime.UtcNow;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options);
        watcher.FileCreated += (_, e) => {
            if (e.NewPath != largeFilePath)
                return;

            createdFired = true;
            eventTime = DateTime.UtcNow - startTime;
        };

        // Wait until the initial snapshot finishes
        await Task.Delay(500, TestContext.Current.CancellationToken);
        startTime = DateTime.UtcNow;
        CreateFileOfSize(largeFilePath, size);

        // Wait for the event with a reasonable timeout (should be fast due to fingerprinting)
        await PollAssert.ThatAsync(() => createdFired, TimeSpan.FromSeconds(5));
        Assert.True(createdFired);
        // Event should fire quickly (< 2 seconds) because of fingerprinting
        Assert.True(eventTime.TotalSeconds < 2.0, $"Large file created event took {eventTime.TotalSeconds:F2} seconds, expected < 2 seconds");
    }

    [Fact]
    public async Task LargeFile_Deleted_EventFiresQuickly()
    {
        // Deleting a large file (>1GB) should fire quickly
        var options = new FileSystemWatcherOptions { EnableFileHashing = true };
        var largeFilePath = _tempSession.GetFilePath();
        var size = FileSizeUnitInfo.Gigabyte.ConvertToBytes(1.2);

        // Create the file first
        CreateFileOfSize(largeFilePath, size);
        var deletedFired = false;
        var eventTime = TimeSpan.Zero;
        var startTime = DateTime.UtcNow;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options);
        watcher.FileDeleted += (_, e) => {
            if (e.OldPath != largeFilePath)
                return;

            deletedFired = true;
            eventTime = DateTime.UtcNow - startTime;
        };

        // Wait until the initial snapshot finishes
        await Task.Delay(500, TestContext.Current.CancellationToken);
        startTime = DateTime.UtcNow;
        File.Delete(largeFilePath);

        // Wait for the event (should be fast)
        await PollAssert.ThatAsync(() => deletedFired, TimeSpan.FromSeconds(5));
        Assert.True(deletedFired);
        // Event should fire quickly (< 1 second) because deletion does not need hashing
        Assert.True(eventTime.TotalSeconds < 1.0, $"Large file deleted event took {eventTime.TotalSeconds:F2} seconds, expected < 1 second");
    }

    [Fact]
    public async Task LargeFile_Moved_EventFiresQuickly()
    {
        // Moving a large file (>1GB) should fire quickly because of fingerprinting
        var options = new FileSystemWatcherOptions { EnableFileHashing = true };
        var sourcePath = _tempSession.GetFilePath();
        var destPath = _tempSession.GetFilePath();
        var size = FileSizeUnitInfo.Gigabyte.ConvertToBytes(1.2);

        // Create the file first
        CreateFileOfSize(sourcePath, size);
        var movedFired = false;
        var eventTime = TimeSpan.Zero;
        var startTime = DateTime.UtcNow;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options);

        void OnMoveOrRename(object? _, FileSystemChangeInfo e)
        {
            if (e.OldPath != sourcePath || e.NewPath != destPath)
                return;

            movedFired = true;
            eventTime = DateTime.UtcNow - startTime;
        }

        watcher.FileMoved += OnMoveOrRename;
        watcher.FileRenamed += OnMoveOrRename;

        // Wait until the initial snapshot finishes
        await Task.Delay(500, TestContext.Current.CancellationToken);
        startTime = DateTime.UtcNow;
        File.Move(sourcePath, destPath);

        // Wait for the event (FileRenamed in the same dir, FileMoved across dirs — fingerprint-based, no full hash)
        await PollAssert.ThatAsync(() => movedFired, TimeSpan.FromSeconds(15));
        Assert.True(movedFired);
        // Event should fire reasonably quickly (< 5 seconds) — threshold allows for CI/slower storage
        Assert.True(eventTime.TotalSeconds < 5.0, $"Large file moved event took {eventTime.TotalSeconds:F2} seconds, expected < 5 seconds");
    }

    [Fact]
    public async Task LargeFile_Renamed_EventFiresQuickly()
    {
        // Renaming a large file (>1GB) should fire quickly
        var options = new FileSystemWatcherOptions { EnableFileHashing = true };
        var sourcePath = _tempSession.GetFilePath();
        var destPath = _tempSession.GetFilePath();
        var size = FileSizeUnitInfo.Gigabyte.ConvertToBytes(1.2);

        // Create the file first
        CreateFileOfSize(sourcePath, size);
        var renamedFired = false;
        var eventTime = TimeSpan.Zero;
        var startTime = DateTime.UtcNow;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options);
        watcher.FileRenamed += (_, e) => {
            if (e.OldPath != sourcePath || e.NewPath != destPath)
                return;

            renamedFired = true;
            eventTime = DateTime.UtcNow - startTime;
        };

        // Wait until the initial snapshot finishes
        await Task.Delay(500, TestContext.Current.CancellationToken);
        startTime = DateTime.UtcNow;
        File.Move(sourcePath, destPath);

        // Wait for the event (should be fast due to fingerprinting)
        await PollAssert.ThatAsync(() => renamedFired, TimeSpan.FromSeconds(5));
        Assert.True(renamedFired);
        // Event should fire reasonably quickly (< 4 seconds) — threshold allows for CI/slower storage
        Assert.True(eventTime.TotalSeconds < 4.0, $"Large file renamed event took {eventTime.TotalSeconds:F2} seconds, expected < 4 seconds");
    }

    [Fact]
    public async Task LargeFile_Changed_EventFiresQuickly()
    {
        // Changing a large file (>1GB) should fire quickly
        var options = new FileSystemWatcherOptions { EnableFileHashing = true };
        var largeFilePath = _tempSession.GetFilePath();
        var size = FileSizeUnitInfo.Gigabyte.ConvertToBytes(1.2);

        // Create the file first
        CreateFileOfSize(largeFilePath, size);
        var changedFired = false;
        var eventTime = TimeSpan.Zero;
        var startTime = DateTime.UtcNow;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options);
        watcher.FileChanged += (_, e) => {
            if (e.OldPath != largeFilePath || e.NewPath != largeFilePath)
                return;

            changedFired = true;
            eventTime = DateTime.UtcNow - startTime;
        };

        // Wait until the initial snapshot finishes
        await Task.Delay(500, TestContext.Current.CancellationToken);

        // Touch the file so modification time changes
        startTime = DateTime.UtcNow;
        File.SetLastWriteTime(largeFilePath, DateTime.UtcNow);

        // Wait for the event (should be fast)
        await PollAssert.ThatAsync(() => changedFired, TimeSpan.FromSeconds(5));
        Assert.True(changedFired);
        // Event should fire quickly (< 1 second) because a modification-time change is cheap to detect
        Assert.True(eventTime.TotalSeconds < 1.0, $"Large file changed event took {eventTime.TotalSeconds:F2} seconds, expected < 1 second");
    }

    [Fact]
    public async Task MultipleLargeFiles_Created_EventsFireQuickly()
    {
        // Creating several large files should fire events quickly
        var options = new FileSystemWatcherOptions { EnableFileHashing = true };
        var fileCount = 3;
        var size = FileSizeUnitInfo.Gigabyte.ConvertToBytes(1.2);
        var createdCount = 0;
        var startTime = DateTime.UtcNow;
        var lastEventTime = TimeSpan.Zero;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options);
        watcher.FileCreated += (_, _) => {
            createdCount++;
            lastEventTime = DateTime.UtcNow - startTime;
        };

        // Wait until the initial snapshot finishes
        await Task.Delay(500, TestContext.Current.CancellationToken);
        startTime = DateTime.UtcNow;

        // Create several large files
        for (var i = 0; i < fileCount; i++) {
            var filePath = _tempSession.GetFilePath();
            CreateFileOfSize(filePath, size);
        }

        // Wait for every event
        await PollAssert.ThatAsync(() => createdCount >= fileCount, TimeSpan.FromSeconds(10));
        Assert.True(createdCount >= fileCount);
        // All events should fire reasonably quickly (< 10 seconds total for 3 files) — threshold allows for CI/slower storage
        Assert.True(lastEventTime.TotalSeconds < 10.0, $"Multiple large files created events took {lastEventTime.TotalSeconds:F2} seconds, expected < 10 seconds");
    }

    private static void CreateFileOfSize(string path, long sizeBytes)
    {
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
        stream.SetLength(sizeBytes);
    }
}