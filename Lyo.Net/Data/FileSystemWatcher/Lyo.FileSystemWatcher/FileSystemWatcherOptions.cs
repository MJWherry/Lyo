using Lyo.Exceptions;
using Lyo.FileSystemWatcher.Models;

namespace Lyo.FileSystemWatcher;

/// <summary>Settings that control how FileSystemWatcher scans and reports changes.</summary>
public class FileSystemWatcherOptions
{
    /// <summary>If true, subdirectories are included in the native watch and in <c>TakeSnapshot</c>. Starts as false.</summary>
    public bool IncludeSubdirectories { get; set; } = false;

    /// <summary>Debounce delay in milliseconds. Starts at 250. Changes inside this window are batched.</summary>
    public int DebounceTimerDelay { get; set; } = 250;

    /// <summary>If true, file hashes are computed so moves and renames can be detected. Starts as true. Turning this off is faster but drops hash-based move detection.</summary>
    public bool EnableFileHashing { get; set; } = true;

    /// <summary>
    /// How paths are compared. Starts as OrdinalIgnoreCase, which fits Windows. Use Ordinal on case-sensitive file systems such as Linux and macOS.
    /// </summary>
    public StringComparison PathComparison { get; set; } = StringComparison.OrdinalIgnoreCase;

    /// <summary>If true, metrics are recorded. Starts as false. When on, the constructor must receive IMetrics.</summary>
    public bool EnableMetrics { get; set; } = false;

    /// <summary>
    /// Include regexes matched against the relative path from the watch root (for example <c>\.json$</c>). Empty means every file is included. Exclude patterns still win.
    /// </summary>
    public IList<string> IncludePatterns { get; set; } = [];

    /// <summary>
    /// Exclude regexes matched against the relative path from the watch root (for example <c>(^|/)\.git(/|$)</c>, <c>(^|/)bin(/|$)</c>). Matching files are omitted from snapshots; matching directories are not descended into.
    /// </summary>
    public IList<string> ExcludePatterns { get; set; } = [];

    /// <summary>Throws ArgumentException when the settings are invalid.</summary>
    internal void Validate()
    {
        ArgumentHelpers.ThrowIfNegative(DebounceTimerDelay);
        FileSystemPathGlob.ValidatePatterns(IncludePatterns, PathComparison);
        FileSystemPathGlob.ValidatePatterns(ExcludePatterns, PathComparison);
    }
}