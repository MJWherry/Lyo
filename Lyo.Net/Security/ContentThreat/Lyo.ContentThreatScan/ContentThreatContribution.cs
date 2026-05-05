namespace Lyo.ContentThreatScan;

/// <summary>One weighted observation during a scan. Avoid logging payloads; use RuleId only.</summary>
public readonly struct ContentThreatContribution
{
    public ContentThreatContribution(string ruleId, ContentThreatCategory category, decimal points, string? detail = null)
    {
        RuleId = ruleId ?? string.Empty;
        Category = category;
        Points = points >= 0m ? points : 0m;
        Detail = detail;
    }

    public string RuleId { get; }
    public ContentThreatCategory Category { get; }
    public decimal Points { get; }
    public string? Detail { get; }
}
