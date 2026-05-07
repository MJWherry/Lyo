namespace Lyo.ContentThreatScan;

/// <summary>One weighted observation during a scan. Avoid logging payloads; use RuleId only.</summary>
public readonly record struct ContentThreatContribution(string RuleId, ContentThreatCategory Category, decimal Points = 0m, string? Detail = null) { }