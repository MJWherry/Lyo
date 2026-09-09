using Lyo.Common.Core.Enums;
using Lyo.Exceptions;
using Lyo.FileSystemWatcher.Enums;
using Lyo.FileSystemWatcher.Models;
using Lyo.Hashing;
using Lyo.Hashing.Files;

namespace Lyo.FileSystemWatcher;

/// <summary>Helpers for snapshotting a directory tree, diffing two snapshots, hashing, and fingerprints.</summary>
public static class Utilities
{
    /// <summary>
    /// Walks <paramref name="path" /> recursively and builds a <see cref="SnapshotTree" />, optionally hashing and reusing data from <paramref name="oldTree" />.
    /// </summary>
    /// <param name="path">Root directory to scan.</param>
    /// <param name="enableHashing">If true, fingerprints and hashes each file for move detection. Slower.</param>
    /// <param name="pathComparison">How path segments are compared in the dictionaries.</param>
    /// <param name="ct">Token that cancels the walk.</param>
    /// <param name="oldTree">Previous snapshot. When hashing is on, metadata may be reused for unchanged files.</param>
    /// <remarks>This overload always walks subdirectories. Prefer <see cref="TakeSnapshot(string, FileSystemWatcherOptions, CancellationToken, SnapshotTree?)" /> to honor include/exclude regexes and <c>IncludeSubdirectories</c>.</remarks>
    public static SnapshotTree TakeSnapshot(
        string path,
        bool enableHashing = true,
        StringComparison pathComparison = StringComparison.OrdinalIgnoreCase,
        CancellationToken ct = default,
        SnapshotTree? oldTree = null)
        => TakeSnapshot(path, new FileSystemWatcherOptions { EnableFileHashing = enableHashing, PathComparison = pathComparison, IncludeSubdirectories = true }, ct, oldTree);

    /// <summary>
    /// Walks <paramref name="path" /> according to <paramref name="options" /> (recursion, include/exclude regexes, hashing) and builds a <see cref="SnapshotTree" />.
    /// </summary>
    public static SnapshotTree TakeSnapshot(string path, FileSystemWatcherOptions options, CancellationToken ct = default, SnapshotTree? oldTree = null)
    {
        ArgumentHelpers.ThrowIfNull(options);
        var enableHashing = options.EnableFileHashing;
        var pathComparison = options.PathComparison;
        var segmentComparer = pathComparison == StringComparison.OrdinalIgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var sizeLookup = new HashSet<long>();
        var fingerprintLookup = new Dictionary<string, HashSet<long>>();
        if (enableHashing && oldTree != null) {
            foreach (var (_, entry) in oldTree.EnumerateFiles()) {
                if (entry.Info is not FileInfo _ || !entry.FileSize.HasValue)
                    continue;

                sizeLookup.Add(entry.FileSize.Value);
                if (entry.Fingerprint == null)
                    continue;

                if (!fingerprintLookup.TryGetValue(entry.Fingerprint, out var sizeSet)) {
                    sizeSet = [];
                    fingerprintLookup[entry.Fingerprint] = sizeSet;
                }

                sizeSet.Add(entry.FileSize.Value);
            }
        }

        var root = new SnapshotDirectoryNode(null, "", path, segmentComparer);
        var fileCount = 0;
        var directoryCount = 0;
        WalkDirectory(root, path, options, oldTree, ct, sizeLookup, fingerprintLookup, segmentComparer, isRoot: true, ref fileCount, ref directoryCount);
        return new(path, pathComparison, root, fileCount, directoryCount);
    }

