using Lyo.Exceptions;

namespace Lyo.FileSystemWatcher;

/// <summary>Configuration options for FileSystemWatcher.</summary>
public class FileSystemWatcherOptions
{
    /// <summary>Gets or sets whether to include subdirectories when watching. Default is false.</summary>
    public bool IncludeSubdirectories { get; set; } = false;

    /// <summary>Gets or sets the debounce timer delay in milliseconds. Default is 250ms. Changes occurring within this delay will be batched together.</summary>
    public int DebounceTimerDelay { get; set; } = 250;

    /// <summary>Gets or sets whether to compute file hashes for move/rename detection. Default is true. Setting this to false improves performance but disables hash-based move detection.</summary>
    public bool EnableFileHashing { get; set; } = true;

    /// <summary>
    /// Gets or sets the string comparison to use for path comparisons. Default is OrdinalIgnoreCase (Windows-appropriate). Use Ordinal for case-sensitive file systems
    /// (Linux/macOS).
    /// </summary>
    public StringComparison PathComparison { get; set; } = StringComparison.OrdinalIgnoreCase;

    /// <summary>Gets or sets whether to enable metrics collection. Default is false. When enabled, requires IMetrics to be provided via constructor.</summary>
    public bool EnableMetrics { get; set; } = false;

    /// <summary>Validates the options and throws ArgumentException if invalid.</summary>
    internal void Validate() => ArgumentHelpers.ThrowIfNegative(DebounceTimerDelay, nameof(DebounceTimerDelay));
}