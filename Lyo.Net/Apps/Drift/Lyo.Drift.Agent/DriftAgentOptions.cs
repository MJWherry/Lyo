using Lyo.Drift.Models;
using Lyo.Exceptions;
using Lyo.FileSystemWatcher.Models;

namespace Lyo.Drift.Agent;

/// <summary>One directory the agent watches.</summary>
public sealed class DriftWatchOptions
{
    /// <summary>Absolute directory path.</summary>
    public string Path { get; set; } = "";

    /// <summary>If true, subdirectories are included.</summary>
    public bool IncludeSubdirectories { get; set; } = true;

    /// <summary>Include regexes matched against the relative path from <see cref="Path" />. Empty means all files.</summary>
    public IList<string> IncludePatterns { get; set; } = [];

    /// <summary>Exclude regexes matched against the relative path from <see cref="Path" />. Exclude wins.</summary>
    public IList<string> ExcludePatterns { get; set; } = [];

    /// <summary>If true, file hashes are computed for move detection.</summary>
    public bool EnableFileHashing { get; set; } = true;

    /// <summary>Debounce delay in milliseconds.</summary>
    public int DebounceTimerDelay { get; set; } = 250;
}

/// <summary>Settings for the Drift agent hosted service.</summary>
public sealed class DriftAgentOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "DriftAgent";

    /// <summary>Collector base URL (also used as the HTTP client route prefix when registering <c>IDriftClient</c>).</summary>
    public string ApiBaseUrl { get; set; } = "";

    /// <summary>Stable instance key. Combined with the machine name when it does not already contain it.</summary>
    public string InstanceKey { get; set; } = "";

    /// <summary>Directories to watch. One <c>FileSystemWatcher</c> is created per entry.</summary>
    public IList<DriftWatchOptions> Watches { get; set; } = [];

    /// <summary>If true, collect a stable system-info projection on start and on <see cref="SystemInfoInterval" />.</summary>
    public bool CollectSystemInfo { get; set; } = true;

    /// <summary>How often to re-collect system info.</summary>
    public TimeSpan SystemInfoInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Which system-info groups to project. Empty Include uses the identity default (OS, CPU, RAM total, host, drives, NIC names).</summary>
    public SystemInfoDriftSettings SystemInfo { get; set; } = new();

    /// <summary>If true, POST the ScanCompleted change batch after each new snapshot.</summary>
    public bool PostLiveChanges { get; set; } = true;

    /// <summary>If true, compute file/system diffs locally and POST them. Structure snapshots are still posted.</summary>
    public bool ComputeDiffsLocally { get; set; } = true;

    /// <summary>
    /// Include globs for attaching file bytes. Default off — v1 posts metadata and hashes only; this does not add bytes until a content field exists on the snapshot DTO.
    /// </summary>
    public IList<string> CaptureContentPatterns { get; set; } = [];

    /// <summary>Maximum captured file size when <see cref="CaptureContentPatterns" /> is used. Unused while content capture is off.</summary>
    public int MaxContentBytes { get; set; } = 64 * 1024;

    /// <summary>Heartbeat interval.</summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Bounded ingest channel capacity. When full, oldest work is dropped.</summary>
    public int IngestChannelCapacity { get; set; } = 32;

    /// <summary>Throws when required settings are missing.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(InstanceKey);
        ArgumentHelpers.ThrowIfNegative(IngestChannelCapacity);
        ArgumentHelpers.ThrowIfLessThanOrEqual(IngestChannelCapacity, 0);
        _ = SystemInfoDriftGroups.Parse(SystemInfo.Include);
        foreach (var watch in Watches) {
            FileSystemPathGlob.ValidatePatterns(watch.IncludePatterns);
            FileSystemPathGlob.ValidatePatterns(watch.ExcludePatterns);
        }
    }

    /// <summary>Resolved include groups for system-info projection.</summary>
    public IReadOnlyList<SystemInfoDriftGroup> ResolvedSystemInfoInclude()
        => SystemInfoDriftGroups.Parse(SystemInfo.Include);
}

/// <summary>Agent-side include list for the system-info projection.</summary>
public sealed class SystemInfoDriftSettings
{
    /// <summary>Named groups from <see cref="SystemInfoDriftGroup" />. Empty means identity-only.</summary>
    public IList<string> Include { get; set; } = [];
}
