using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.IO.Temp.Models;

namespace Lyo.FileSystemWatcher.Benchmarks;

/// <summary>Steady-state poll of one sparse file at 1 GiB or 2 GiB.</summary>
/// <remarks>
/// The file is created with <see cref="FileStream.SetLength(long)" />, so setup does not write the bytes. Above 1 GiB the fingerprinter reads 64 bytes plus the
/// modification time. A full digest is taken only when another file shares that size, and this arm keeps the huge file's size unique.
/// </remarks>
[BenchmarkDescription(
    "LargeFilePoll is the warm poll for a single sparse file of FileBytes, either 1 GiB (beginning, middle, and end samples) or 2 GiB (64-byte sample plus " +
    "modification time). The file lives in an IOTempSession directory. Event is none, a same-length rewrite, or a move to the neighbouring directory. The " +
    "same-length rewrite is reported only when EnableHashing is on.")]
[BenchmarkParameter("FileBytes", Unit = "bytes", Description = "Logical size of the one sparse file. 1073741824 is 1 GiB; 2147483648 is 2 GiB.")]
[BenchmarkParameter("EnableHashing", Description = "Whether the snapshot samples the file. The sample does not read the whole file.")]
[BenchmarkParameter("Event", Description = "None leaves the file untouched. Changed rewrites the start and keeps the length. Moved shifts it to the other directory.")]
public class LargeFilePollBenchmarks
{
    private const long OneGiB = 1024L * 1024 * 1024;

    private IOTempSession _session = null!;
    private FileSystemWatcherOptions _options = null!;
    private SnapshotTree _before = null!;
    private SnapshotTree _after = null!;

    [Params(OneGiB, OneGiB * 2)]
    public long FileBytes { get; set; }

    [Params(false, true)]
    public bool EnableHashing { get; set; }

    [Params(BenchEvent.None, BenchEvent.Changed, BenchEvent.Moved)]
    public BenchEvent Event { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _session = WatchTree.Open();
        _options = WatchTree.Options(EnableHashing);
        var root = _session.SessionDirectory;
        var files = WatchTree.BuildLarge(root, FileBytes);
        _before = Utilities.TakeSnapshot(root, _options);
        WatchTree.Apply(Event, root, files, FileBytes);
        _after = Utilities.TakeSnapshot(root, _options, oldTree: _before);
        WatchTree.AssertDetected(Event, EnableHashing, Utilities.DetectChanges(_before, _after, _options.PathComparison));
    }

    [GlobalCleanup]
    public void Cleanup() => _session.Dispose();

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Re-walk the session directory that holds the sparse file, reusing the snapshot taken before the event.")]
    public int WarmTakeSnapshot() => Utilities.TakeSnapshot(_session.SessionDirectory, _options, oldTree: _before).TotalEntryCount;

    [Benchmark]
    [BenchmarkDescription("Diff the two setup snapshots of the sparse file. This does not read the file.")]
    public int DetectChanges() => Utilities.DetectChanges(_before, _after, _options.PathComparison).Count;
}
