using Lyo.IO.Temp.Models;
using Lyo.Testing;
using Microsoft.Extensions.Logging;

namespace Lyo.FileSystemWatcher.Tests;

public class SubdirectoryTests : IDisposable
{
    private readonly IIOTempSession _tempSession;

    public SubdirectoryTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _tempSession = new IOTempSession(new(), loggerFactory.CreateLogger<IOTempSession>());
    }

    public void Dispose() => _tempSession.Dispose();

    [Fact]
    public async Task Subdirectory_FileCreated_WhenIncludeSubdirectoriesTrue()
    {
        var options = new FileSystemWatcherOptions { IncludeSubdirectories = true };
        var subDir = Path.Combine(_tempSession.SessionDirectory, "subdir");
        Directory.CreateDirectory(subDir);
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var filePath = Path.Combine(subDir, fileName);
        var eventFired = false;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options);
        watcher.FileCreated += (_, e) => {
            if (e.NewPath?.Contains("subdir") == true && e.NewPath.EndsWith(fileName))
                eventFired = true;
        };

        await _tempSession.CreateFileAsync(new byte[100], Path.Combine("subdir", fileName), TestContext.Current.CancellationToken).ConfigureAwait(false);
        await PollAssert.ThatAsync(() => eventFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.True(eventFired);
    }

    [Fact]
    public async Task Subdirectory_FileDeleted_WhenIncludeSubdirectoriesTrue()
    {
        var options = new FileSystemWatcherOptions { IncludeSubdirectories = true };
        var subDir = Path.Combine(_tempSession.SessionDirectory, "subdir");
        Directory.CreateDirectory(subDir);
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var filePath = Path.Combine(subDir, fileName);
        await _tempSession.CreateFileAsync(new byte[100], Path.Combine("subdir", fileName), TestContext.Current.CancellationToken).ConfigureAwait(false);
        var eventFired = false;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options);
        await Task.Delay(200, TestContext.Current.CancellationToken).ConfigureAwait(false); // Wait for initial snapshot
        watcher.FileDeleted += (_, e) => {
            if (e.OldPath == filePath)
                eventFired = true;
        };

        File.Delete(filePath);
        await PollAssert.ThatAsync(() => eventFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.True(eventFired);
    }

    [Fact]
    public async Task NestedSubdirectory_Works()
    {
        var options = new FileSystemWatcherOptions { IncludeSubdirectories = true };
        var subDir1 = Path.Combine(_tempSession.SessionDirectory, "subdir1");
        var subDir2 = Path.Combine(subDir1, "subdir2");
        Directory.CreateDirectory(subDir2);
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var filePath = Path.Combine(subDir2, fileName);
        var eventFired = false;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options);
        watcher.FileCreated += (_, e) => {
            if (e.NewPath?.Contains("subdir2") == true && e.NewPath.EndsWith(fileName))
                eventFired = true;
        };

        await _tempSession.CreateFileAsync(new byte[100], Path.Combine("subdir1", "subdir2", fileName), TestContext.Current.CancellationToken).ConfigureAwait(false);
        await PollAssert.ThatAsync(() => eventFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.True(eventFired);
    }

    [Fact]
    public async Task Subdirectory_DirectoryCreated_WhenIncludeSubdirectoriesTrue()
    {
        var options = new FileSystemWatcherOptions { IncludeSubdirectories = true };
        var subDir = Path.Combine(_tempSession.SessionDirectory, "subdir");
        var nestedDir = Path.Combine(subDir, "nested");
        var eventFired = false;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options);
        Directory.CreateDirectory(subDir);
        await Task.Delay(200, TestContext.Current.CancellationToken).ConfigureAwait(false); // Wait for subdir to be detected
        watcher.DirectoryCreated += (_, e) => {
            if (e.NewPath == nestedDir)
                eventFired = true;
        };

        Directory.CreateDirectory(nestedDir);
        await PollAssert.ThatAsync(() => eventFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.True(eventFired);
    }

    [Fact]
    public async Task Subdirectory_FileMoved_Detected()
    {
        var options = new FileSystemWatcherOptions { IncludeSubdirectories = true };
        var subDir1 = Path.Combine(_tempSession.SessionDirectory, "subdir1");
        var subDir2 = Path.Combine(_tempSession.SessionDirectory, "subdir2");
        Directory.CreateDirectory(subDir1);
        Directory.CreateDirectory(subDir2);
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var sourcePath = Path.Combine(subDir1, fileName);
        var destPath = Path.Combine(subDir2, fileName);
        await _tempSession.CreateFileAsync(new byte[100], Path.Combine("subdir1", fileName), TestContext.Current.CancellationToken).ConfigureAwait(false);
        var moveFired = false;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options);
        await Task.Delay(200, TestContext.Current.CancellationToken).ConfigureAwait(false); // Wait for initial snapshot
        watcher.FileMoved += (_, e) => {
            if (e.OldPath == sourcePath && e.NewPath == destPath)
                moveFired = true;
        };

        File.Move(sourcePath, destPath);
        await PollAssert.ThatAsync(() => moveFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.True(moveFired);
    }
}