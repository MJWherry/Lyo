using Lyo.Exceptions;

namespace Lyo.FileSystemWatcher.Models;

/// <summary>
/// Diffs two persistable snapshot trees. Uses path, directory flag, size, fingerprint, and hash — never live <c>FileSystemInfo</c>.
/// </summary>
public static class FileSystemSnapshotDiffer
{
    /// <summary>Diffs two snapshots and returns create, delete, move, rename, and changed events for files and directories.</summary>
    public static IReadOnlyList<FileSystemChangeDto> DetectChanges(
        FileSystemSnapshotTreeDto oldTree,
        FileSystemSnapshotTreeDto newTree,
        DateTime? occurredAtUtc = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(oldTree);
        ArgumentHelpers.ThrowIfNull(newTree);
        var comparison = newTree.GetPathComparison();
        var occurred = occurredAtUtc ?? DateTime.UtcNow;
        var stringComparer = comparison == StringComparison.OrdinalIgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var changes = new List<FileSystemChangeDto>();
        var processedPaths = new HashSet<string>(stringComparer);
        var fileHashLookup = oldTree.EnumerateFiles()
            .Where(e => !e.Entry.IsDirectory && e.Entry.Hash != null)
            .GroupBy(e => e.Entry.Hash!, stringComparer)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Path).ToList(), stringComparer);

        var fileFingerprintLookup = oldTree.EnumerateFiles()
            .Where(e => !e.Entry.IsDirectory && e.Entry.Fingerprint != null)
            .GroupBy(e => e.Entry.Fingerprint!, stringComparer)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Path).ToList(), stringComparer);

        foreach (var (path, entry) in EnumerateNewSnapshotEntries(newTree.Root)) {
            ct.ThrowIfCancellationRequested();
            if (TryGetEntry(oldTree, path, out var value) && value != null) {
                if (!entry.IsDirectory && !value.IsDirectory && HasFileContentChanged(entry, value))
                    changes.Add(Create(path, path, FileSystemChangeKind.Changed, false, occurred));

                continue;
            }

            if (!entry.IsDirectory) {
                if (entry.Hash != null && fileHashLookup.TryGetValue(entry.Hash, out var oldPaths)) {
                    var oldPath = FindBestMatch(oldPaths, path, newTree, comparison);
                    if (oldPath != null) {
                        oldPaths.Remove(oldPath);
                        if (oldPaths.Count == 0)
                            fileHashLookup.Remove(entry.Hash);

                        processedPaths.Add(oldPath);
                        var isMove = !ParentsEqual(oldPath, path, comparison);
                        changes.Add(Create(oldPath, path, isMove ? FileSystemChangeKind.Moved : FileSystemChangeKind.Renamed, false, occurred));
                        continue;
                    }
                }

                if (entry.Fingerprint != null && entry.FileSize.HasValue && fileFingerprintLookup.TryGetValue(entry.Fingerprint, out var oldFingerprintPaths)) {
                    var matchingOldPaths = oldFingerprintPaths.Where(oldPath => {
                            if (oldTree.TryGetFile(oldPath, out var oldEntry) && oldEntry!.FileSize.HasValue)
                                return oldEntry.FileSize.Value == entry.FileSize.Value;

                            return false;
                        })
                        .ToList();

                    if (matchingOldPaths.Count > 0) {
                        var oldPath = FindBestMatch(matchingOldPaths, path, newTree, comparison);
                        if (oldPath != null) {
                            matchingOldPaths.Remove(oldPath);
                            if (matchingOldPaths.Count == 0)
                                fileFingerprintLookup.Remove(entry.Fingerprint);

                            processedPaths.Add(oldPath);
                            var isMove = !ParentsEqual(oldPath, path, comparison);
                            changes.Add(Create(oldPath, path, isMove ? FileSystemChangeKind.Moved : FileSystemChangeKind.Renamed, false, occurred));
                            continue;
                        }
                    }
                }

                changes.Add(Create(null, path, FileSystemChangeKind.Created, false, occurred));
                continue;
            }

            var oldDirMatch = FindDirectoryMatch(oldTree, newTree, path, processedPaths, comparison);
            if (oldDirMatch != null) {
                processedPaths.Add(oldDirMatch);
                var isMove = !ParentsEqual(oldDirMatch, path, comparison);
                var oldCounts = oldTree.GetSnapshotCounts(oldDirMatch);
                var newCounts = newTree.GetSnapshotCounts(path);
                changes.Add(
                    Create(
                        oldDirMatch, path, isMove ? FileSystemChangeKind.Moved : FileSystemChangeKind.Renamed, true, occurred, oldCounts.FileCount, oldCounts.DirectoryCount,
                        newCounts.FileCount, newCounts.DirectoryCount));
                continue;
            }

            var createdCounts = newTree.GetSnapshotCounts(path);
            changes.Add(Create(null, path, FileSystemChangeKind.Created, true, occurred, 0, 0, createdCounts.FileCount, createdCounts.DirectoryCount));
        }

        foreach (var (path, entry) in EnumerateOldSnapshotEntries(oldTree.Root)) {
            ct.ThrowIfCancellationRequested();
            if (processedPaths.Contains(path) || newTree.ContainsPath(path))
                continue;

            if (entry.IsDirectory) {
                var oldCounts = oldTree.GetSnapshotCounts(path);
                changes.Add(Create(path, null, FileSystemChangeKind.Deleted, true, occurred, oldCounts.FileCount, oldCounts.DirectoryCount, 0, 0));
            }
            else
                changes.Add(Create(path, null, FileSystemChangeKind.Deleted, false, occurred));
        }

        return changes;
    }

    private static bool HasFileContentChanged(FileSystemSnapshotEntryDto entry, FileSystemSnapshotEntryDto value)
    {
        var hasChanged = false;
        if (entry.FileSize.HasValue && value.FileSize.HasValue && entry.FileSize.Value != value.FileSize.Value)
            hasChanged = true;

        if (!hasChanged && entry.Fingerprint != null && value.Fingerprint != null && entry.Fingerprint != value.Fingerprint)
            hasChanged = true;

        if (!hasChanged && entry.Hash != null && value.Hash != null) {
            if (entry.Hash != value.Hash)
                hasChanged = true;
        }
        else if (!hasChanged && entry.Fingerprint != null && value.Fingerprint != null && entry.Fingerprint == value.Fingerprint)
            hasChanged = false;
        else if (entry.Hash != null && value.Hash != null && entry.Hash != value.Hash)
            hasChanged = true;

        return hasChanged;
    }

    private static bool TryGetEntry(FileSystemSnapshotTreeDto tree, string path, out FileSystemSnapshotEntryDto? entry)
    {
        if (tree.TryGetFile(path, out entry) && entry != null)
            return true;

        if (tree.TryGetDirectory(path, out var node) && node != null) {
            entry = new() { Path = node.RelativePath, IsDirectory = true };
            return true;
        }

        entry = null;
        return false;
    }

    private static IEnumerable<(string Path, FileSystemSnapshotEntryDto Entry)> EnumerateNewSnapshotEntries(FileSystemSnapshotDirectoryDto root)
    {
        foreach (var sub in root.Directories.Values) {
            yield return (sub.RelativePath, new() { Path = sub.RelativePath, IsDirectory = true });
            foreach (var pair in EnumerateNewSnapshotEntries(sub))
                yield return pair;
        }

        foreach (var fileEntry in root.Files.Values)
            yield return (fileEntry.Path, fileEntry);
    }

    private static IEnumerable<(string Path, FileSystemSnapshotEntryDto Entry)> EnumerateOldSnapshotEntries(FileSystemSnapshotDirectoryDto root)
        => EnumerateNewSnapshotEntries(root);

    private static string? FindBestMatch(List<string> candidates, string newPath, FileSystemSnapshotTreeDto newTree, StringComparison comparison)
    {
        if (candidates.Count == 0)
            return null;

        var validCandidates = candidates.Where(c => !newTree.ContainsPath(c)).ToList();
        if (validCandidates.Count == 0)
            return null;

        if (validCandidates.Count == 1)
            return validCandidates[0];

        var newFileName = GetFileName(newPath);
        return validCandidates.FirstOrDefault(c => string.Equals(GetFileName(c), newFileName, comparison)) ?? validCandidates[0];
    }

    private static string? FindDirectoryMatch(
        FileSystemSnapshotTreeDto oldTree,
        FileSystemSnapshotTreeDto newTree,
        string newPath,
        HashSet<string> processedPaths,
        StringComparison comparison)
    {
        var newDirName = GetFileName(newPath);
        var newParent = GetDirectoryName(newPath);
        var renameCandidate = oldTree.EnumerateDirectories()
            .Select(e => e.Path)
            .Where(e => string.Equals(GetDirectoryName(e), newParent, comparison))
            .FirstOrDefault(e => !newTree.ContainsPath(e) && !processedPaths.Contains(e));

        if (renameCandidate != null)
            return renameCandidate;

        return oldTree.EnumerateDirectories()
            .Select(e => e.Path)
            .Where(e => string.Equals(GetFileName(e), newDirName, comparison))
            .Where(e => !string.Equals(GetDirectoryName(e), newParent, comparison))
            .FirstOrDefault(e => !newTree.ContainsPath(e) && !processedPaths.Contains(e));
    }

    private static bool ParentsEqual(string a, string b, StringComparison comparison) => string.Equals(GetDirectoryName(a), GetDirectoryName(b), comparison);

    private static string GetFileName(string relativePath)
    {
        var normalized = FileSystemPathGlob.Normalize(relativePath);
        var slash = normalized.LastIndexOf('/');
        return slash < 0 ? normalized : normalized.Substring(slash + 1);
    }

    private static string GetDirectoryName(string relativePath)
    {
        var normalized = FileSystemPathGlob.Normalize(relativePath);
        var slash = normalized.LastIndexOf('/');
        return slash < 0 ? "" : normalized.Substring(0, slash);
    }

    private static FileSystemChangeDto Create(
        string? oldPath,
        string? newPath,
        FileSystemChangeKind kind,
        bool isDirectory,
        DateTime occurredAtUtc,
        int? oldFileCount = null,
        int? oldDirCount = null,
        int? newFileCount = null,
        int? newDirCount = null)
        => new() {
            OldPath = oldPath,
            NewPath = newPath,
            ChangeType = kind,
            IsDirectory = isDirectory,
            OldFileCount = oldFileCount,
            OldDirectoryCount = oldDirCount,
            NewFileCount = newFileCount,
            NewDirCount = newDirCount,
            OccurredAtUtc = occurredAtUtc
        };
}
