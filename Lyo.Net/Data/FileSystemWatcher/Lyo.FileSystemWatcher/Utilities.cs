using System.Security.Cryptography;
using System.Text;
using Lyo.Exceptions;
using Lyo.FileSystemWatcher.Enums;

namespace Lyo.FileSystemWatcher;

public static class Utilities
{
    public static SnapshotTree TakeSnapshot(
        string path,
        bool enableHashing = true,
        StringComparison pathComparison = StringComparison.OrdinalIgnoreCase,
        CancellationToken ct = default,
        SnapshotTree? oldTree = null)
    {
        var segmentComparer = pathComparison == StringComparison.OrdinalIgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var sizeLookup = new HashSet<long>();
        var fingerprintLookup = new Dictionary<string, HashSet<long>>();
        if (enableHashing && oldTree != null) {
            foreach (var (_, entry) in oldTree.EnumerateFiles()) {
                if (entry.Info is FileInfo fileInfo && entry.FileSize.HasValue) {
                    sizeLookup.Add(entry.FileSize.Value);
                    if (entry.Fingerprint != null) {
                        if (!fingerprintLookup.TryGetValue(entry.Fingerprint, out var sizeSet)) {
                            sizeSet = new HashSet<long>();
                            fingerprintLookup[entry.Fingerprint] = sizeSet;
                        }

                        sizeSet.Add(entry.FileSize.Value);
                    }
                }
            }
        }

        var root = new SnapshotDirectoryNode(null, "", path, segmentComparer);
        var fileCount = 0;
        var directoryCount = 0;
        WalkDirectory(root, enableHashing, pathComparison, oldTree, ct, sizeLookup, fingerprintLookup, segmentComparer, ref fileCount, ref directoryCount);
        return new SnapshotTree(path, pathComparison, root, fileCount, directoryCount);
    }

    private static void WalkDirectory(
        SnapshotDirectoryNode node,
        bool enableHashing,
        StringComparison pathComparison,
        SnapshotTree? oldTree,
        CancellationToken ct,
        HashSet<long> sizeLookup,
        Dictionary<string, HashSet<long>> fingerprintLookup,
        IEqualityComparer<string> segmentComparer,
        ref int fileCount,
        ref int directoryCount)
    {
        string[] subdirs;
        try {
            subdirs = Directory.GetDirectories(node.FullPath);
        }
        catch {
            subdirs = [];
        }

        foreach (var dirPath in subdirs) {
            ct.ThrowIfCancellationRequested();
            var name = Path.GetFileName(dirPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrEmpty(name))
                continue;

            var child = new SnapshotDirectoryNode(node, name, dirPath, segmentComparer);
            node.Directories[name] = child;
            directoryCount++;
            WalkDirectory(child, enableHashing, pathComparison, oldTree, ct, sizeLookup, fingerprintLookup, segmentComparer, ref fileCount, ref directoryCount);
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
                fingerprint = FingerprintMetadataOnly(fileInfo.Length, fileInfo.LastWriteTimeUtc);
            }
            else if (enableHashing) {
                fileSize = fileInfo.Length;

                try {
                    var fingerprintBytes = Fingerprint(file, fileSize.Value, ct).GetAwaiter().GetResult();
                    if (fingerprintBytes != null && fingerprintBytes.Length > 0) {
#if NET9_0_OR_GREATER
                        fingerprint = Convert.ToHexString(fingerprintBytes);
#else
                        fingerprint = ToHexString(fingerprintBytes);
#endif
                    }
                }
                catch { /* Ignore fingerprint failures */
                }

                var needsHash = false;
                if (oldTree != null && fileSize.HasValue) {
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
                        if (hashBytes.Length > 0) {
#if NET9_0_OR_GREATER
                            hash = Convert.ToHexString(hashBytes);
#else
                            hash = ToHexString(hashBytes);
#endif
                        }
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

            node.Files[fileName] = new DirectorySnapshotEntry(file, fileInfo, hash, fingerprint, fileSize);
            fileCount++;
        }
    }

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
                if (entry.Info is FileInfo fileInfo && value.Info is FileInfo oldFileInfo) {
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
            entry = new DirectorySnapshotEntry(node.FullPath, new DirectoryInfo(node.FullPath));
            return true;
        }

        entry = null;
        return false;
    }

