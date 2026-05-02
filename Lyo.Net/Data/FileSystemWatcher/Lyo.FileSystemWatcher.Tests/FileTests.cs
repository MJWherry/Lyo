using Lyo.FileSystemWatcher.Enums;
using Lyo.IO.Temp.Models;
using Lyo.Testing;
using Microsoft.Extensions.Logging;

namespace Lyo.FileSystemWatcher.Tests;

public class FileTests : IDisposable
{
    private readonly IIOTempSession _tempSession;

    public FileTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _tempSession = new IOTempSession(new(), loggerFactory.CreateLogger<IOTempSession>());
    }

    public void Dispose() => _tempSession.Dispose();

    [Fact]
    public async Task File_CreateFile_AnyEventFires()
    {
        var fired = false;
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.OnAnyChange += (_, e) => {
            //ignore directory event for root dir change event
            if (e is { ChangeType: ChangeTypeEnum.Changed, IsDirectory: true })
                return;

            e.ChangeType.ShouldBe(ChangeTypeEnum.Created);
            e.OldPath.ShouldBeNull();
            e.NewPath.ShouldEndWith(fileName);
            e.IsDirectory.ShouldBeFalse();
            fired = true;
        };

        await _tempSession.CreateFileAsync(new byte[100], fileName, TestContext.Current.CancellationToken);
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task File_ChangeFile_AnyEventFires()
    {
        var fired = false;
        var filePath = await _tempSession.CreateFileAsync(new byte[100], ct: TestContext.Current.CancellationToken);
        var fileName = Path.GetFileName(filePath);
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.OnAnyChange += (_, e) => {
            //ignore directory event for root dir change event
            if (e is { ChangeType: ChangeTypeEnum.Changed, IsDirectory: true })
                return;

            e.ChangeType.ShouldBe(ChangeTypeEnum.Changed);
            e.OldPath.ShouldEndWith(fileName);
            e.NewPath.ShouldEndWith(fileName);
            e.IsDirectory.ShouldBeFalse();
            fired = true;
        };

        Testing.Utilities.AppendBytesToFile(filePath, 100);
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task File_FileDeleted_AnyEventFires()
    {
        var fired = false;
        var filePath = await _tempSession.CreateFileAsync(new byte[100], ct: TestContext.Current.CancellationToken);
        var fileName = Path.GetFileName(filePath);
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.OnAnyChange += (_, e) => {
            //ignore directory event for root dir change event
            if (e is { ChangeType: ChangeTypeEnum.Changed, IsDirectory: true })
                return;

            e.ChangeType.ShouldBe(ChangeTypeEnum.Deleted);
            e.OldPath.ShouldEndWith(fileName);
            e.NewPath.ShouldBeNull();
            e.IsDirectory.ShouldBeFalse();
            fired = true;
        };

        File.Delete(filePath);
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task File_FileRenamed_AnyEventFires()
    {
        var fired = false;
        var filePath = await _tempSession.CreateFileAsync(new byte[100], ct: TestContext.Current.CancellationToken);
        var fileName = Path.GetFileName(filePath);
        var newFileName = Path.GetFileName(_tempSession.GetFilePath());
        var newFilePath = Path.Combine(_tempSession.SessionDirectory, newFileName);
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.OnAnyChange += (_, e) => {
            //ignore directory event for root dir change event
            if (e is { ChangeType: ChangeTypeEnum.Changed, IsDirectory: true })
                return;

            e.ChangeType.ShouldBe(ChangeTypeEnum.Renamed);
            e.OldPath.ShouldEndWith(fileName);
            e.NewPath.ShouldEndWith(newFileName);
            e.IsDirectory.ShouldBeFalse();
            fired = true;
        };

        File.Move(filePath, newFilePath);
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task File_FileMoved_AnyEventFires()
    {
        var fired = false;
        var filePath = await _tempSession.CreateFileAsync(new byte[100], ct: TestContext.Current.CancellationToken);
        var fileName = Path.GetFileName(filePath);
        var subDirectoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        Directory.CreateDirectory(Path.Combine(_tempSession.SessionDirectory, subDirectoryName));
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.OnAnyChange += (_, e) => {
            //ignore directory event for root dir change event
            if (e is { ChangeType: ChangeTypeEnum.Changed, IsDirectory: true })
                return;

            e.ChangeType.ShouldBe(ChangeTypeEnum.Moved);
            e.OldPath.ShouldEndWith(fileName);
            e.NewPath.ShouldEndWith(Path.Combine(subDirectoryName, fileName));
            e.IsDirectory.ShouldBeFalse();
            fired = true;
        };

        File.Move(filePath, Path.Combine(_tempSession.SessionDirectory, subDirectoryName, fileName));
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task File_CreateFile_CreateEventFires()
    {
        var fired = false;
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.FileCreated += (_, e) => {
            e.ChangeType.ShouldBe(ChangeTypeEnum.Created);
            e.OldPath.ShouldBeNull();
            e.NewPath.ShouldEndWith(fileName);
            e.IsDirectory.ShouldBeFalse();
            fired = true;
        };

        await _tempSession.CreateFileAsync(new byte[100], fileName, TestContext.Current.CancellationToken);
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task File_FileChanged_ChangedEventFires()
    {
        var fired = false;
        var filePath = await _tempSession.CreateFileAsync(new byte[100], ct: TestContext.Current.CancellationToken);
        var fileName = Path.GetFileName(filePath);
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.FileChanged += (_, e) => {
            e.ChangeType.ShouldBe(ChangeTypeEnum.Changed);
            e.OldPath.ShouldEndWith(fileName);
            e.NewPath.ShouldEndWith(fileName);
            e.IsDirectory.ShouldBeFalse();
            fired = true;
        };

        Testing.Utilities.AppendBytesToFile(filePath, 100);
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task File_FileDeleted_DeletedEventFires()
    {
        var fired = false;
        var filePath = await _tempSession.CreateFileAsync(new byte[100], ct: TestContext.Current.CancellationToken);
        var fileName = Path.GetFileName(filePath);
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.FileDeleted += (_, e) => {
            e.ChangeType.ShouldBe(ChangeTypeEnum.Deleted);
            e.OldPath.ShouldEndWith(fileName);
            e.NewPath.ShouldBeNull();
            e.IsDirectory.ShouldBeFalse();
            fired = true;
        };

        File.Delete(filePath);
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task File_FileRenamed_RenamedEventFires()
    {
        var fired = false;
        var filePath = await _tempSession.CreateFileAsync(new byte[100], ct: TestContext.Current.CancellationToken);
        var fileName = Path.GetFileName(filePath);
        var newFileName = Path.GetFileName(_tempSession.GetFilePath());
        var newFilePath = Path.Combine(_tempSession.SessionDirectory, newFileName);
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.FileRenamed += (_, e) => {
            e.ChangeType.ShouldBe(ChangeTypeEnum.Renamed);
            e.OldPath.ShouldEndWith(fileName);
            e.NewPath.ShouldEndWith(newFileName);
            e.IsDirectory.ShouldBeFalse();
            fired = true;
        };

        File.Move(filePath, newFilePath);
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task File_FileMoved_MovedEventFires()
    {
        var fired = false;
        var filePath = await _tempSession.CreateFileAsync(new byte[100], ct: TestContext.Current.CancellationToken);
        var fileName = Path.GetFileName(filePath);
        var subDirectoryName = Path.GetFileName(_tempSession.GetDirectoryPath());
        Directory.CreateDirectory(Path.Combine(_tempSession.SessionDirectory, subDirectoryName));
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.FileMoved += (_, e) => {
            e.ChangeType.ShouldBe(ChangeTypeEnum.Moved);
            e.OldPath.ShouldEndWith(fileName);
            e.NewPath.ShouldEndWith(Path.Combine(subDirectoryName, fileName));
            e.IsDirectory.ShouldBeFalse();
            fired = true;
        };

        File.Move(filePath, Path.Combine(_tempSession.SessionDirectory, subDirectoryName, fileName));
        await PollAssert.ThatAsync(() => fired, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task File_FileCreated_DirectoryAndFileEventsFires()
    {
        var createdFired = false;
        var changedFired = false;
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.OnAnyChange += (_, e) => {
            switch (e.IsDirectory) {
                case false when e.ChangeType == ChangeTypeEnum.Created:
                    createdFired = true;
                    break;
                case true when e.ChangeType == ChangeTypeEnum.Changed:
                    changedFired = true;
                    break;
                default:
                    Assert.Fail("Invalid Change event thrown");
                    break;
            }
        };

        await _tempSession.CreateFileAsync(new byte[100], fileName, TestContext.Current.CancellationToken);
        await PollAssert.ThatAsync(() => createdFired && changedFired, TimeSpan.FromSeconds(30));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(25)]
    [InlineData(100)]
    public async Task File_CreateFiles_MultipleCreateEventFires(int fileAmount)
    {
        var fired = 0;
        var expectedFileNames = new HashSet<string>(StringComparer.Ordinal);
        var createdFileNames = new HashSet<string>(StringComparer.Ordinal);
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory);
        watcher.FileCreated += (_, e) => {
            e.ChangeType.ShouldBe(ChangeTypeEnum.Created);
            e.OldPath.ShouldBeNull();
            e.NewPath.ShouldNotBeNull();
            e.IsDirectory.ShouldBeFalse();
            createdFileNames.Add(Path.GetFileName(e.NewPath!));
            fired++;
        };

        for (var i = 0; i < fileAmount; i++) {
            var filePath = await _tempSession.CreateFileAsync(new byte[100], ct: TestContext.Current.CancellationToken);
            expectedFileNames.Add(Path.GetFileName(filePath));
        }

        await PollAssert.ThatAsync(() => fileAmount == fired, TimeSpan.FromSeconds(30));
        expectedFileNames.ShouldBe(createdFileNames);
    }
}