    private static void WalkDirectory(
        SnapshotDirectoryNode node,
        string rootPath,
        FileSystemWatcherOptions options,
        SnapshotTree? oldTree,
        CancellationToken ct,
        HashSet<long> sizeLookup,
        Dictionary<string, HashSet<long>> fingerprintLookup,
        IEqualityComparer<string> segmentComparer,
        bool isRoot,
        ref int fileCount,
        ref int directoryCount)
    {
        var enableHashing = options.EnableFileHashing;
        var recurse = options.IncludeSubdirectories || isRoot;
        string[] subdirs;
        try {
            subdirs = recurse ? Directory.GetDirectories(node.FullPath) : [];
        }
        catch {
            subdirs = [];
        }

        foreach (var dirPath in subdirs) {
            ct.ThrowIfCancellationRequested();
            var name = Path.GetFileName(dirPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrEmpty(name))
                continue;

            var relativeDir = FileSystemWatcherMapping.ToRelativePath(rootPath, dirPath, options.PathComparison);
            if (FileSystemPathGlob.IsDirectoryExcluded(relativeDir, options.ExcludePatterns, options.PathComparison))
                continue;

            var child = new SnapshotDirectoryNode(node, name, dirPath, segmentComparer);
            node.Directories[name] = child;
            directoryCount++;
            if (options.IncludeSubdirectories)
                WalkDirectory(child, rootPath, options, oldTree, ct, sizeLookup, fingerprintLookup, segmentComparer, isRoot: false, ref fileCount, ref directoryCount);
        }

        string[] files;
        try {
            files = Directory.GetFiles(node.FullPath);
        }
        catch {
            files = [];
        }

        foreach (var file in files) {
            ct.ThrowIfCancellationRequested();
            var relativeFile = FileSystemWatcherMapping.ToRelativePath(rootPath, file, options.PathComparison);
            if (!FileSystemPathGlob.IsFileIncluded(relativeFile, options.IncludePatterns, options.ExcludePatterns, options.PathComparison))
                continue;

            var fileInfo = new FileInfo(file);
            string? hash = null;
            string? fingerprint = null;
            long? fileSize = null;
            var canReuseFromOld = false;
            var metadataOnlyFingerprint = false;
            DirectorySnapshotEntry? oldEntry = null;
            if (enableHashing && oldTree != null && oldTree.TryGetFile(file, out oldEntry) && oldEntry != null) {
                var sameSize = oldEntry.FileSize.HasValue && oldEntry.FileSize.Value == fileInfo.Length;
                var sameModTime = oldEntry.Info is FileInfo ofi && ofi.LastWriteTimeUtc == fileInfo.LastWriteTimeUtc;
                canReuseFromOld = sameSize && sameModTime;
                metadataOnlyFingerprint = !canReuseFromOld && sameSize;
            }

            if (canReuseFromOld && oldEntry != null) {
                fingerprint = oldEntry.Fingerprint;
                hash = oldEntry.Hash;
                fileSize = oldEntry.FileSize;
            }
            else if (metadataOnlyFingerprint) {
                fileSize = fileInfo.Length;
                fingerprint = SparseFileFingerprinter.MetadataOnlyHex(fileInfo.Length, fileInfo.LastWriteTimeUtc);
            }
            else if (enableHashing) {
                fileSize = fileInfo.Length;
                try {
                    var fingerprintBytes = SparseFileFingerprinter.FingerprintAsync(file, fileSize.Value, ct).GetAwaiter().GetResult();
                    if (fingerprintBytes != null && fingerprintBytes.Length > 0)
                        fingerprint = HexEncoding.ToHexString(fingerprintBytes);
                }
                catch { /* Ignore fingerprint failures */
                }

                var needsHash = false;
                if (oldTree != null) {
                    if (sizeLookup.Contains(fileSize.Value))
                        needsHash = true;
                    else if (fingerprint != null && fingerprintLookup.TryGetValue(fingerprint, out var sizes) && sizes.Contains(fileSize.Value))
                        needsHash = true;

                    if (needsHash && fingerprint != null && fingerprintLookup.TryGetValue(fingerprint, out var fpSizes) && fpSizes.Contains(fileSize.Value))
                        needsHash = false;
                }

                if (needsHash) {
                    try {
                        var hashBytes = Hash(file, ct);
                        if (hashBytes.Length > 0)
                            hash = HexEncoding.ToHexString(hashBytes);
                    }
                    catch (OperationCanceledException) {
                        throw;
                    }
                    catch { /* Ignore hash failures */
                    }
                }
            }

            var fileName = Path.GetFileName(file);
            if (string.IsNullOrEmpty(fileName))
                continue;

            node.Files[fileName] = new(file, fileInfo, hash, fingerprint, fileSize);
            fileCount++;
        }
    }

