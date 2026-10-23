using Lyo.FileSystemWatcher.Enums;
using Lyo.FileSystemWatcher.Models;
using Lyo.IO.Temp.Models;
using Lyo.Testing;
using Microsoft.Extensions.Logging;

namespace Lyo.FileSystemWatcher.Tests;

public class SnapshotFilterTests : IDisposable
{
    private readonly IIOTempSession _tempSession;

    public SnapshotFilterTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _tempSession = new IOTempSession(new(), loggerFactory.CreateLogger<IOTempSession>());
    }

    public void Dispose() => _tempSession.Dispose();

    [Fact]
    public void TakeSnapshot_IncludeSubdirectoriesFalse_OmitsNestedFiles()
    {
        var nested = Path.Combine(_tempSession.SessionDirectory, "sub");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(_tempSession.SessionDirectory, "root.txt"), "a");
        File.WriteAllText(Path.Combine(nested, "nested.txt"), "b");
        var tree = Utilities.TakeSnapshot(_tempSession.SessionDirectory, new() { IncludeSubdirectories = false, EnableFileHashing = false }, TestCt);
        tree.TryGetFile(Path.Combine(_tempSession.SessionDirectory, "root.txt"), out var rootFile).ShouldBeTrue();
        rootFile.ShouldNotBeNull();
        tree.TryGetFile(Path.Combine(nested, "nested.txt"), out var _).ShouldBeFalse();
    }

    [Fact]
    public void TakeSnapshot_IncludeJsonOnly_OmitsTxt()
    {
        File.WriteAllText(Path.Combine(_tempSession.SessionDirectory, "a.json"), "{}");
        File.WriteAllText(Path.Combine(_tempSession.SessionDirectory, "b.txt"), "x");
        var tree = Utilities.TakeSnapshot(
            _tempSession.SessionDirectory, new() { IncludeSubdirectories = false, EnableFileHashing = false, IncludePatterns = { @"\.json$" } }, TestCt);

        tree.FileCount.ShouldBe(1);
        tree.TryGetFile(Path.Combine(_tempSession.SessionDirectory, "a.json"), out var _).ShouldBeTrue();
        tree.TryGetFile(Path.Combine(_tempSession.SessionDirectory, "b.txt"), out var _).ShouldBeFalse();
    }

    [Fact]
    public void TakeSnapshot_ExcludeGit_SkipsSubtree()
    {
        var git = Path.Combine(_tempSession.SessionDirectory, ".git");
        Directory.CreateDirectory(git);
        File.WriteAllText(Path.Combine(git, "config"), "x");
        File.WriteAllText(Path.Combine(_tempSession.SessionDirectory, "keep.txt"), "y");
        var tree = Utilities.TakeSnapshot(
            _tempSession.SessionDirectory, new() { IncludeSubdirectories = true, EnableFileHashing = false, ExcludePatterns = { @"(^|/)\.git(/|$)" } }, TestCt);

        tree.TryGetFile(Path.Combine(_tempSession.SessionDirectory, "keep.txt"), out var _).ShouldBeTrue();
        tree.TryGetFile(Path.Combine(git, "config"), out var _).ShouldBeFalse();
    }

    [Fact]
    public void ToDto_CopiesTimestampsAndRelativePaths()
    {
        File.WriteAllText(Path.Combine(_tempSession.SessionDirectory, "a.txt"), "hello");
        var tree = Utilities.TakeSnapshot(_tempSession.SessionDirectory, new() { EnableFileHashing = false, IncludeSubdirectories = false }, TestCt);
        var dto = tree.ToDto();
        dto.RootPath.ShouldBe(_tempSession.SessionDirectory);
        var file = dto.EnumerateFiles().Single();
        file.Path.ShouldBe("a.txt");
        file.Entry.LastWriteTimeUtc.ShouldNotBeNull();
        file.Entry.FileSize.ShouldNotBeNull();
        file.Entry.FileSize!.Value.ShouldBeGreaterThan(0);
        file.Entry.IsDirectory.ShouldBeFalse();
    }

    [Fact]
    public void DetectChanges_LiveAndDto_AgreeOnCreatedFileKind()
    {
        File.WriteAllText(Path.Combine(_tempSession.SessionDirectory, "a.txt"), "a");
        var oldLive = Utilities.TakeSnapshot(_tempSession.SessionDirectory, new() { EnableFileHashing = true, IncludeSubdirectories = false }, TestCt);
        File.WriteAllText(Path.Combine(_tempSession.SessionDirectory, "b.txt"), "b");
        var newLive = Utilities.TakeSnapshot(_tempSession.SessionDirectory, new() { EnableFileHashing = true, IncludeSubdirectories = false }, TestCt, oldLive);
        var live = Utilities.DetectChanges(oldLive, newLive, ct: TestCt);
        var dto = FileSystemSnapshotDiffer.DetectChanges(oldLive.ToDto(), newLive.ToDto(), ct: TestCt);
        var liveCreated = live.Where(c => !c.IsDirectory && c.ChangeType == ChangeTypeEnum.Created).Select(c => Path.GetFileName(c.NewPath)).ToArray();
        var dtoCreated = dto.Where(c => !c.IsDirectory && c.ChangeType == FileSystemChangeKind.Created).Select(c => c.NewPath).ToArray();
        liveCreated.ShouldBeEquivalentTo(dtoCreated);
    }

    [Fact]
    public async Task ScanCompleted_FiresOnceWithBatch()
    {
        FileSystemScanCompletedEventArgs? args = null;
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, new FileSystemWatcherOptions { DebounceTimerDelay = 50 });
        watcher.ScanCompleted += (_, e) => args = e;
        await _tempSession.CreateFileAsync(new byte[100], fileName, TestCt);
        await PollAssert.ThatAsync(() => args != null, TimeSpan.FromSeconds(5));
        args.ShouldNotBeNull();
        args!.Current.ShouldNotBeNull();
        args.Previous.ShouldNotBeNull();
        args.Changes.ShouldAnySatisfy(c => !c.IsDirectory && c.ChangeType == ChangeTypeEnum.Created);
    }
}