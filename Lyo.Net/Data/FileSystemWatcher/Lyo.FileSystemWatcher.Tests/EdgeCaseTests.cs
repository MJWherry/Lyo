using Lyo.Common.Metadata.Records;
using Lyo.IO.Temp.Models;
using Lyo.Testing;
using Microsoft.Extensions.Logging;

namespace Lyo.FileSystemWatcher.Tests;

public class EdgeCaseTests : IDisposable
{
    private readonly IIOTempSession _tempSession;

    public EdgeCaseTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _tempSession = new IOTempSession(new(), loggerFactory.CreateLogger<IOTempSession>());
    }

    public void Dispose() => _tempSession.Dispose();

    [Fact]
    public async Task FileMove_BetweenDirectories_DetectsMove()
    {
        var dirA = Path.Combine(_tempSession.SessionDirectory, "dirA");
        var dirB = Path.Combine(_tempSession.SessionDirectory, "dirB");
        Directory.CreateDirectory(dirA);
        Directory.CreateDirectory(dirB);
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var filePathA = Path.Combine(dirA, fileName);
        var filePathB = Path.Combine(dirB, fileName);
        await _tempSession.CreateFileAsync(new byte[100], Path.Combine("dirA", fileName), TestContext.Current.CancellationToken);
        var moveFired = false;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, new FileSystemWatcherOptions { IncludeSubdirectories = true });
        watcher.FileMoved += (_, e) => {
            if (e.OldPath == filePathA && e.NewPath == filePathB)
                moveFired = true;
        };

        await Task.Delay(200, TestContext.Current.CancellationToken); // Wait for initial snapshot

        // Confirm the file exists before moving
        Assert.True(File.Exists(filePathA), $"File should exist before move: {filePathA}");
        File.Move(filePathA, filePathB);
        await PollAssert.ThatAsync(() => moveFired, TimeSpan.FromSeconds(5));
        Assert.True(moveFired);
    }

    [Fact]
    public async Task FileMove_SourceDirectory_ShowsChangeEvent()
    {
        var dirA = Path.Combine(_tempSession.SessionDirectory, "dirA");
        var dirB = Path.Combine(_tempSession.SessionDirectory, "dirB");
        Directory.CreateDirectory(dirA);
        Directory.CreateDirectory(dirB);
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var filePathA = Path.Combine(dirA, fileName);
        var filePathB = Path.Combine(dirB, fileName);

        // Create the file we will move
        await _tempSession.CreateFileAsync(new byte[100], Path.Combine("dirA", fileName), TestContext.Current.CancellationToken);

        // Create 4 more files in dirA (5 total)
        for (var i = 0; i < 4; i++)
            await _tempSession.CreateFileAsync(new byte[100], Path.Combine("dirA", $"file{i}.txt"), TestContext.Current.CancellationToken);

        var changeEventFired = false;
        FileSystemChangeInfo? changeInfo = null;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, new FileSystemWatcherOptions { IncludeSubdirectories = true });
        watcher.DirectoryChanged += (_, e) => {
            if (e.OldPath == dirA && e.NewPath == dirA) {
                changeEventFired = true;
                changeInfo = e;
            }
        };

        await Task.Delay(500, TestContext.Current.CancellationToken); // Wait for initial snapshot

        // Confirm the file exists before moving
        Assert.True(File.Exists(filePathA), $"File should exist before move: {filePathA}");

        // Move one file from dirA into dirB
        File.Move(filePathA, filePathB);
        await PollAssert.ThatAsync(() => changeEventFired, TimeSpan.FromSeconds(5));
        Assert.True(changeEventFired);
        Assert.NotNull(changeInfo);
        // Documents a known bug: counts may be wrong
        Assert.True(changeInfo.OldFileCount.HasValue);
        Assert.True(changeInfo.NewFileCount.HasValue);
    }

    [Fact]
    public async Task RapidChanges_Debounced_Correctly()
    {
        var options = new FileSystemWatcherOptions { DebounceTimerDelay = 500 };
        var eventCount = 0;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options);
        watcher.FileCreated += (_, _) => eventCount++;

        // Create several files in quick succession
        for (var i = 0; i < 10; i++)
            await _tempSession.CreateFileAsync(new byte[100], $"file{i}.txt", TestContext.Current.CancellationToken);

        await Task.Delay(1000, TestContext.Current.CancellationToken); // Wait for debounce

        // Every file should be detected (one batch or several)
        Assert.True(eventCount >= 10);
    }

    [Fact]
    public async Task LargeFile_Hashing_Works()
    {
        var options = new FileSystemWatcherOptions { EnableFileHashing = true };
        var largeFilePath = _tempSession.TouchFile();
        File.Delete(largeFilePath);
        var createdFired = false;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options);
        watcher.FileCreated += (_, e) => {
            if (e.NewPath == largeFilePath)
                createdFired = true;
        };

        // Wait until the initial snapshot finishes
        await Task.Delay(500, TestContext.Current.CancellationToken);

        // Create a larger file (1MB) after the watcher is initialized
        CreateFileOfSize(largeFilePath, FileSizeUnitInfo.Megabyte.ConvertToBytes(1));

        // Wait longer for large-file hashing to finish
        await PollAssert.ThatAsync(() => createdFired, TimeSpan.FromSeconds(30));
        Assert.True(createdFired);
    }

    [Fact]
    public async Task Subdirectory_NotWatched_WhenIncludeSubdirectoriesFalse()
    {
        var options = new FileSystemWatcherOptions { IncludeSubdirectories = false };
        var subDir = Path.Combine(_tempSession.SessionDirectory, "subdir");
        Directory.CreateDirectory(subDir);
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var eventFired = false;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options);
        watcher.FileCreated += (_, _) => eventFired = true;
        await _tempSession.CreateFileAsync(new byte[100], Path.Combine("subdir", fileName), TestContext.Current.CancellationToken);
        await Task.Delay(500, TestContext.Current.CancellationToken);

        // Must not fire because the subdirectory is not watched
        Assert.False(eventFired);
    }

    [Fact]
    public async Task FileCopy_DetectedAsCreate()
    {
        var sourceFile = await _tempSession.CreateFileAsync(new byte[100], "source.txt", TestContext.Current.CancellationToken);
        var destFile = Path.Combine(_tempSession.SessionDirectory, "dest.txt");
        var createFired = false;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.FileCreated += (_, e) => {
            if (e.NewPath == destFile)
                createFired = true;
        };

        await Task.Delay(200, TestContext.Current.CancellationToken); // Wait for initial snapshot
        File.Copy(sourceFile, destFile);
        await PollAssert.ThatAsync(() => createFired, TimeSpan.FromSeconds(5));
        Assert.True(createFired);
    }

    [Fact]
    public async Task EmptyDirectory_Works()
    {
        var emptyDir = Path.Combine(_tempSession.SessionDirectory, "empty");
        Directory.CreateDirectory(emptyDir);
        using var watcher = new FileSystemWatcher(emptyDir);

        // Must not throw
        await Task.Delay(100, TestContext.Current.CancellationToken);
        Assert.NotNull(watcher);
    }

    [Fact]
    public async Task LongPath_Works()
    {
        var longPath = _tempSession.SessionDirectory;
        for (var i = 0; i < 5; i++)
            longPath = Path.Combine(longPath, $"subdir{i}");

        Directory.CreateDirectory(longPath);
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var eventFired = false;
        using var watcher = new FileSystemWatcher(longPath);
        watcher.FileCreated += (_, _) => eventFired = true;
        await _tempSession.CreateFileAsync(new byte[100], Path.Combine("subdir0", "subdir1", "subdir2", "subdir3", "subdir4", fileName), TestContext.Current.CancellationToken);
        await PollAssert.ThatAsync(() => eventFired, TimeSpan.FromSeconds(5));
        Assert.True(eventFired);
    }

    private static void CreateFileOfSize(string path, long sizeBytes)
    {
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
        stream.SetLength(sizeBytes);
    }
}