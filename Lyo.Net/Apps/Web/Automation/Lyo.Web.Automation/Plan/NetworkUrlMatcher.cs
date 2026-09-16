using System.Text.RegularExpressions;

namespace Lyo.Web.Automation.Plan;

/// <summary>Decides which request URLs a <see cref="Lyo.Web.Automation.Models.CaptureNetworkAutomationStep" /> keeps.</summary>
public static class NetworkUrlMatcher
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    /// <summary>True when <paramref name="pattern" /> is blank or matches <paramref name="url" /> without regard to case.</summary>
    public static bool Matches(string? pattern, string url)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return true;

        return Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, MatchTimeout);
    }

    /// <summary>Throws <see cref="InvalidOperationException" /> when <paramref name="pattern" /> is not a regular expression.</summary>
    public static void ThrowIfInvalid(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return;

        try {
            _ = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, MatchTimeout);
        }
        catch (ArgumentException ex) {
            throw new InvalidOperationException($"URL pattern is not a valid regular expression: {pattern}", ex);
        }
    }
}
