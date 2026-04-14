using System.Text;
using Lyo.Common.Records;
using Lyo.IO.Temp.Models;
using Lyo.Testing;
using Microsoft.Extensions.Logging;

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
        // Small file (<100MB) should use first + last bytes
        var filePath = _tempSession.GetFilePath();
        var content = "Hello World! This is a test file for fingerprinting.";
        await File.WriteAllTextAsync(filePath, content, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var fileInfo = new FileInfo(filePath);
        var fingerprint = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(fingerprint);
        Assert.NotEmpty(fingerprint);
        Assert.Equal(16, fingerprint.Length); // MD5 hash is 16 bytes

        // Fingerprint should be consistent for same file
        var fingerprint2 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(fingerprint, fingerprint2);
    }

    [Fact]
    public async Task Fingerprint_SmallFile_DetectsEndChange()
    {
        // Test that fingerprint detects changes at the end of small files
        var filePath = _tempSession.GetFilePath();
        await File.WriteAllTextAsync(filePath, "Initial content", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var fileInfo = new FileInfo(filePath);
        var fingerprint1 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Append to end of file
        await File.AppendAllTextAsync(filePath, " - appended", TestContext.Current.CancellationToken).ConfigureAwait(false);
        fileInfo.Refresh();
        var fingerprint2 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Fingerprints should be different (size changed and last bytes changed)
        Assert.NotEqual(fingerprint1, fingerprint2);
    }

    [Fact]
    public async Task Fingerprint_MediumFile_UsesSparseSampling()
    {
        // Medium file (100MB - 1GB) should use beginning + middle + end
        var filePath = _tempSession.GetFilePath();
        var size = FileSizeUnitInfo.Megabyte.ConvertToBytes(150);
        CreateFileOfSize(filePath, size);
        var fileInfo = new FileInfo(filePath);
        var fingerprint = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(fingerprint);
        Assert.NotEmpty(fingerprint);
        Assert.Equal(16, fingerprint.Length); // MD5 hash is 16 bytes

        // Fingerprint should be consistent
        var fingerprint2 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(fingerprint, fingerprint2);
    }

    [Fact]
    public async Task Fingerprint_MediumFile_DetectsEndChange()
    {
        // Test that fingerprint detects changes at the end of medium files
        var filePath = _tempSession.GetFilePath();
        var size = FileSizeUnitInfo.Megabyte.ConvertToBytes(150);
        CreateFileOfSize(filePath, size);
        var fileInfo = new FileInfo(filePath);
        var fingerprint1 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Modify end of file
        await using (var stream = File.OpenWrite(filePath)) {
            stream.Seek(-100, SeekOrigin.End);
            var bytes = Encoding.UTF8.GetBytes("MODIFIED");
            stream.Write(bytes, 0, bytes.Length);
        }

        fileInfo.Refresh();
        var fingerprint2 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Fingerprints should be different (last bytes changed)
        Assert.NotEqual(fingerprint1, fingerprint2);
    }

    [Fact]
    public async Task Fingerprint_LargeFile_UsesSparseSamplingWithModTime()
    {
        // Very large file (>1GB) should use beginning + middle + modification time
        var filePath = _tempSession.GetFilePath();
        var size = FileSizeUnitInfo.Gigabyte.ConvertToBytes(1.2);
        CreateFileOfSize(filePath, size);
        var fileInfo = new FileInfo(filePath);
        var fingerprint = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(fingerprint);
        Assert.NotEmpty(fingerprint);
        Assert.Equal(16, fingerprint.Length); // MD5 hash is 16 bytes

        // Fingerprint should be consistent
        var fingerprint2 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(fingerprint, fingerprint2);
    }

    [Fact]
    public async Task Fingerprint_LargeFile_DetectsModificationTimeChange()
    {
        // Test that fingerprint detects modification time changes for very large files
        var filePath = _tempSession.GetFilePath();
        var size = FileSizeUnitInfo.Gigabyte.ConvertToBytes(1.2);
        CreateFileOfSize(filePath, size);
        var fileInfo = new FileInfo(filePath);
        var fingerprint1 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Touch the file to update modification time
        File.SetLastWriteTime(filePath, DateTime.UtcNow.AddSeconds(1));
        fileInfo.Refresh();
        var fingerprint2 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Fingerprints should be different (modification time changed)
        Assert.NotEqual(fingerprint1, fingerprint2);
    }

    [Fact]
    public async Task Fingerprint_DifferentSizes_DifferentFingerprints()
    {
        // Files with different sizes should have different fingerprints
        var file1 = _tempSession.GetFilePath();
        var file2 = _tempSession.GetFilePath();
        await File.WriteAllTextAsync(file1, "Short", TestContext.Current.CancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(file2, "Much longer content here", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var info1 = new FileInfo(file1);
        var info2 = new FileInfo(file2);
        var fingerprint1 = await Utilities.Fingerprint(file1, info1.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var fingerprint2 = await Utilities.Fingerprint(file2, info2.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotEqual(fingerprint1, fingerprint2);
    }

    [Fact]
    public async Task Fingerprint_SameContent_SameFingerprint()
    {
        // Files with same content should have same fingerprint (if same size)
        var file1 = _tempSession.GetFilePath();
        var file2 = _tempSession.GetFilePath();
        var content = "Identical content for both files";
        await File.WriteAllTextAsync(file1, content, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(file2, content, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var info1 = new FileInfo(file1);
        var info2 = new FileInfo(file2);
        var fingerprint1 = await Utilities.Fingerprint(file1, info1.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var fingerprint2 = await Utilities.Fingerprint(file2, info2.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(fingerprint1, fingerprint2);
    }

    [Fact]
    public async Task Fingerprint_EmptyFile_Works()
    {
        // Empty file should still produce a fingerprint
        var filePath = _tempSession.GetFilePath();
        await File.Create(filePath).DisposeAsync().ConfigureAwait(false);
        var fileInfo = new FileInfo(filePath);
        var fingerprint = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(fingerprint);
        Assert.NotEmpty(fingerprint);
        Assert.Equal(16, fingerprint.Length);
    }

    [Fact]
    public async Task Fingerprint_TextFile_DetectsEndAppend()
    {
        // Test that appending to a text file changes fingerprint
        var filePath = _tempSession.GetFilePath();
        await File.WriteAllTextAsync(filePath, "Line 1\nLine 2\n", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var fileInfo = new FileInfo(filePath);
        var fingerprint1 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Append new line
        await File.AppendAllTextAsync(filePath, "Line 3\n", TestContext.Current.CancellationToken).ConfigureAwait(false);
        fileInfo.Refresh();
        var fingerprint2 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Fingerprints should be different
        Assert.NotEqual(fingerprint1, fingerprint2);
    }

    [Fact]
    public async Task Fingerprint_CSVFile_DetectsEndChange()
    {
        // Test CSV file end change detection
        var filePath = _tempSession.GetFilePath();
        await File.WriteAllTextAsync(filePath, "Name,Age,City\nJohn,30,NYC\nJane,25,LA\n", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var fileInfo = new FileInfo(filePath);
        var fingerprint1 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Add new row
        await File.AppendAllTextAsync(filePath, "Bob,35,Chicago\n", TestContext.Current.CancellationToken).ConfigureAwait(false);
        fileInfo.Refresh();
        var fingerprint2 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Fingerprints should be different
        Assert.NotEqual(fingerprint1, fingerprint2);
    }

    [Fact]
    public async Task Fingerprint_MediumFile_DetectsMiddleChange()
    {
        // Test that middle changes are detected for medium files
        var filePath = _tempSession.GetFilePath();
        var size = FileSizeUnitInfo.Megabyte.ConvertToBytes(150);
        CreateFileOfSize(filePath, size);
        var fileInfo = new FileInfo(filePath);
        var fingerprint1 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Modify middle of file
        await using (var stream = File.OpenWrite(filePath)) {
            stream.Seek(size / 2, SeekOrigin.Begin);
            var bytes = Encoding.UTF8.GetBytes("MODIFIED_MIDDLE");
            stream.Write(bytes, 0, bytes.Length);
        }

        fileInfo.Refresh();
        var fingerprint2 = await Utilities.Fingerprint(filePath, fileInfo.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Fingerprints should be different (middle bytes changed)
        Assert.NotEqual(fingerprint1, fingerprint2);
    }

    [Fact]
    public async Task Fingerprint_NonExistentFile_ReturnsNull()
    {
        var nonExistentPath = _tempSession.GetFilePath();
        File.Delete(nonExistentPath);
        var fingerprint = await Utilities.Fingerprint(nonExistentPath, 100, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Null(fingerprint);
    }

    [Fact]
    public async Task Fingerprint_ThresholdBoundaries_WorkCorrectly()
    {
        // Test files at threshold boundaries
        var smallFile = _tempSession.GetFilePath();
        var mediumFile = _tempSession.GetFilePath();
        var largeFile = _tempSession.GetFilePath();

        // Just under 100MB
        CreateFileOfSize(smallFile, FileSizeUnitInfo.Megabyte.ConvertToBytes(99.9));
        // Just over 100MB
        CreateFileOfSize(mediumFile, FileSizeUnitInfo.Megabyte.ConvertToBytes(100.1));
        // Just over 1GB
        CreateFileOfSize(largeFile, FileSizeUnitInfo.Gigabyte.ConvertToBytes(1));
        var smallInfo = new FileInfo(smallFile);
        var mediumInfo = new FileInfo(mediumFile);
        var largeInfo = new FileInfo(largeFile);
        var smallFp = await Utilities.Fingerprint(smallFile, smallInfo.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var mediumFp = await Utilities.Fingerprint(mediumFile, mediumInfo.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var largeFp = await Utilities.Fingerprint(largeFile, largeInfo.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(smallFp);
        Assert.NotNull(mediumFp);
        Assert.NotNull(largeFp);
        Assert.NotEmpty(smallFp);
        Assert.NotEmpty(mediumFp);
        Assert.NotEmpty(largeFp);

        // All should be different
        Assert.NotEqual(smallFp, mediumFp);
        Assert.NotEqual(mediumFp, largeFp);
        Assert.NotEqual(smallFp, largeFp);
    }

    [Fact]
    public async Task LargeFile_Created_EventFiresQuickly()
    {
        // Test that creating a large file (>1GB) fires event quickly due to fingerprinting optimization
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

        // Wait for initial snapshot to complete
        await Task.Delay(500, TestContext.Current.CancellationToken).ConfigureAwait(false);
        startTime = DateTime.UtcNow;
        CreateFileOfSize(largeFilePath, size);

        // Wait for event with reasonable timeout (should be fast due to fingerprinting)
        await PollAssert.ThatAsync(() => createdFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.True(createdFired);
        // Event should fire quickly (< 2 seconds) due to fingerprinting optimization
        Assert.True(eventTime.TotalSeconds < 2.0, $"Large file created event took {eventTime.TotalSeconds:F2} seconds, expected < 2 seconds");
    }

    [Fact]
    public async Task LargeFile_Deleted_EventFiresQuickly()
    {
        // Test that deleting a large file (>1GB) fires event quickly
        var options = new FileSystemWatcherOptions { EnableFileHashing = true };
        var largeFilePath = _tempSession.GetFilePath();
        var size = FileSizeUnitInfo.Gigabyte.ConvertToBytes(1.2);

        // Create file first
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

        // Wait for initial snapshot to complete
        await Task.Delay(500, TestContext.Current.CancellationToken).ConfigureAwait(false);
        startTime = DateTime.UtcNow;
        File.Delete(largeFilePath);

        // Wait for event (should be fast)
        await PollAssert.ThatAsync(() => deletedFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.True(deletedFired);
        // Event should fire quickly (< 1 second) as deletion doesn't require hashing
        Assert.True(eventTime.TotalSeconds < 1.0, $"Large file deleted event took {eventTime.TotalSeconds:F2} seconds, expected < 1 second");
    }

    [Fact]
    public async Task LargeFile_Moved_EventFiresQuickly()
    {
        // Test that moving a large file (>1GB) fires event quickly due to fingerprinting optimization
        var options = new FileSystemWatcherOptions { EnableFileHashing = true };
        var sourcePath = _tempSession.GetFilePath();
        var destPath = _tempSession.GetFilePath();
        var size = FileSizeUnitInfo.Gigabyte.ConvertToBytes(1.2);

        // Create file first
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

        // Wait for initial snapshot to complete
        await Task.Delay(500, TestContext.Current.CancellationToken).ConfigureAwait(false);
        startTime = DateTime.UtcNow;
        File.Move(sourcePath, destPath);

        // Wait for event (FileRenamed when same dir, FileMoved when different dir - fingerprint-based, no full hash)
        await PollAssert.ThatAsync(() => movedFired, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
        Assert.True(movedFired);
        // Event should fire reasonably quickly (< 5 seconds) - threshold allows for CI/slower storage
        Assert.True(eventTime.TotalSeconds < 5.0, $"Large file moved event took {eventTime.TotalSeconds:F2} seconds, expected < 5 seconds");
    }

    [Fact]
    public async Task LargeFile_Renamed_EventFiresQuickly()
    {
        // Test that renaming a large file (>1GB) fires event quickly
        var options = new FileSystemWatcherOptions { EnableFileHashing = true };
        var sourcePath = _tempSession.GetFilePath();
        var destPath = _tempSession.GetFilePath();
        var size = FileSizeUnitInfo.Gigabyte.ConvertToBytes(1.2);

        // Create file first
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

        // Wait for initial snapshot to complete
        await Task.Delay(500, TestContext.Current.CancellationToken).ConfigureAwait(false);
        startTime = DateTime.UtcNow;
        File.Move(sourcePath, destPath);

        // Wait for event (should be fast due to fingerprinting)
        await PollAssert.ThatAsync(() => renamedFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.True(renamedFired);
        // Event should fire reasonably quickly (< 4 seconds) - threshold allows for CI/slower storage
        Assert.True(eventTime.TotalSeconds < 4.0, $"Large file renamed event took {eventTime.TotalSeconds:F2} seconds, expected < 4 seconds");
    }

    [Fact]
    public async Task LargeFile_Changed_EventFiresQuickly()
    {
        // Test that changing a large file (>1GB) fires event quickly
        var options = new FileSystemWatcherOptions { EnableFileHashing = true };
        var largeFilePath = _tempSession.GetFilePath();
        var size = FileSizeUnitInfo.Gigabyte.ConvertToBytes(1.2);

        // Create file first
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

        // Wait for initial snapshot to complete
        await Task.Delay(500, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Modify the file (touch it to change modification time)
        startTime = DateTime.UtcNow;
        File.SetLastWriteTime(largeFilePath, DateTime.UtcNow);

        // Wait for event (should be fast)
        await PollAssert.ThatAsync(() => changedFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.True(changedFired);
        // Event should fire quickly (< 1 second) as modification time change is fast to detect
        Assert.True(eventTime.TotalSeconds < 1.0, $"Large file changed event took {eventTime.TotalSeconds:F2} seconds, expected < 1 second");
    }

    [Fact]
    public async Task MultipleLargeFiles_Created_EventsFireQuickly()
    {
        // Test that creating multiple large files fires events quickly
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

        // Wait for initial snapshot to complete
        await Task.Delay(500, TestContext.Current.CancellationToken).ConfigureAwait(false);
        startTime = DateTime.UtcNow;

        // Create multiple large files
        for (var i = 0; i < fileCount; i++) {
            var filePath = _tempSession.GetFilePath();
            CreateFileOfSize(filePath, size);
        }

        // Wait for all events
        await PollAssert.ThatAsync(() => createdCount >= fileCount, TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        Assert.True(createdCount >= fileCount);
        // All events should fire reasonably quickly (< 10 seconds total for 3 files) - threshold allows for CI/slower storage
        Assert.True(lastEventTime.TotalSeconds < 10.0, $"Multiple large files created events took {lastEventTime.TotalSeconds:F2} seconds, expected < 10 seconds");
    }

    private static void CreateFileOfSize(string path, long sizeBytes)
    {
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
        stream.SetLength(sizeBytes);
    }
}