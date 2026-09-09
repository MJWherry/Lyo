namespace Lyo.ContentThreatScan;

/// <summary>One weighted observation from a scan. Log <c>RuleId</c> only — never the payload.</summary>
public readonly record struct ContentThreatContribution(string RuleId, ContentThreatCategory Category, decimal Points = 0m, string? Detail = null) { }