using Lyo.Exceptions;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

/// <summary>Where a tree file sits after joining QueryProject rows to one storage-key LIST. Not a live HEAD.</summary>
public enum FileStoragePresence
{
    /// <summary>LIST failed or was skipped; the metadata row is the only signal.</summary>
    Unknown = 0,

    /// <summary>Metadata row and a matching listed object key.</summary>
    Both = 1,

    /// <summary>Metadata row whose expected key is not in the LIST.</summary>
    MissingBlob = 2,

    /// <summary>Listed object with no matching metadata row.</summary>
    CloudOnly = 3
}

/// <summary>Joins listed storage keys to expected metadata keys (exact or suffix). Does not call FileStorage per file.</summary>
public static class FileStorageStorageKeyJoin
{
    /// <summary>True when <paramref name="expected" /> is in <paramref name="listedKeys" /> exactly or as a <c>.../{expected}</c> suffix.</summary>
    /// <param name="listedKeys">Keys from one diagnostics LIST.</param>
    /// <param name="expected">Metadata-derived object key.</param>
    public static bool KeyExists(IReadOnlyCollection<string> listedKeys, string? expected)
    {
        ArgumentHelpers.ThrowIfNull(listedKeys);
        if (string.IsNullOrWhiteSpace(expected) || listedKeys.Count == 0)
            return false;

        if (listedKeys.Contains(expected))
            return true;

        foreach (var key in listedKeys) {
            if (key.Length > expected.Length && key.EndsWith(expected, StringComparison.Ordinal) && key[key.Length - expected.Length - 1] == '/')
                return true;
        }

        return false;
    }

    /// <summary>Sets <see cref="FileStoragePathTreeRow.Presence" /> from one LIST. Null <paramref name="listedKeys" /> means diagnostics were unavailable.</summary>
    /// <param name="rows">QueryProject file rows.</param>
    /// <param name="listedKeys">Listed object keys, or null when LIST failed.</param>
    public static List<FileStoragePathTreeRow> ApplyPresence(IReadOnlyList<FileStoragePathTreeRow> rows, IReadOnlyCollection<string>? listedKeys)
    {
        ArgumentHelpers.ThrowIfNull(rows);
        if (listedKeys == null) {
            var unknown = new List<FileStoragePathTreeRow>(rows.Count);
            foreach (var row in rows)
                unknown.Add(row with { Presence = FileStoragePresence.Unknown });

            return unknown;
        }

        var applied = new List<FileStoragePathTreeRow>(rows.Count);
        foreach (var row in rows) {
            var expected = FileStorageGridRowHelper.BuildExpectedStorageKey(row.FileId, row.SourceFileName, row.PathPrefix);
            applied.Add(row with { Presence = KeyExists(listedKeys, expected) ? FileStoragePresence.Both : FileStoragePresence.MissingBlob });
        }

        return applied;
    }

    /// <summary>Listed keys that did not match any metadata row, skipping health probes.</summary>
    /// <param name="listedKeys">Keys from one diagnostics LIST.</param>
    /// <param name="rows">QueryProject file rows already joined.</param>
    public static List<string> UnmatchedKeys(IReadOnlyCollection<string> listedKeys, IReadOnlyList<FileStoragePathTreeRow> rows)
    {
        ArgumentHelpers.ThrowIfNull(listedKeys);
        ArgumentHelpers.ThrowIfNull(rows);
        var unmatched = new List<string>();
        foreach (var key in listedKeys) {
            if (IsHealthKey(key))
                continue;

            var matched = false;
            foreach (var row in rows) {
                var expected = FileStorageGridRowHelper.BuildExpectedStorageKey(row.FileId, row.SourceFileName, row.PathPrefix);
                if (!KeyExists([key], expected))
                    continue;

                matched = true;
                break;
            }

            if (!matched)
                unmatched.Add(key);
        }

        return unmatched;
    }

    /// <summary>
    /// Parses a stored object key into file id, logical prefix, and source file name. Shard layout <c>aa/bb/{id:N}...</c> has a null prefix.
    /// </summary>
    /// <param name="key">Object key from LIST.</param>
    /// <param name="fileId">Parsed file id.</param>
    /// <param name="pathPrefix">Logical prefix, or null for shard layout.</param>
    /// <param name="sourceFileName">Last path segment (id plus suffix).</param>
    public static bool TryParseStorageKey(string key, out Guid fileId, out string? pathPrefix, out string sourceFileName)
    {
        fileId = default;
        pathPrefix = null;
        sourceFileName = "";
        if (string.IsNullOrWhiteSpace(key) || IsHealthKey(key))
            return false;

        var parts = key.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return false;

        var last = parts[^1];
        if (last.Length < 32)
            return false;

        var idText = last[..32];
        if (!Guid.TryParseExact(idText, "N", out fileId))
            return false;

        sourceFileName = last;
        if (parts.Length == 1) {
            pathPrefix = null;
            return true;
        }

        var prefixParts = parts[..^1];
        if (prefixParts.Length == 2 && IsShardSegment(prefixParts[0]) && IsShardSegment(prefixParts[1])) {
            pathPrefix = null;
            return true;
        }

        pathPrefix = string.Join("/", prefixParts);
        return true;
    }

    private static bool IsHealthKey(string key)
        => key.Contains(".lyo-health", StringComparison.Ordinal);

    private static bool IsShardSegment(string segment)
        => segment.Length == 2 && char.IsAsciiHexDigit(segment[0]) && char.IsAsciiHexDigit(segment[1]);
}
