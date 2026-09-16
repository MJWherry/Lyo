using System.Text;
using Lyo.FileSystemWatcher.Enums;
using Lyo.IO.Temp.Models;

namespace Lyo.FileSystemWatcher.Benchmarks;

/// <summary>Change applied between the before and after snapshots of a poll.</summary>
public enum BenchEvent
{
    /// <summary>Tree is left untouched.</summary>
    None,

    /// <summary>One existing file is rewritten at the same length.</summary>
    Changed,

    /// <summary>One new file is added.</summary>
    Created,

    /// <summary>One existing file is deleted.</summary>
    Deleted,

    /// <summary>One existing file is renamed inside its current directory.</summary>
    Renamed,

    /// <summary>One existing file is moved to a different directory.</summary>
    Moved,

    /// <summary>A small share of files is changed, created, deleted, and renamed together.</summary>
    Mixed
}

/// <summary>Builds and mutates the directory an <see cref="IOTempSession" /> lends to a watcher benchmark.</summary>
internal static class WatchTree
{
    private const int PadBufferSize = 8192;

    /// <summary>Opens a session with no per-file cap. The default cap is 1 GiB, which the large-file arm exceeds.</summary>
    public static IOTempSession Open() => new(new() { MaxFileSizeBytes = null });

    /// <summary>Watcher options for one matrix point. Path comparison is ordinal so the diff does not fold case.</summary>
    public static FileSystemWatcherOptions Options(bool enableHashing)
        => new() {
            EnableFileHashing = enableHashing,
            IncludeSubdirectories = true,
            PathComparison = StringComparison.Ordinal
        };

    /// <summary>Writes <paramref name="fileCount" /> small files under <paramref name="root" />, spread across <c>fileCount / 10</c> directories.</summary>
    public static List<string> BuildSmall(string root, int fileCount, long fileBytes)
    {
        var directories = Math.Max(1, fileCount / 10);
        for (var i = 0; i < directories; i++)
            Directory.CreateDirectory(Path.Combine(root, $"dir-{i:D4}"));

        List<string> paths = [];
        for (var i = 0; i < fileCount; i++) {
            var path = Path.Combine(root, $"dir-{i % directories:D4}", $"file-{i:D5}.bin");
            WritePadded(path, fileBytes, i);
            paths.Add(path);
        }

        return paths;
    }

    /// <summary>Writes one sparse file of <paramref name="fileBytes" /> under <paramref name="root" />. A second directory exists so a move has somewhere to go.</summary>
    public static List<string> BuildLarge(string root, long fileBytes)
    {
        var sourceDir = Path.Combine(root, "dir-0000");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(Path.Combine(root, "dir-0001"));
        var path = Path.Combine(sourceDir, "huge.bin");
        WriteSparse(path, fileBytes, 0);
        return [path];
    }

