using System.Diagnostics;

namespace Lyo.FileStorage.Policy;

public enum FileScanThreatLevel
{
    Clean = 0,
    Suspect = 1,
    Threat = 2
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileScanResult(FileScanThreatLevel ThreatLevel, string? Detail = null)
{
    /// <inheritdoc />
    public override string ToString() => $"FileScanResult: {ThreatLevel}{(Detail == null ? "" : $", {Detail}")}";
}