    /// <summary>Diffs two snapshots and returns create, delete, move, rename, and changed events for files and directories.</summary>
    /// <param name="oldTree">Previous snapshot.</param>
    /// <param name="newTree">Current snapshot.</param>
    /// <param name="pathComparison">How paths are matched.</param>
    /// <param name="ct">Token that cancels the diff.</param>
    public static List<FileSystemChangeInfo> DetectChanges(
        SnapshotTree oldTree,
        SnapshotTree newTree,
        StringComparison pathComparison = StringComparison.OrdinalIgnoreCase,
        CancellationToken ct = default)
    {
        var stringComparer = pathComparison == StringComparison.OrdinalIgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var changes = new List<FileSystemChangeInfo>();
        var processedPaths = new HashSet<string>(stringComparer);
        var fileHashLookup = oldTree.EnumerateFiles()
            .Where(e => e.Entry.Info is FileInfo && e.Entry.Hash != null)
            .GroupBy(e => e.Entry.Hash!)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Path).ToList());

        var fileFingerprintLookup = oldTree.EnumerateFiles()
            .Where(e => e.Entry.Info is FileInfo && e.Entry.Fingerprint != null)
            .GroupBy(e => e.Entry.Fingerprint!)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Path).ToList());

        foreach (var (path, entry) in EnumerateNewSnapshotEntries(newTree.Root)) {
            ct.ThrowIfCancellationRequested();
            if (TryGetSnapshotEntry(oldTree, path, out var value) && value != null) {
                if (entry.Info is FileInfo _ && value.Info is FileInfo _) {
                    var hasChanged = false;
                    if (entry.FileSize.HasValue && value.FileSize.HasValue) {
                        if (entry.FileSize.Value != value.FileSize.Value)
                            hasChanged = true;
                    }

                    if (!hasChanged && entry.Fingerprint != null && value.Fingerprint != null) {
                        if (entry.Fingerprint != value.Fingerprint)
                            hasChanged = true;
                    }

                    if (!hasChanged && entry.Hash != null && value.Hash != null) {
                        if (entry.Hash != value.Hash)
                            hasChanged = true;
                    }
                    else if (!hasChanged && entry.Fingerprint != null && value.Fingerprint != null && entry.Fingerprint == value.Fingerprint)
                        hasChanged = false;
                    else if (entry.Hash != null && value.Hash != null && entry.Hash != value.Hash)
                        hasChanged = true;

                    if (hasChanged)
                        changes.Add(new(path, path, ChangeTypeEnum.Changed, false));
                }

                continue;
            }

            var isDirectory = entry.Info is DirectoryInfo;
            if (!isDirectory) {
                if (entry.Hash != null && fileHashLookup.TryGetValue(entry.Hash, out var oldPaths)) {
                    var oldPath = FindBestMatch(oldPaths, path, newTree, pathComparison);
                    if (oldPath != null) {
                        oldPaths.Remove(oldPath);
                        if (oldPaths.Count == 0)
                            fileHashLookup.Remove(entry.Hash);

                        processedPaths.Add(oldPath);
                        var isMove = Path.GetDirectoryName(oldPath) != Path.GetDirectoryName(path);
                        changes.Add(new(oldPath, path, isMove ? ChangeTypeEnum.Moved : ChangeTypeEnum.Renamed, false));
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
                        var oldPath = FindBestMatch(matchingOldPaths, path, newTree, pathComparison);
                        if (oldPath != null) {
                            matchingOldPaths.Remove(oldPath);
                            if (matchingOldPaths.Count == 0)
                                fileFingerprintLookup.Remove(entry.Fingerprint);

                            processedPaths.Add(oldPath);
                            var isMove = Path.GetDirectoryName(oldPath) != Path.GetDirectoryName(path);
                            changes.Add(new(oldPath, path, isMove ? ChangeTypeEnum.Moved : ChangeTypeEnum.Renamed, false));
                            continue;
                        }
                    }
                }
            }

            if (isDirectory) {
                var oldDirMatch = FindDirectoryMatch(oldTree, newTree, entry, processedPaths, pathComparison);
                if (oldDirMatch != null) {
                    processedPaths.Add(oldDirMatch);
                    var isMove = Path.GetDirectoryName(oldDirMatch) != Path.GetDirectoryName(path);
                    var oldCounts = GetSnapshotCounts(oldDirMatch, oldTree, pathComparison);
                    var newCounts = GetDirectoryContentCounts(path);
                    changes.Add(
                        new(
                            oldDirMatch, path, isMove ? ChangeTypeEnum.Moved : ChangeTypeEnum.Renamed, true, oldCounts.fileCount, oldCounts.dirCount, newCounts.fileCount,
                            newCounts.dirCount));

                    continue;
                }

                var counts = GetDirectoryContentCounts(path);
                changes.Add(new(null, path, ChangeTypeEnum.Created, true, 0, 0, counts.fileCount, counts.dirCount));
            }
            else
                changes.Add(new(null, path, ChangeTypeEnum.Created, false));
        }

        foreach (var (path, entry) in oldTree.EnumerateDirectoryAndFileEntries()) {
            ct.ThrowIfCancellationRequested();
            if (processedPaths.Contains(path) || newTree.ContainsPath(path))
                continue;

            if (entry.Info is DirectoryInfo) {
                var oldCounts = GetSnapshotCounts(path, oldTree, pathComparison);
                changes.Add(new(path, null, ChangeTypeEnum.Deleted, true, oldCounts.fileCount, oldCounts.dirCount, 0, 0));
            }
            else
                changes.Add(new(path, null, ChangeTypeEnum.Deleted, false));
        }

        return changes;
    }

    private static bool TryGetSnapshotEntry(SnapshotTree tree, string path, out DirectorySnapshotEntry? entry)
    {
        if (tree.TryGetFile(path, out entry) && entry != null)
            return true;

        if (tree.TryGetDirectory(path, out var node) && node != null) {
            entry = new(node.FullPath, new DirectoryInfo(node.FullPath));
            return true;
        }

        entry = null;
        return false;
    }

    /// <summary>Depth-first: each subdirectory path, then files in that directory. The snapshot root is omitted.</summary>
    private static IEnumerable<(string Path, DirectorySnapshotEntry Entry)> EnumerateNewSnapshotEntries(SnapshotDirectoryNode root)
    {
        foreach (var sub in root.Directories.Values) {
            yield return (sub.FullPath, new(sub.FullPath, new DirectoryInfo(sub.FullPath)));

            foreach (var pair in EnumerateNewSnapshotEntries(sub))
                yield return pair;
        }

        foreach (var fileEntry in root.Files.Values)
            yield return (fileEntry.Path, fileEntry);
    }

    /// <summary>Picks the best prior path for a file that looks moved or renamed, using hash or fingerprint candidates.</summary>
    public static string? FindBestMatch(List<string> candidates, string newPath, SnapshotTree newTree, StringComparison pathComparison = StringComparison.OrdinalIgnoreCase)
    {
        if (candidates.Count == 0)
            return null;

        var validCandidates = candidates.Where(c => !newTree.ContainsPath(c)).ToList();
        if (validCandidates.Count == 0)
            return null;

        if (validCandidates.Count == 1)
            return validCandidates[0];

        var newFileName = Path.GetFileName(newPath);
        return validCandidates.FirstOrDefault(c => string.Equals(Path.GetFileName(c), newFileName, pathComparison)) ?? validCandidates[0];
    }

    /// <summary>Heuristic that matches a new directory path to a missing old directory path, used for rename and move detection.</summary>
    public static string? FindDirectoryMatch(
        SnapshotTree oldTree,
        SnapshotTree newTree,
        DirectorySnapshotEntry newEntry,
        HashSet<string> processedPaths,
        StringComparison pathComparison = StringComparison.OrdinalIgnoreCase)
    {
        var newDirName = Path.GetFileName(newEntry.Path);
        var newParent = Path.GetDirectoryName(newEntry.Path) ?? "";
        var renameCandidate = oldTree.EnumerateDirectoryNodes()
            .Select(n => n.FullPath)
            .Where(e => string.Equals(Path.GetDirectoryName(e) ?? "", newParent, pathComparison))
            .FirstOrDefault(e => !newTree.ContainsPath(e) && !processedPaths.Contains(e));

        if (renameCandidate != null)
            return renameCandidate;

        return oldTree.EnumerateDirectoryNodes()
            .Select(n => n.FullPath)
            .Where(e => string.Equals(Path.GetFileName(e), newDirName, pathComparison))
            .Where(e => !string.Equals(Path.GetDirectoryName(e) ?? "", newParent, pathComparison))
            .FirstOrDefault(e => !newTree.ContainsPath(e) && !processedPaths.Contains(e));
    }

    /// <summary>True if immediate children or file-content fingerprints under <paramref name="dirPath" /> differ between the two snapshots.</summary>
    public static bool HasDirectoryChanged(string dirPath, SnapshotTree oldTree, SnapshotTree newTree, StringComparison pathComparison = StringComparison.OrdinalIgnoreCase)
    {
        var stringComparer = pathComparison == StringComparison.OrdinalIgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        oldTree.TryGetDirectory(dirPath, out var oldNode);
        newTree.TryGetDirectory(dirPath, out var newNode);
        var oldChildren = new HashSet<string>(GetDirectChildPaths(oldNode, stringComparer), stringComparer);
        var newChildren = new HashSet<string>(GetDirectChildPaths(newNode, stringComparer), stringComparer);
        if (!oldChildren.SetEquals(newChildren))
            return true;

        return oldChildren.Any(child => {
            if (!oldTree.TryGetFile(child, out var oldEntry) || !newTree.TryGetFile(child, out var newEntry) || oldEntry!.Info is not FileInfo || newEntry!.Info is not FileInfo)
                return false;

            if (oldEntry.Hash != null && newEntry.Hash != null)
                return oldEntry.Hash != newEntry.Hash;

            return oldEntry.Fingerprint != null && newEntry.Fingerprint != null && oldEntry.Fingerprint != newEntry.Fingerprint;
        });
    }

    private static IEnumerable<string> GetDirectChildPaths(SnapshotDirectoryNode? node, StringComparer comparer)
    {
        if (node == null)
            yield break;

        foreach (var fn in node.Files.Keys.OrderBy(x => x, comparer))
            yield return Path.Combine(node.FullPath, fn);

        foreach (var sub in node.Directories.Values.OrderBy(x => x.FullPath, comparer))
            yield return sub.FullPath;
    }

    /// <summary>Counts files and subdirectories under <paramref name="directoryPath" /> by enumerating the live file system.</summary>
    public static (int fileCount, int dirCount) GetDirectoryContentCounts(string directoryPath)
    {
        try {
            return !Directory.Exists(directoryPath)
                ? (0, 0)
                : (Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories).Length, Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories).Length);
        }
        catch {
            return (0, 0);
        }
    }

    /// <summary>Counts files and descendant directories under <paramref name="directoryPath" /> as stored in <paramref name="snapshot" />.</summary>
    public static (int fileCount, int dirCount) GetSnapshotCounts(string directoryPath, SnapshotTree snapshot, StringComparison pathComparison = StringComparison.OrdinalIgnoreCase)
    {
        if (!snapshot.TryGetDirectory(directoryPath, out var node) || node is null)
            return (0, 0);

        var fileCount = 0;
        var dirCount = 0;
        CountDescendants(node, ref fileCount, ref dirCount);
        return (fileCount, dirCount);
    }

    private static void CountDescendants(SnapshotDirectoryNode node, ref int fileCount, ref int dirCount)
    {
        fileCount += node.Files.Count;
        foreach (var sub in node.Directories.Values) {
            dirCount++;
            CountDescendants(sub, ref fileCount, ref dirCount);
        }
    }

    /// <summary>Computes MD5 of the file at <paramref name="path" />. Returns an empty array when access fails.</summary>
    public static byte[] Hash(string path, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        ArgumentHelpers.ThrowIfFileNotFound(path);
        try {
            using var stream = File.OpenRead(path);
            ct.ThrowIfCancellationRequested();
            return Hasher.ComputeMd5(stream);
        }
        catch (UnauthorizedAccessException) {
            return [];
        }
        catch (IOException) {
            return [];
        }
    }

    /// <summary>
    /// Builds a light fingerprint with sparse sampling. Small files: first and last bytes. Medium files: beginning, middle, and end. Very large files: beginning only plus
    /// modification time, to avoid expensive seeks. Much faster than hashing the whole file, and still good enough for move, rename, and change detection.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <param name="fileSize">File size in bytes.</param>
    /// <param name="ct">Token that cancels the read.</param>
    /// <returns>MD5 of (size plus sampled bytes), or null if the file does not exist.</returns>
    /// <param name="options">Optional sparse-fingerprint knobs. Forwarded to <see cref="SparseFileFingerprinter.FingerprintAsync" />.</param>
    /// <exception cref="UnauthorizedAccessException">Raised when access to the file is denied.</exception>
    /// <exception cref="IOException">Raised when the file cannot be read because of I/O errors.</exception>
    public static Task<byte[]?> Fingerprint(string path, long fileSize, CancellationToken ct = default, FileFingerprintOptions? options = null)
        => SparseFileFingerprinter.FingerprintAsync(path, fileSize, ct, options);

    /// <inheritdoc cref="SparseFileFingerprinter.MetadataOnlyHex" />
    public static string MetadataOnlyFingerprintHex(long fileSize, DateTime lastWriteTimeUtc) => SparseFileFingerprinter.MetadataOnlyHex(fileSize, lastWriteTimeUtc);

    /// <summary>Lowercase hex. Legacy shape for this module.</summary>
    public static string ToHexString(byte[] bytes) => HexEncoding.ToHexString(bytes, TextLetterCase.Lower);
}