namespace Lyo.FileSystemWatcher.Models;

/// <summary>Serializable watch settings (patterns, recursion, hashing, debounce).</summary>
public sealed class FileSystemWatchOptionsDto
{
    /// <summary>If true, subdirectories are included in the snapshot and native watch.</summary>
    public bool IncludeSubdirectories { get; set; }

    /// <summary>Debounce delay in milliseconds.</summary>
    public int DebounceTimerDelay { get; set; } = 250;

    /// <summary>If true, file hashes and fingerprints are computed for move detection.</summary>
    public bool EnableFileHashing { get; set; } = true;

    /// <summary>How paths are compared. <c>Ordinal</c> or <c>OrdinalIgnoreCase</c>.</summary>
    public string PathComparison { get; set; } = nameof(StringComparison.OrdinalIgnoreCase);

    /// <summary>Include regexes matched against the relative path from the watch root. Empty means match all files.</summary>
    public IList<string> IncludePatterns { get; set; } = [];

    /// <summary>Exclude regexes matched against the relative path from the watch root. Exclude wins over include.</summary>
    public IList<string> ExcludePatterns { get; set; } = [];
}
