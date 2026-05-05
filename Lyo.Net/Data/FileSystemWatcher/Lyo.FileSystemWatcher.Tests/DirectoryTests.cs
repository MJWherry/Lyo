using Lyo.FileSystemWatcher.Enums;
using Lyo.IO.Temp.Models;
using Lyo.Testing;
using Microsoft.Extensions.Logging;

namespace Lyo.FileSystemWatcher.Tests;

public class DirectoryTests : IDisposable
{
    private readonly IIOTempSession _tempSession;

    public DirectoryTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _tempSession = new IOTempSession(new(), loggerFactory.CreateLogger<IOTempSession>());
    }

    public void Dispose() => _tempSession.Dispose();

    [Fact]
    public async Task Directory_CreateDirectory_AnyEventFires()
    {
        var fired = false;
        var directoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.OnAnyChange += (_, e) => {
            //ignore directory event for root dir change event
            if (e is { ChangeType: ChangeTypeEnum.Changed, IsDirectory: true })
                return;

            Assert.Equal(ChangeTypeEnum.Created, e.ChangeType);
            Assert.Null(e.OldPath);
            Assert.EndsWith(directoryName, e.NewPath);
            Assert.True(e.IsDirectory);
            fired = true;
        };

        Directory.CreateDirectory(Path.Combine(_tempSession.SessionDirectory, directoryName));
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Directory_DirectoryDeleted_AnyEventFires()
    {
        var fired = false;
        var directoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        Directory.CreateDirectory(Path.Combine(_tempSession.SessionDirectory, directoryName));
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.OnAnyChange += (_, e) => {
            //ignore directory event for root dir change event
            if (e is { ChangeType: ChangeTypeEnum.Changed, IsDirectory: true })
                return;

            Assert.Equal(ChangeTypeEnum.Deleted, e.ChangeType);
            Assert.EndsWith(directoryName, e.OldPath);
            Assert.Null(e.NewPath);
            Assert.True(e.IsDirectory);
            fired = true;
        };

        Directory.Delete(Path.Combine(_tempSession.SessionDirectory, directoryName));
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Directory_DirectoryRenamed_AnyEventFires()
    {
        var fired = false;
        var directoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        var newDirectoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        Directory.CreateDirectory(Path.Combine(_tempSession.SessionDirectory, directoryName));
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.OnAnyChange += (_, e) => {
            //ignore directory event for root dir change event
            if (e is { ChangeType: ChangeTypeEnum.Changed, IsDirectory: true })
                return;

            Assert.Equal(ChangeTypeEnum.Renamed, e.ChangeType);
            Assert.EndsWith(directoryName, e.OldPath);
            Assert.EndsWith(newDirectoryName, e.NewPath);
            Assert.True(e.IsDirectory);
            fired = true;
        };

        Directory.Move(Path.Combine(_tempSession.SessionDirectory, directoryName), Path.Combine(_tempSession.SessionDirectory, newDirectoryName));
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Directory_DirectoryMoved_AnyEventFires()
    {
        var fired = false;
        var directoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        var subDirectoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        Directory.CreateDirectory(Path.Combine(_tempSession.SessionDirectory, directoryName));
        Directory.CreateDirectory(Path.Combine(_tempSession.SessionDirectory, subDirectoryName));
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.OnAnyChange += (_, e) => {
            //ignore directory event for root dir change event
            if (e is { ChangeType: ChangeTypeEnum.Changed, IsDirectory: true })
                return;

            Assert.Equal(ChangeTypeEnum.Moved, e.ChangeType);
            Assert.EndsWith(directoryName, e.OldPath);
            Assert.EndsWith(Path.Combine(subDirectoryName, directoryName), e.NewPath);
            Assert.True(e.IsDirectory);
            fired = true;
        };

        Directory.Move(Path.Combine(_tempSession.SessionDirectory, directoryName), Path.Combine(_tempSession.SessionDirectory, subDirectoryName, directoryName));
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Directory_CreateDirectory_CreateEventFires()
    {
        var fired = false;
        var directoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.DirectoryCreated += (_, e) => {
            var (oldPath, newPath, changeTypeEnum, isDirectory, _, _, _, _) = e;
            Assert.Equal(ChangeTypeEnum.Created, changeTypeEnum);
            Assert.Null(oldPath);
            Assert.EndsWith(directoryName, newPath);
            Assert.True(isDirectory);
            fired = true;
        };

        Directory.CreateDirectory(Path.Combine(_tempSession.SessionDirectory, directoryName));
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Directory_ChangeDirectoryAddDirectory_ChangeEventFires()
    {
        var fired = false;
        var directoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.DirectoryChanged += (_, e) => {
            var (oldPath, newPath, changeTypeEnum, isDirectory, _, oldDirectoryCount, _, newDirCount) = e;
            Assert.Equal(ChangeTypeEnum.Changed, changeTypeEnum);
            Assert.EndsWith(_tempSession.SessionDirectory, oldPath);
            Assert.EndsWith(_tempSession.SessionDirectory, newPath);
            Assert.True(isDirectory);
            Assert.Equal(0, oldDirectoryCount);
            Assert.Equal(1, newDirCount);
            fired = true;
        };

        Directory.CreateDirectory(Path.Combine(_tempSession.SessionDirectory, directoryName));
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Directory_ChangeDirectoryAddFile_ChangeEventFires()
    {
        var fired = false;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.DirectoryChanged += (_, e) => {
            var (oldPath, newPath, changeTypeEnum, isDirectory, oldFileCount, _, newFileCount, _) = e;
            Assert.Equal(ChangeTypeEnum.Changed, changeTypeEnum);
            Assert.EndsWith(_tempSession.SessionDirectory, oldPath);
            Assert.EndsWith(_tempSession.SessionDirectory, newPath);
            Assert.True(isDirectory);
            Assert.Equal(0, oldFileCount);
            Assert.Equal(1, newFileCount);
            fired = true;
        };

        await _tempSession.CreateFileAsync(new byte[100], ct: TestContext.Current.CancellationToken);
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Directory_DirectoryDeleted_DeletedEventFires()
    {
        var fired = false;
        var directoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        Directory.CreateDirectory(Path.Combine(_tempSession.SessionDirectory, directoryName));
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.DirectoryDeleted += (_, e) => {
            var (oldPath, newPath, changeTypeEnum, isDirectory, _, _, _, _) = e;
            Assert.Equal(ChangeTypeEnum.Deleted, changeTypeEnum);
            Assert.EndsWith(directoryName, oldPath);
            Assert.Null(newPath);
            Assert.True(isDirectory);
            fired = true;
        };

        Directory.Delete(Path.Combine(_tempSession.SessionDirectory, directoryName));
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Directory_DirectoryRenamed_RenamedEventFires()
    {
        var fired = false;
        var directoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        var newFileName = Path.GetFileName(_tempSession.GetDirectoryPath());
        Directory.CreateDirectory(Path.Combine(_tempSession.SessionDirectory, directoryName));
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.DirectoryRenamed += (_, e) => {
            var (oldPath, newPath, changeTypeEnum, isDirectory, _, _, _, _) = e;
            Assert.Equal(ChangeTypeEnum.Renamed, changeTypeEnum);
            Assert.EndsWith(directoryName, oldPath);
            Assert.EndsWith(newFileName, newPath);
            Assert.True(isDirectory);
            fired = true;
        };

        Directory.Move(Path.Combine(_tempSession.SessionDirectory, directoryName), Path.Combine(_tempSession.SessionDirectory, newFileName));
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Directory_DirectoryMoved_MovedEventFires()
    {
        var fired = false;
        var directoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        var subDirectoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        Directory.CreateDirectory(Path.Combine(_tempSession.SessionDirectory, directoryName));
        Directory.CreateDirectory(Path.Combine(_tempSession.SessionDirectory, subDirectoryName));
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.DirectoryMoved += (_, e) => {
            Assert.Equal(ChangeTypeEnum.Moved, e.ChangeType);
            Assert.EndsWith(directoryName, e.OldPath);
            Assert.EndsWith(Path.Combine(subDirectoryName, directoryName), e.NewPath);
            Assert.True(e.IsDirectory);
            fired = true;
        };

        Directory.Move(Path.Combine(_tempSession.SessionDirectory, directoryName), Path.Combine(_tempSession.SessionDirectory, subDirectoryName, directoryName));
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(25)]
    [InlineData(100)]
    public async Task Directory_CreateDirectories_MultipleCreateEventFires(int directoryAmount)
    {
        var fired = 0;
        var directoryPrefix = Path.GetFileName(_tempSession.GetDirectoryPath());
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.DirectoryCreated += (_, e) => {
            var (oldPath, newPath, changeTypeEnum, isDirectory, _, _, _, _) = e;
            Assert.Equal(ChangeTypeEnum.Created, changeTypeEnum);
            Assert.Null(oldPath);
            Assert.Contains($"{directoryPrefix}_", newPath);
            Assert.True(isDirectory);
            fired++;
        };

        for (var i = 0; i < directoryAmount; i++)
            Directory.CreateDirectory(Path.Combine(_tempSession.SessionDirectory, $"{directoryPrefix}_{i}"));

        await PollAssert.ThatAsync(() => directoryAmount == fired, TimeSpan.FromSeconds(30));
    }
}