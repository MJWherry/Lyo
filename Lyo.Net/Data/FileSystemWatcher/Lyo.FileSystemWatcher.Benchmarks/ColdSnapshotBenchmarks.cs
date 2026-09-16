using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.IO.Temp.Models;

namespace Lyo.FileSystemWatcher.Benchmarks;

/// <summary>First scan of a small-file tree, with no previous snapshot to reuse.</summary>
[BenchmarkDescription(
    "Cold TakeSnapshot walks an IOTempSession directory of FileCount files spread across FileCount/10 directories. There is no previous tree, so hashing " +
    "cannot reuse fingerprints. FileBytes is the size of each file. EnableHashing off stats the tree; on also samples each file for move detection.")]
[BenchmarkParameter("FileCount", Unit = "files", Description = "Files in the tree. Directory count is FileCount/10, so both scale together.")]
[BenchmarkParameter("FileBytes", Unit = "bytes", Description = "Size of each file. Each file starts with its own index so fingerprints do not collide.")]
[BenchmarkParameter("EnableHashing", Description = "Whether the snapshot computes per-file fingerprints for move and rename detection.")]
public class ColdSnapshotBenchmarks
{
    private IOTempSession _session = null!;
    private FileSystemWatcherOptions _options = null!;

    [Params(200, 2000)]
    public int FileCount { get; set; }

    [Params(64L, 65536L)]
    public long FileBytes { get; set; }

    [Params(false, true)]
    public bool EnableHashing { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _session = WatchTree.Open();
        _options = WatchTree.Options(EnableHashing);
        WatchTree.BuildSmall(_session.SessionDirectory, FileCount, FileBytes);
    }

    [GlobalCleanup]
    public void Cleanup() => _session.Dispose();

    [Benchmark]
    [BenchmarkDescription("Walk the session directory from disk and build a SnapshotTree, with no previous tree.")]
    public int TakeSnapshot() => Utilities.TakeSnapshot(_session.SessionDirectory, _options).TotalEntryCount;
}