    /// <summary>Depth-first: subdirectories (each path), then files in each directory. Omits the snapshot root.</summary>
    private static IEnumerable<(string Path, DirectorySnapshotEntry Entry)> EnumerateNewSnapshotEntries(SnapshotDirectoryNode root)
    {
        foreach (var sub in root.Directories.Values) {
            yield return (sub.FullPath, new DirectorySnapshotEntry(sub.FullPath, new DirectoryInfo(sub.FullPath)));
            foreach (var pair in EnumerateNewSnapshotEntries(sub))
                yield return pair;
        }

        foreach (var fileEntry in root.Files.Values)
            yield return (fileEntry.Path, fileEntry);
    }

    public static string? FindBestMatch(
        List<string> candidates,
        string newPath,
        SnapshotTree newTree,
        StringComparison pathComparison = StringComparison.OrdinalIgnoreCase)
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

    public static bool HasDirectoryChanged(
        string dirPath,
        SnapshotTree oldTree,
        SnapshotTree newTree,
        StringComparison pathComparison = StringComparison.OrdinalIgnoreCase)
    {
        var stringComparer = pathComparison == StringComparison.OrdinalIgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        oldTree.TryGetDirectory(dirPath, out var oldNode);
        newTree.TryGetDirectory(dirPath, out var newNode);

        var oldChildren = new HashSet<string>(GetDirectChildPaths(oldNode, stringComparer), stringComparer);
        var newChildren = new HashSet<string>(GetDirectChildPaths(newNode, stringComparer), stringComparer);

        if (!oldChildren.SetEquals(newChildren))
            return true;

        return oldChildren.Any(child => {
            if (!oldTree.TryGetFile(child, out var oldEntry) || !newTree.TryGetFile(child, out var newEntry) || oldEntry!.Info is not FileInfo ||
                newEntry!.Info is not FileInfo)
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

    public static (int fileCount, int dirCount) GetSnapshotCounts(
        string directoryPath,
        SnapshotTree snapshot,
        StringComparison pathComparison = StringComparison.OrdinalIgnoreCase)
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

    public static byte[] Hash(string path, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path, nameof(path));
        ArgumentHelpers.ThrowIfFileNotFound(path, nameof(path));
        try {
            using var stream = File.OpenRead(path);
            ct.ThrowIfCancellationRequested();
            using var md5 = MD5.Create();
            return md5.ComputeHash(stream);
        }
        catch (UnauthorizedAccessException) {
            return [];
        }
        catch (IOException) {
            return [];
        }
    }

    /// <summary>
    /// Creates a lightweight fingerprint for files using sparse sampling. For small files: reads first and last bytes. For medium files: reads beginning, middle, and end. For
    /// very large files: reads beginning only and uses modification time to avoid expensive disk seeks. This is much faster than hashing the entire file while still providing good
    /// move/rename and change detection.
    /// </summary>
    /// <param name="path">Path to the file</param>
    /// <param name="fileSize">Size of the file in bytes</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>MD5 hash of (size + sampled bytes), or null if the file does not exist</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the file cannot be accessed due to access restrictions</exception>
    /// <exception cref="IOException">Thrown when the file cannot be accessed due to I/O errors</exception>
    public static Task<byte[]?> Fingerprint(string path, long fileSize, CancellationToken ct = default)
    {
        ExceptionThrower.ThrowIfDirectoryNotAccessible(path, nameof(path));
        if (!File.Exists(path))
            return Task.FromResult<byte[]?>(null);

        const long largeFileThreshold = 100 * 1024 * 1024; // 100MB
        const long veryLargeThreshold = 1024 * 1024 * 1024; // 1GB
        const int sampleSize = 128; // Smaller chunks for faster I/O
        const int veryLargeSampleSize = 64; // Minimal read for very large files

        if (fileSize > veryLargeThreshold) {
            var fileInfo = new FileInfo(path);
            var combined = new byte[8 + veryLargeSampleSize + 8];
            var sizeBytes = BitConverter.GetBytes(fileSize);
            var modTimeBytes = BitConverter.GetBytes(fileInfo.LastWriteTimeUtc.Ticks);
            Array.Copy(sizeBytes, 0, combined, 0, 8);
            Array.Copy(modTimeBytes, 0, combined, combined.Length - 8, 8);
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, veryLargeSampleSize * 2)) {
                var firstRead = stream.Read(combined, 8, veryLargeSampleSize);
                if (firstRead < veryLargeSampleSize)
                    Array.Clear(combined, 8 + firstRead, veryLargeSampleSize - firstRead);
            }

            using var md5 = MD5.Create();
            return Task.FromResult<byte[]?>(md5.ComputeHash(combined));
        }

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, sampleSize * 2);
        ct.ThrowIfCancellationRequested();
        var firstBytes = new byte[sampleSize];
        var firstBytesRead = fs.Read(firstBytes, 0, sampleSize);
        var middleBytesRead = 0;
        var lastBytesRead = 0;
        byte[]? middleBytes = null;
        byte[]? lastBytes = null;
        if (fileSize > largeFileThreshold) {
            if (fileSize > sampleSize * 2) {
                middleBytes = new byte[sampleSize];
                fs.Seek(fileSize / 2, SeekOrigin.Begin);
                middleBytesRead = fs.Read(middleBytes, 0, sampleSize);
            }

            if (fileSize > sampleSize) {
                lastBytes = new byte[sampleSize];
                fs.Seek(-sampleSize, SeekOrigin.End);
                lastBytesRead = fs.Read(lastBytes, 0, sampleSize);
            }
        }
        else if (fileSize > sampleSize) {
            lastBytes = new byte[sampleSize];
            fs.Seek(-sampleSize, SeekOrigin.End);
            lastBytesRead = fs.Read(lastBytes, 0, sampleSize);
        }

