using Lyo.Benchmark;

[assembly:
    BenchmarkReport(
        "filesystemwatcher", "File System Watcher",
        Description = "Snapshot and diff costs for Lyo.FileSystemWatcher. Every job watches an IOTempSession directory and disposes that session when the job ends. " +
            "ColdSnapshot is the first walk of a small-file tree, with no previous snapshot. Poll is the steady-state cycle over file count, file size, hashing, and " +
            "change events: a warm TakeSnapshot that reuses the previous tree, then DetectChanges. LargeFilePoll is that same cycle for one sparse 1 GiB or 2 GiB file.")]

BenchmarkEntry.Run(args);