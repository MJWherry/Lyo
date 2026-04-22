using Lyo.Exceptions;

namespace Lyo.Web.Automation.Selenium.WebDriver;

/// <summary>
/// Formats driver command-line switches: Chromium-style <c>--key=value</c> when <paramref name="value" /> is set; flag-only (e.g. <c>-headless</c>) when value is null or
/// whitespace.
/// </summary>
public static class WebDriverArgumentFormatter
{
    /// <summary>
    /// When <paramref name="value" /> is null or whitespace, returns <paramref name="key" /> trimmed (use for Firefox <c>-headless</c>, etc.). Otherwise returns <c>key=value</c>
    /// (trimmed), avoiding a double <c>=</c> on <paramref name="key" />.
    /// </summary>
    public static string Format(string key, string? value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        var k = key.Trim();
        if (value == null || string.IsNullOrWhiteSpace(value))
            return k;

        var trimmedKey = k.TrimEnd();
        if (trimmedKey.EndsWith("=", StringComparison.Ordinal))
            trimmedKey = trimmedKey.TrimEnd('=');

        return $"{trimmedKey}={value.Trim()}";
    }
}