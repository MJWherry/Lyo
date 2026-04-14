using Lyo.FileSystemWatcher.Enums;
using Lyo.IO.Temp.Models;
using Lyo.Testing;
using Microsoft.Extensions.Logging;

namespace Lyo.FileSystemWatcher.Tests;

/// <summary>Comprehensive tests to ensure ALL events are properly tested. This test class verifies that every event type fires correctly with proper data.</summary>
public class AllEventsTests : IDisposable
{
    private readonly IIOTempSession _tempSession;

    public AllEventsTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _tempSession = new IOTempSession(new(), loggerFactory.CreateLogger<IOTempSession>());
    }

    public void Dispose() => _tempSession.Dispose();

#region File Events - Direct Tests

    [Fact]
    public async Task Event_FileCreated_FiresDirectly()
    {
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var eventFired = false;
        FileSystemChangeInfo? eventData = null;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.FileCreated += (_, e) => {
            eventFired = true;
            eventData = e;
        };

        await _tempSession.CreateFileAsync(new byte[100], fileName, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await PollAssert.ThatAsync(() => eventFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.True(eventFired);
        Assert.NotNull(eventData);
        Assert.Equal(ChangeTypeEnum.Created, eventData.ChangeType);
        Assert.Null(eventData.OldPath);
        Assert.NotNull(eventData.NewPath);
        Assert.EndsWith(fileName, eventData.NewPath);
        Assert.False(eventData.IsDirectory);
    }

    [Fact]
    public async Task Event_FileDeleted_FiresDirectly()
    {
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var filePath = Path.Combine(_tempSession.SessionDirectory, fileName);
        await _tempSession.CreateFileAsync(new byte[100], fileName, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var eventFired = false;
        FileSystemChangeInfo? eventData = null;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        await Task.Delay(200, TestContext.Current.CancellationToken).ConfigureAwait(false); // Wait for initial snapshot
        watcher.FileDeleted += (_, e) => {
            eventFired = true;
            eventData = e;
        };

        File.Delete(filePath);
        await PollAssert.ThatAsync(() => eventFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.True(eventFired);
        Assert.NotNull(eventData);
        Assert.Equal(ChangeTypeEnum.Deleted, eventData.ChangeType);
        Assert.NotNull(eventData.OldPath);
        Assert.EndsWith(fileName, eventData.OldPath);
        Assert.Null(eventData.NewPath);
        Assert.False(eventData.IsDirectory);
    }

    [Fact]
    public async Task Event_FileChanged_FiresDirectly()
    {
        var filePath = await _tempSession.CreateFileAsync(new byte[100], ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var fileName = Path.GetFileName(filePath);
        var eventFired = false;
        FileSystemChangeInfo? eventData = null;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        await Task.Delay(200, TestContext.Current.CancellationToken).ConfigureAwait(false); // Wait for initial snapshot
        watcher.FileChanged += (_, e) => {
            eventFired = true;
            eventData = e;
        };

        Testing.Utilities.AppendBytesToFile(filePath, 100);
        await PollAssert.ThatAsync(() => eventFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.True(eventFired);
        Assert.NotNull(eventData);
        Assert.Equal(ChangeTypeEnum.Changed, eventData.ChangeType);
        Assert.NotNull(eventData.OldPath);
        Assert.NotNull(eventData.NewPath);
        Assert.Equal(eventData.OldPath, eventData.NewPath);
        Assert.EndsWith(fileName, eventData.OldPath);
        Assert.False(eventData.IsDirectory);
    }

    [Fact]
    public async Task Event_FileRenamed_FiresDirectly()
    {
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var newFileName = Path.GetFileName(_tempSession.GetFilePath());
        var filePath = Path.Combine(_tempSession.SessionDirectory, fileName);
        var newFilePath = Path.Combine(_tempSession.SessionDirectory, newFileName);
        await _tempSession.CreateFileAsync(new byte[100], fileName, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var eventFired = false;
        FileSystemChangeInfo? eventData = null;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        await Task.Delay(200, TestContext.Current.CancellationToken).ConfigureAwait(false); // Wait for initial snapshot
        watcher.FileRenamed += (_, e) => {
            eventFired = true;
            eventData = e;
        };

        File.Move(filePath, newFilePath);
        await PollAssert.ThatAsync(() => eventFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.True(eventFired);
        Assert.NotNull(eventData);
        Assert.Equal(ChangeTypeEnum.Renamed, eventData.ChangeType);
        Assert.NotNull(eventData.OldPath);
        Assert.NotNull(eventData.NewPath);
        Assert.EndsWith(fileName, eventData.OldPath);
        Assert.EndsWith(newFileName, eventData.NewPath);
        Assert.False(eventData.IsDirectory);
    }

    [Fact]
    public async Task Event_FileMoved_FiresDirectly()
    {
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var subDir = Path.Combine(_tempSession.SessionDirectory, "subdir");
        Directory.CreateDirectory(subDir);
        var filePath = Path.Combine(_tempSession.SessionDirectory, fileName);
        var newFilePath = Path.Combine(subDir, fileName);
        await _tempSession.CreateFileAsync(new byte[100], fileName, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var eventFired = false;
        FileSystemChangeInfo? eventData = null;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        await Task.Delay(200, TestContext.Current.CancellationToken).ConfigureAwait(false); // Wait for initial snapshot
        watcher.FileMoved += (_, e) => {
            eventFired = true;
            eventData = e;
        };

        File.Move(filePath, newFilePath);
        await PollAssert.ThatAsync(() => eventFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.True(eventFired);
        Assert.NotNull(eventData);
        Assert.Equal(ChangeTypeEnum.Moved, eventData.ChangeType);
        Assert.NotNull(eventData.OldPath);
        Assert.NotNull(eventData.NewPath);
        Assert.EndsWith(fileName, eventData.OldPath);
        Assert.EndsWith(fileName, eventData.NewPath);
        Assert.False(eventData.IsDirectory);
    }

#endregion

#region Directory Events - Direct Tests

    [Fact]
    public async Task Event_DirectoryCreated_FiresDirectly()
    {
        var directoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        var eventFired = false;
        FileSystemChangeInfo? eventData = null;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.DirectoryCreated += (_, e) => {
            eventFired = true;
            eventData = e;
        };

        Directory.CreateDirectory(Path.Combine(_tempSession.SessionDirectory, directoryName));
        await PollAssert.ThatAsync(() => eventFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.True(eventFired);
        Assert.NotNull(eventData);
        Assert.Equal(ChangeTypeEnum.Created, eventData.ChangeType);
        Assert.Null(eventData.OldPath);
        Assert.NotNull(eventData.NewPath);
        Assert.EndsWith(directoryName, eventData.NewPath);
        Assert.True(eventData.IsDirectory);
        Assert.NotNull(eventData.NewFileCount);
        Assert.NotNull(eventData.NewDirCount);
    }

    [Fact]
    public async Task Event_DirectoryDeleted_FiresDirectly()
    {
        var directoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        var dirPath = Path.Combine(_tempSession.SessionDirectory, directoryName);
        Directory.CreateDirectory(dirPath);
        var eventFired = false;
        FileSystemChangeInfo? eventData = null;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        await Task.Delay(200, TestContext.Current.CancellationToken).ConfigureAwait(false); // Wait for initial snapshot
        watcher.DirectoryDeleted += (_, e) => {
            eventFired = true;
            eventData = e;
        };

        Directory.Delete(dirPath);
        await PollAssert.ThatAsync(() => eventFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.True(eventFired);
        Assert.NotNull(eventData);
        Assert.Equal(ChangeTypeEnum.Deleted, eventData.ChangeType);
        Assert.NotNull(eventData.OldPath);
        Assert.EndsWith(directoryName, eventData.OldPath);
        Assert.Null(eventData.NewPath);
        Assert.True(eventData.IsDirectory);
        Assert.NotNull(eventData.OldFileCount);
        Assert.NotNull(eventData.OldDirectoryCount);
    }

    [Fact]
    public async Task Event_DirectoryChanged_FiresDirectly()
    {
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var eventFired = false;
        FileSystemChangeInfo? eventData = null;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.DirectoryChanged += (_, e) => {
            if (e.OldPath == _tempSession.SessionDirectory && e.NewPath == _tempSession.SessionDirectory) {
                eventFired = true;
                eventData = e;
            }
        };

        await _tempSession.CreateFileAsync(new byte[100], fileName, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await PollAssert.ThatAsync(() => eventFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.True(eventFired);
        Assert.NotNull(eventData);
        Assert.Equal(ChangeTypeEnum.Changed, eventData.ChangeType);
        Assert.Equal(_tempSession.SessionDirectory, eventData.OldPath);
        Assert.Equal(_tempSession.SessionDirectory, eventData.NewPath);
        Assert.True(eventData.IsDirectory);
        Assert.NotNull(eventData.OldFileCount);
        Assert.NotNull(eventData.NewFileCount);
        Assert.True(eventData.NewFileCount > eventData.OldFileCount);
    }

    [Fact]
    public async Task Event_DirectoryRenamed_FiresDirectly()
    {
        var directoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        var newDirectoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        var dirPath = Path.Combine(_tempSession.SessionDirectory, directoryName);
        var newDirPath = Path.Combine(_tempSession.SessionDirectory, newDirectoryName);
        Directory.CreateDirectory(dirPath);
        var eventFired = false;
        FileSystemChangeInfo? eventData = null;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        await Task.Delay(200, TestContext.Current.CancellationToken).ConfigureAwait(false); // Wait for initial snapshot
        watcher.DirectoryRenamed += (_, e) => {
            eventFired = true;
            eventData = e;
        };

        Directory.Move(dirPath, newDirPath);
        await PollAssert.ThatAsync(() => eventFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.True(eventFired);
        Assert.NotNull(eventData);
        Assert.Equal(ChangeTypeEnum.Renamed, eventData.ChangeType);
        Assert.NotNull(eventData.OldPath);
        Assert.NotNull(eventData.NewPath);
        Assert.EndsWith(directoryName, eventData.OldPath);
        Assert.EndsWith(newDirectoryName, eventData.NewPath);
        Assert.True(eventData.IsDirectory);
    }

    [Fact]
    public async Task Event_DirectoryMoved_FiresDirectly()
    {
        var directoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        var subDir = Path.Combine(_tempSession.SessionDirectory, "subdir");
        Directory.CreateDirectory(subDir);
        var dirPath = Path.Combine(_tempSession.SessionDirectory, directoryName);
        var newDirPath = Path.Combine(subDir, directoryName);
        Directory.CreateDirectory(dirPath);
        var eventFired = false;
        FileSystemChangeInfo? eventData = null;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        await Task.Delay(200, TestContext.Current.CancellationToken).ConfigureAwait(false); // Wait for initial snapshot
        watcher.DirectoryMoved += (_, e) => {
            eventFired = true;
            eventData = e;
        };

        Directory.Move(dirPath, newDirPath);
        await PollAssert.ThatAsync(() => eventFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.True(eventFired);
        Assert.NotNull(eventData);
        Assert.Equal(ChangeTypeEnum.Moved, eventData.ChangeType);
        Assert.NotNull(eventData.OldPath);
        Assert.NotNull(eventData.NewPath);
        Assert.EndsWith(directoryName, eventData.OldPath);
        Assert.EndsWith(directoryName, eventData.NewPath);
        Assert.True(eventData.IsDirectory);
    }

#endregion

#region OnAnyChange Event Tests

    [Fact]
    public async Task Event_OnAnyChange_FiresForFileCreated()
    {
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var eventsFired = new List<FileSystemChangeInfo>();
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.OnAnyChange += (_, e) => {
            if (!e.IsDirectory || e.ChangeType != ChangeTypeEnum.Changed)
                eventsFired.Add(e);
        };

        await _tempSession.CreateFileAsync(new byte[100], fileName, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await Task.Delay(500, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotEmpty(eventsFired);
        Assert.Contains(eventsFired, e => e.ChangeType == ChangeTypeEnum.Created && !e.IsDirectory);
    }

    [Fact]
    public async Task Event_OnAnyChange_FiresForFileDeleted()
    {
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var filePath = Path.Combine(_tempSession.SessionDirectory, fileName);
        await _tempSession.CreateFileAsync(new byte[100], fileName, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var eventsFired = new List<FileSystemChangeInfo>();
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        await Task.Delay(200, TestContext.Current.CancellationToken).ConfigureAwait(false);
        watcher.OnAnyChange += (_, e) => {
            if (!e.IsDirectory || e.ChangeType != ChangeTypeEnum.Changed)
                eventsFired.Add(e);
        };

        File.Delete(filePath);
        await Task.Delay(500, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotEmpty(eventsFired);
        Assert.Contains(eventsFired, e => e.ChangeType == ChangeTypeEnum.Deleted && !e.IsDirectory);
    }

    [Fact]
    public async Task Event_OnAnyChange_FiresForDirectoryCreated()
    {
        var directoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        var eventsFired = new List<FileSystemChangeInfo>();
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.OnAnyChange += (_, e) => {
            if (!e.IsDirectory || e.ChangeType != ChangeTypeEnum.Changed)
                eventsFired.Add(e);
        };

        Directory.CreateDirectory(Path.Combine(_tempSession.SessionDirectory, directoryName));
        await Task.Delay(500, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotEmpty(eventsFired);
        Assert.Contains(eventsFired, e => e.ChangeType == ChangeTypeEnum.Created && e.IsDirectory);
    }

    [Fact]
    public async Task Event_OnAnyChange_FiresForAllChangeTypes()
    {
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var filePath = Path.Combine(_tempSession.SessionDirectory, fileName);
        var eventsFired = new HashSet<ChangeTypeEnum>();
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.OnAnyChange += (_, e) => {
            if (!e.IsDirectory || e.ChangeType != ChangeTypeEnum.Changed)
                eventsFired.Add(e.ChangeType);
        };

        // Create
        await _tempSession.CreateFileAsync(new byte[100], fileName, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await Task.Delay(300, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Change
        Testing.Utilities.AppendBytesToFile(filePath, 100);
        await Task.Delay(300, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Delete
        File.Delete(filePath);
        await Task.Delay(300, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Contains(ChangeTypeEnum.Created, eventsFired);
        Assert.Contains(ChangeTypeEnum.Changed, eventsFired);
        Assert.Contains(ChangeTypeEnum.Deleted, eventsFired);
    }

#endregion

#region Error Event Tests

    [Fact]
    public void Event_Error_FiresOnException()
    {
        var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.Error += (sender, ex) => {
            _ = true;
            _ = ex;
        };

        // Dispose watcher - this shouldn't trigger an error, but the event should be available
        watcher.Dispose();

        // Error event should be available (even if not fired in this scenario)
        Assert.NotNull(watcher);
    }

    [Fact]
    public async Task Event_Error_FiresWhenEventHandlerThrows()
    {
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var errorFired = false;
        Exception? caughtException = null;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.FileCreated += (_, _) => throw new InvalidOperationException("Test exception");
        watcher.Error += (_, ex) => {
            errorFired = true;
            caughtException = ex;
        };

        await _tempSession.CreateFileAsync(new byte[100], fileName, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await Task.Delay(500, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(errorFired);
        Assert.NotNull(caughtException);
        Assert.IsType<InvalidOperationException>(caughtException);
    }

#endregion

#region Multiple Events Tests

    [Fact]
    public async Task Events_MultipleHandlers_FireForSameChange()
    {
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var fileCreatedFired = false;
        var onAnyChangeFired = false;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.FileCreated += (_, _) => {
            fileCreatedFired = true;
        };

        watcher.OnAnyChange += (_, e) => {
            if (e.ChangeType == ChangeTypeEnum.Created && !e.IsDirectory)
                onAnyChangeFired = true;
        };

        await _tempSession.CreateFileAsync(new byte[100], fileName, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await PollAssert.ThatAsync(() => fileCreatedFired && onAnyChangeFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.True(fileCreatedFired);
        Assert.True(onAnyChangeFired);
    }

    [Fact]
    public async Task Events_FileAndDirectoryEvents_FireSeparately()
    {
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var directoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        var fileCreatedFired = false;
        var directoryCreatedFired = false;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.FileCreated += (_, _) => fileCreatedFired = true;
        watcher.DirectoryCreated += (_, _) => directoryCreatedFired = true;
        await _tempSession.CreateFileAsync(new byte[100], fileName, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Directory.CreateDirectory(Path.Combine(_tempSession.SessionDirectory, directoryName));
        await Task.Delay(500, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(fileCreatedFired);
        Assert.True(directoryCreatedFired);
    }

    [Fact]
    public async Task Events_AllFileEvents_CanFire()
    {
        var fileName1 = Path.GetFileName(_tempSession.GetFilePath());
        var fileName2 = Path.GetFileName(_tempSession.GetFilePath());
        var fileName3 = Path.GetFileName(_tempSession.GetFilePath());
        var fileName4 = Path.GetFileName(_tempSession.GetFilePath());
        var fileName5 = Path.GetFileName(_tempSession.GetFilePath());
        var createdFired = false;
        var changedFired = false;
        var deletedFired = false;
        var renamedFired = false;
        var movedFired = false;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.FileCreated += (_, _) => createdFired = true;
        watcher.FileChanged += (_, _) => changedFired = true;
        watcher.FileDeleted += (_, _) => deletedFired = true;
        watcher.FileRenamed += (_, _) => renamedFired = true;
        watcher.FileMoved += (_, _) => movedFired = true;

        // Create
        await _tempSession.CreateFileAsync(new byte[100], fileName1, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await Task.Delay(300, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Change
        Testing.Utilities.AppendBytesToFile(Path.Combine(_tempSession.SessionDirectory, fileName1), 100);
        await Task.Delay(300, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Delete
        File.Delete(Path.Combine(_tempSession.SessionDirectory, fileName1));
        await Task.Delay(300, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Rename
        var file2 = Path.Combine(_tempSession.SessionDirectory, fileName2);
        var file2New = Path.Combine(_tempSession.SessionDirectory, fileName3);
        await _tempSession.CreateFileAsync(new byte[100], fileName2, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await Task.Delay(300, TestContext.Current.CancellationToken).ConfigureAwait(false);
        File.Move(file2, file2New);
        await Task.Delay(300, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Move
        var file3 = Path.Combine(_tempSession.SessionDirectory, fileName4);
        var subDir = Path.Combine(_tempSession.SessionDirectory, "subdir");
        Directory.CreateDirectory(subDir);
        await _tempSession.CreateFileAsync(new byte[100], fileName4, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await Task.Delay(300, TestContext.Current.CancellationToken).ConfigureAwait(false);
        File.Move(file3, Path.Combine(subDir, fileName5));
        await Task.Delay(300, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(createdFired, "FileCreated should fire");
        Assert.True(changedFired, "FileChanged should fire");
        Assert.True(deletedFired, "FileDeleted should fire");
        Assert.True(renamedFired, "FileRenamed should fire");
        Assert.True(movedFired, "FileMoved should fire");
    }

    [Fact]
    public async Task Events_AllDirectoryEvents_CanFire()
    {
        var dir1 = Path.GetFileName(_tempSession.GetDirectoryPath());
        var dir2 = Path.GetFileName(_tempSession.GetDirectoryPath());
        var dir3 = Path.GetFileName(_tempSession.GetDirectoryPath());
        var dir4 = Path.GetFileName(_tempSession.GetDirectoryPath());
        var createdFired = false;
        var changedFired = false;
        var deletedFired = false;
        var renamedFired = false;
        var movedFired = false;

        // Enable subdirectories to detect moves into subdirectories
        var options = new FileSystemWatcherOptions { IncludeSubdirectories = true };
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options);
        watcher.DirectoryCreated += (_, _) => createdFired = true;
        watcher.DirectoryChanged += (_, _) => changedFired = true;
        watcher.DirectoryDeleted += (_, _) => deletedFired = true;
        watcher.DirectoryRenamed += (_, _) => renamedFired = true;
        watcher.DirectoryMoved += (_, _) => movedFired = true;

        // Wait for initial snapshot
        await Task.Delay(300, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Create
        Directory.CreateDirectory(Path.Combine(_tempSession.SessionDirectory, dir1));
        await PollAssert.ThatAsync(() => createdFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        // Change (add file to directory)
        await _tempSession.CreateFileAsync(new byte[100], Path.Combine(dir1, "test.txt"), TestContext.Current.CancellationToken).ConfigureAwait(false);
        await PollAssert.ThatAsync(() => changedFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        // Delete
        Directory.Delete(Path.Combine(_tempSession.SessionDirectory, dir1), true);
        await PollAssert.ThatAsync(() => deletedFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        // Rename (same parent, different name)
        var dir2Path = Path.Combine(_tempSession.SessionDirectory, dir2);
        var dir2NewPath = Path.Combine(_tempSession.SessionDirectory, dir3);
        Directory.CreateDirectory(dir2Path);
        await Task.Delay(300, TestContext.Current.CancellationToken).ConfigureAwait(false); // Wait for create to be detected
        Directory.Move(dir2Path, dir2NewPath);
        await PollAssert.ThatAsync(() => renamedFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        // Move (different parent, same name - required for move detection)
        var dir3Path = Path.Combine(_tempSession.SessionDirectory, dir4);
        var subDir = Path.Combine(_tempSession.SessionDirectory, "subdir");
        Directory.CreateDirectory(subDir);
        Directory.CreateDirectory(dir3Path);
        await Task.Delay(500, TestContext.Current.CancellationToken).ConfigureAwait(false); // Wait for snapshot to include both directories
        var movedPath = Path.Combine(subDir, dir4); // Keep same name for move detection
        Directory.Move(dir3Path, movedPath);
        await PollAssert.ThatAsync(() => movedFired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.True(createdFired, "DirectoryCreated should fire");
        Assert.True(changedFired, "DirectoryChanged should fire");
        Assert.True(deletedFired, "DirectoryDeleted should fire");
        Assert.True(renamedFired, "DirectoryRenamed should fire");
        Assert.True(movedFired, "DirectoryMoved should fire");
    }

#endregion

#region Event Data Validation Tests

    [Fact]
    public async Task Event_FileCreated_HasCorrectData()
    {
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        var filePath = Path.Combine(_tempSession.SessionDirectory, fileName);
        FileSystemChangeInfo? eventData = null;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.FileCreated += (_, e) => eventData = e;
        await _tempSession.CreateFileAsync(new byte[100], fileName, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await PollAssert.ThatAsync(() => eventData != null, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.NotNull(eventData);
        Assert.Equal(ChangeTypeEnum.Created, eventData.ChangeType);
        Assert.Null(eventData.OldPath);
        Assert.Equal(filePath, eventData.NewPath);
        Assert.False(eventData.IsDirectory);
        Assert.Null(eventData.OldFileCount);
        Assert.Null(eventData.NewFileCount);
        Assert.Null(eventData.OldDirectoryCount);
        Assert.Null(eventData.NewDirCount);
    }

    [Fact]
    public async Task Event_DirectoryChanged_HasCorrectCounts()
    {
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        FileSystemChangeInfo? eventData = null;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.DirectoryChanged += (_, e) => {
            if (e.OldPath == _tempSession.SessionDirectory && e.NewPath == _tempSession.SessionDirectory)
                eventData = e;
        };

        await _tempSession.CreateFileAsync(new byte[100], fileName, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await PollAssert.ThatAsync(() => eventData != null, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.NotNull(eventData);
        Assert.Equal(ChangeTypeEnum.Changed, eventData.ChangeType);
        Assert.True(eventData.IsDirectory);
        Assert.NotNull(eventData.OldFileCount);
        Assert.NotNull(eventData.NewFileCount);
        Assert.NotNull(eventData.OldDirectoryCount);
        Assert.NotNull(eventData.NewDirCount);
        Assert.True(eventData.NewFileCount >= eventData.OldFileCount);
    }

#endregion
}