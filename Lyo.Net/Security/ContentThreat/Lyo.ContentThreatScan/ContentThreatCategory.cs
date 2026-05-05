namespace Lyo.ContentThreatScan;

/// <summary>Contribution category used for tagging rule hits (scoring thresholds are numeric, not enums).</summary>
public enum ContentThreatCategory
{
    Other = 0,
    SqlInjection = 1,
    ScriptInjection = 2,
    Reputation = 3,
    AntiMalwareEngine = 4
}