        var capacity = 8 + firstBytesRead + middleBytesRead + lastBytesRead;
        var combined2 = new byte[capacity];
        var offset = 0;
        var sizeBytes2 = BitConverter.GetBytes(fileSize);
        Array.Copy(sizeBytes2, 0, combined2, offset, 8);
        offset += 8;
        Array.Copy(firstBytes, 0, combined2, offset, firstBytesRead);
        offset += firstBytesRead;
        if (middleBytes != null && middleBytesRead > 0) {
            Array.Copy(middleBytes, 0, combined2, offset, middleBytesRead);
            offset += middleBytesRead;
        }

        if (lastBytes != null && lastBytesRead > 0)
            Array.Copy(lastBytes, 0, combined2, offset, lastBytesRead);

        using var md52 = MD5.Create();
        return Task.FromResult<byte[]?>(md52.ComputeHash(combined2));
    }

    /// <summary>Creates a fingerprint from file metadata only (size + mod time). No file I/O. Used when metadata changed (e.g. touch) for same path.</summary>
    private static string FingerprintMetadataOnly(long fileSize, DateTime lastWriteTimeUtc)
    {
        var combined = new byte[16];
        BitConverter.GetBytes(fileSize).CopyTo(combined, 0);
        BitConverter.GetBytes(lastWriteTimeUtc.Ticks).CopyTo(combined, 8);
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(combined);
#if NET9_0_OR_GREATER
        return Convert.ToHexString(hash);
#else
        return ToHexString(hash);
#endif
    }

    public static string ToHexString(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append($"{b:x2}");

        return sb.ToString();
    }
}
