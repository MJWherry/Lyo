namespace Lyo.Drift.Web.Components;

/// <summary>MudBlazor chip colors and icons for Drift instance, snapshot, diff, and change vocabularies.</summary>
public static class DriftColorHelper
{
    /// <summary>A heartbeat older than this is treated as stale even if the instance row still exists.</summary>
    public static readonly TimeSpan HeartbeatStaleAfter = TimeSpan.FromSeconds(90);

    /// <summary>True when the last heartbeat is older than <paramref name="threshold" /> (90s by default).</summary>
    public static bool IsHeartbeatStale(DateTime? lastHeartbeatUtc, TimeSpan? threshold = null)
        => lastHeartbeatUtc is { } at && DateTime.UtcNow - at > (threshold ?? HeartbeatStaleAfter);

    /// <summary>Color for an agent instance lifecycle state.</summary>
    public static Color ForInstanceState(DriftInstanceState state)
        => state switch {
            DriftInstanceState.Running => Color.Success,
            DriftInstanceState.Stopped => Color.Default,
            var _ => Color.Default
        };

    /// <summary>Icon for an agent instance lifecycle state.</summary>
    public static string InstanceStateIcon(DriftInstanceState state)
        => state switch {
            DriftInstanceState.Running => Icons.Material.Filled.PlayArrow,
            DriftInstanceState.Stopped => Icons.Material.Filled.Stop,
            var _ => Icons.Material.Filled.Help
        };

    /// <summary>Color for a stored snapshot kind.</summary>
    public static Color ForSnapshotKind(DriftSnapshotKind kind)
        => kind switch {
            DriftSnapshotKind.FileTree => Color.Info,
            DriftSnapshotKind.SystemInfo => Color.Primary,
            var _ => Color.Default
        };

    /// <summary>Color for who computed a stored diff.</summary>
    public static Color ForDiffSource(DriftDiffSource source)
        => source switch {
            DriftDiffSource.Agent => Color.Info,
            DriftDiffSource.Server => Color.Primary,
            var _ => Color.Default
        };

    /// <summary>Color for a persistable file-system change kind.</summary>
    public static Color ForChangeKind(FileSystemChangeKind kind)
        => kind switch {
            FileSystemChangeKind.Created => Color.Success,
            FileSystemChangeKind.Changed => Color.Info,
            FileSystemChangeKind.Deleted => Color.Error,
            FileSystemChangeKind.Renamed => Color.Warning,
            FileSystemChangeKind.Moved => Color.Warning,
            var _ => Color.Default
        };

    /// <summary>Reads a projected Guid <c>Id</c> (CLR, string, or JSON).</summary>
    public static Guid? GetGuidId(object? item)
    {
        var value = ProjectedValueHelper.GetValue(item, "Id");
        return value switch {
            Guid g => g,
            string s when Guid.TryParse(s, out var parsed) => parsed,
            System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.String } el when Guid.TryParse(el.GetString(), out var fromJson) => fromJson,
            var _ => null
        };
    }
}
