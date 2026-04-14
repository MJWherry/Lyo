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
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1)).ConfigureAwait(false);
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
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1)).ConfigureAwait(false);
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
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1)).ConfigureAwait(false);
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
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1)).ConfigureAwait(false);
    }

    [Fact]
    public async Task Directory_CreateDirectory_CreateEventFires()
    {
        var fired = false;
        var directoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.DirectoryCreated += (_, e) => {
            Assert.Equal(ChangeTypeEnum.Created, e.ChangeType);
            Assert.Null(e.OldPath);
            Assert.EndsWith(directoryName, e.NewPath);
            Assert.True(e.IsDirectory);
            fired = true;
        };

        Directory.CreateDirectory(Path.Combine(_tempSession.SessionDirectory, directoryName));
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1)).ConfigureAwait(false);
    }

    [Fact]
    public async Task Directory_ChangeDirectoryAddDirectory_ChangeEventFires()
    {
        var fired = false;
        var directoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.DirectoryChanged += (_, e) => {
            Assert.Equal(ChangeTypeEnum.Changed, e.ChangeType);
            Assert.EndsWith(_tempSession.SessionDirectory, e.OldPath);
            Assert.EndsWith(_tempSession.SessionDirectory, e.NewPath);
            Assert.True(e.IsDirectory);
            Assert.Equal(0, e.OldDirectoryCount);
            Assert.Equal(1, e.NewDirCount);
            fired = true;
        };

        Directory.CreateDirectory(Path.Combine(_tempSession.SessionDirectory, directoryName));
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1)).ConfigureAwait(false);
    }

    [Fact]
    public async Task Directory_ChangeDirectoryAddFile_ChangeEventFires()
    {
        var fired = false;
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.DirectoryChanged += (_, e) => {
            Assert.Equal(ChangeTypeEnum.Changed, e.ChangeType);
            Assert.EndsWith(_tempSession.SessionDirectory, e.OldPath);
            Assert.EndsWith(_tempSession.SessionDirectory, e.NewPath);
            Assert.True(e.IsDirectory);
            Assert.Equal(0, e.OldFileCount);
            Assert.Equal(1, e.NewFileCount);
            fired = true;
        };

        await _tempSession.CreateFileAsync(new byte[100], ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
    }

    [Fact]
    public async Task Directory_DirectoryDeleted_DeletedEventFires()
    {
        var fired = false;
        var directoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        Directory.CreateDirectory(Path.Combine(_tempSession.SessionDirectory, directoryName));
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.DirectoryDeleted += (_, e) => {
            Assert.Equal(ChangeTypeEnum.Deleted, e.ChangeType);
            Assert.EndsWith(directoryName, e.OldPath);
            Assert.Null(e.NewPath);
            Assert.True(e.IsDirectory);
            fired = true;
        };

        Directory.Delete(Path.Combine(_tempSession.SessionDirectory, directoryName));
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1)).ConfigureAwait(false);
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
            Assert.Equal(ChangeTypeEnum.Renamed, e.ChangeType);
            Assert.EndsWith(directoryName, e.OldPath);
            Assert.EndsWith(newFileName, e.NewPath);
            Assert.True(e.IsDirectory);
            fired = true;
        };

        Directory.Move(Path.Combine(_tempSession.SessionDirectory, directoryName), Path.Combine(_tempSession.SessionDirectory, newFileName));
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1)).ConfigureAwait(false);
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
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1)).ConfigureAwait(false);
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
            Assert.Equal(ChangeTypeEnum.Created, e.ChangeType);
            Assert.Null(e.OldPath);
            Assert.Contains($"{directoryPrefix}_", e.NewPath);
            Assert.True(e.IsDirectory);
            fired++;
        };

        for (var i = 0; i < directoryAmount; i++)
            Directory.CreateDirectory(Path.Combine(_tempSession.SessionDirectory, $"{directoryPrefix}_{i}"));

        await PollAssert.ThatAsync(() => directoryAmount == fired, TimeSpan.FromSeconds(30)).ConfigureAwait(false);
    }
}