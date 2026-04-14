namespace Lyo.FileStorage.Policy;

public enum FileScanThreatLevel
{
    Clean = 0,
    Suspect = 1,
    Threat = 2
}

public sealed record FileScanResult(FileScanThreatLevel ThreatLevel, string? Detail = null);