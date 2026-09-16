namespace Lyo.Web.Automation.Api;

/// <summary>Rejects snapshot names that are not a single file name.</summary>
public static class AutomationSnapshotNames
{
    /// <summary>True when <paramref name="name" /> has no directory separators and no <c>..</c> segment.</summary>
    public static bool IsSingleFileName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (!string.Equals(name, Path.GetFileName(name), StringComparison.Ordinal))
            return false;

        if (name.Contains("..", StringComparison.Ordinal))
            return false;

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return false;

        return true;
    }
}