    /// <summary>Applies <paramref name="ev" /> to a tree that was just snapshotted.</summary>
    public static void Apply(BenchEvent ev, string root, IReadOnlyList<string> files, long fileBytes)
    {
        switch (ev) {
            case BenchEvent.None:
                return;
            case BenchEvent.Changed:
                RewriteSameLength(files[0]);
                return;
            case BenchEvent.Created:
                WritePadded(Path.Combine(root, "dir-0000", "added.bin"), fileBytes, files.Count);
                return;
            case BenchEvent.Deleted:
                File.Delete(files[0]);
                return;
            case BenchEvent.Renamed:
                MoveWithinDirectory(files[0], "renamed.bin");
                return;
            case BenchEvent.Moved:
                File.Move(files[0], Path.Combine(root, "dir-0001", Path.GetFileName(files[0])));
                return;
            case BenchEvent.Mixed:
                ApplyMixed(root, files, fileBytes);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(ev), ev, "Unhandled benchmark event.");
        }
    }

    /// <summary>
    /// Throws when <paramref name="changes" /> is not what <paramref name="ev" /> should produce. Content edits and rename matching show up only when hashing is on,
    /// because a snapshot taken without hashing stores no size, fingerprint, or digest.
    /// </summary>
    public static void AssertDetected(BenchEvent ev, bool hashing, IReadOnlyList<FileSystemChangeInfo> changes)
    {
        var files = changes.Where(c => !c.IsDirectory).Select(c => c.ChangeType).ToList();
        switch (ev) {
            case BenchEvent.None:
                if (changes.Count != 0)
                    throw Fail(ev, changes);

                return;
            case BenchEvent.Changed:
                if (hashing)
                    Require(files, ChangeTypeEnum.Changed, ev, changes);
                else if (files.Contains(ChangeTypeEnum.Changed))
                    throw Fail(ev, changes);

                return;
            case BenchEvent.Created:
                Require(files, ChangeTypeEnum.Created, ev, changes);
                return;
            case BenchEvent.Deleted:
                Require(files, ChangeTypeEnum.Deleted, ev, changes);
                return;
            case BenchEvent.Renamed:
            case BenchEvent.Moved:
                if (hashing)
                    Require(files, ev == BenchEvent.Renamed ? ChangeTypeEnum.Renamed : ChangeTypeEnum.Moved, ev, changes);
                else {
                    Require(files, ChangeTypeEnum.Created, ev, changes);
                    Require(files, ChangeTypeEnum.Deleted, ev, changes);
                }

                return;
            case BenchEvent.Mixed:
                Require(files, ChangeTypeEnum.Created, ev, changes);
                Require(files, ChangeTypeEnum.Deleted, ev, changes);
                if (hashing) {
                    Require(files, ChangeTypeEnum.Changed, ev, changes);
                    Require(files, ChangeTypeEnum.Renamed, ev, changes);
                }

                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(ev), ev, "Unhandled benchmark event.");
        }
    }

    private static void ApplyMixed(string root, IReadOnlyList<string> files, long fileBytes)
    {
        var touched = Math.Max(4, files.Count / 100);
        var quarter = Math.Max(1, touched / 4);
        if (files.Count < quarter * 3)
            throw new InvalidOperationException($"Mixed needs at least {quarter * 3} files, got {files.Count}.");

        for (var i = 0; i < quarter; i++)
            RewriteSameLength(files[i]);

        for (var i = quarter; i < quarter * 2; i++)
            File.Delete(files[i]);

        for (var i = quarter * 2; i < quarter * 3; i++)
            MoveWithinDirectory(files[i], $"renamed-{i:D5}.bin");

        var destDir = Path.Combine(root, "dir-0000");
        for (var i = 0; i < quarter; i++)
            WritePadded(Path.Combine(destDir, $"added-{i:D5}.bin"), fileBytes, files.Count + i);
    }

    private static void WritePadded(string path, long size, int index)
    {
        var prefix = Encoding.UTF8.GetBytes($"id-{index:D8}\n");
        if (prefix.Length > size)
            throw new ArgumentOutOfRangeException(nameof(size), size, "File size is smaller than the unique prefix.");

        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        stream.Write(prefix);
        var remaining = size - prefix.Length;
        var pad = new byte[PadBufferSize];
        Array.Fill(pad, (byte)('a' + index % 26));
        while (remaining > 0) {
            var count = (int)Math.Min(remaining, pad.Length);
            stream.Write(pad, 0, count);
            remaining -= count;
        }
    }

    private static void WriteSparse(string path, long size, int index)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        stream.SetLength(size);
        stream.Position = 0;
        stream.Write(Encoding.UTF8.GetBytes($"id-{index:D8}"));
    }

    private static void RewriteSameLength(string path)
    {
        var length = new FileInfo(path).Length;
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None)) {
            var stamp = Encoding.UTF8.GetBytes($"changed-{DateTime.UtcNow.Ticks}");
            stream.Write(stamp, 0, (int)Math.Min(stamp.Length, length));
            stream.SetLength(length);
        }

        File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
    }

    private static void MoveWithinDirectory(string path, string newName)
    {
        var directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException($"File has no directory: {path}");
        File.Move(path, Path.Combine(directory, newName));
    }

    private static void Require(List<ChangeTypeEnum> files, ChangeTypeEnum expected, BenchEvent ev, IReadOnlyList<FileSystemChangeInfo> changes)
    {
        if (!files.Contains(expected))
            throw Fail(ev, changes);
    }

    private static InvalidOperationException Fail(BenchEvent ev, IReadOnlyList<FileSystemChangeInfo> changes)
    {
        var reported = changes.Count == 0 ? "(none)" : string.Join("; ", changes.Select(c => c.ToString()));
        return new($"Event {ev} produced unexpected changes: {reported}");
    }
}
