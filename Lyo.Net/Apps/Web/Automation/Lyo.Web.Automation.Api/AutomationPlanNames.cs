namespace Lyo.Web.Automation.Api;

/// <summary>Rejects plan names that are not a single URL segment.</summary>
public static class AutomationPlanNames
{
    /// <summary>True when <paramref name="name" /> has no directory separators and no <c>..</c> segment.</summary>
    public static bool IsSingleSegment(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (name.Contains('/') || name.Contains('\\'))
            return false;

        if (name.Contains("..", StringComparison.Ordinal))
            return false;

        return true;
    }
}