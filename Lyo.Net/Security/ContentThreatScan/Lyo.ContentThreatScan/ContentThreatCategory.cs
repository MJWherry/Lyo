namespace Lyo.ContentThreatScan;

/// <summary>Category tag on a rule hit. Thresholds themselves are numeric, not this enum.</summary>
public enum ContentThreatCategory
{
    Other = 0,
    SqlInjection = 1,
    ScriptInjection = 2,
    Reputation = 3,
    AntiMalwareEngine = 4
}