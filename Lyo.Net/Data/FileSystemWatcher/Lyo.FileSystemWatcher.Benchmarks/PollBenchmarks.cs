using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.IO.Temp.Models;

namespace Lyo.FileSystemWatcher.Benchmarks;

/// <summary>Steady-state poll over a small-file tree: a warm walk that reuses the previous snapshot, then the diff.</summary>
[BenchmarkDescription(
    "Poll watches an IOTempSession directory. Setup writes FileCount files of FileBytes each, snapshots the tree, applies Event, then snapshots again with the " +
    "first tree passed in so unchanged files reuse their fingerprints. WarmTakeSnapshot repeats that second walk. DetectChanges diffs the two setup snapshots and " +
    "does not touch disk. A same-length edit is reported only when EnableHashing is on; without hashing a rename or move is reported as a delete plus a create.")]
[BenchmarkParameter("FileCount", Unit = "files", Description = "Files in the tree. Directory count is FileCount/10, so both scale together.")]
[BenchmarkParameter("FileBytes", Unit = "bytes", Description = "Size of each file. Each file starts with its own index so fingerprints do not collide.")]
[BenchmarkParameter("EnableHashing", Description = "Whether snapshots compute per-file fingerprints. Rename and move matching needs this.")]
[BenchmarkParameter("Event", Description = "Change applied between the two snapshots: none, one file changed, created, deleted, renamed, or moved, or a mixed batch.")]
public class PollBenchmarks
{
    private IOTempSession _session = null!;
    private FileSystemWatcherOptions _options = null!;
    private SnapshotTree _before = null!;
    private SnapshotTree _after = null!;

    [Params(200, 2000)]
    public int FileCount { get; set; }

    [Params(64L, 65536L)]
    public long FileBytes { get; set; }

    [Params(false, true)]
    public bool EnableHashing { get; set; }

    [Params(BenchEvent.None, BenchEvent.Changed, BenchEvent.Created, BenchEvent.Deleted, BenchEvent.Renamed, BenchEvent.Moved, BenchEvent.Mixed)]
    public BenchEvent Event { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _session = WatchTree.Open();
        _options = WatchTree.Options(EnableHashing);
        var root = _session.SessionDirectory;
        var files = WatchTree.BuildSmall(root, FileCount, FileBytes);
        _before = Utilities.TakeSnapshot(root, _options);
        WatchTree.Apply(Event, root, files, FileBytes);
        _after = Utilities.TakeSnapshot(root, _options, oldTree: _before);
        WatchTree.AssertDetected(Event, EnableHashing, Utilities.DetectChanges(_before, _after, _options.PathComparison));
    }

    [GlobalCleanup]
    public void Cleanup() => _session.Dispose();

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Re-walk the session directory, reusing fingerprints from the snapshot taken before the event.")]
    public int WarmTakeSnapshot() => Utilities.TakeSnapshot(_session.SessionDirectory, _options, oldTree: _before).TotalEntryCount;

    [Benchmark]
    [BenchmarkDescription("Diff the two setup snapshots. Allocated bytes per operation is the per-poll diff cost, with no disk walk.")]
    public int DetectChanges() => Utilities.DetectChanges(_before, _after, _options.PathComparison).Count;
}
