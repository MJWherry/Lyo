namespace Lyo.Web.Automation.Studio;

/// <summary>Turns an address-bar string into a URL the browser can open.</summary>
public static class StudioAddress
{
    /// <summary>
    /// Trims <paramref name="text" />. A host with no scheme gets <c>https://</c>. <c>http</c>, <c>https</c>, and <c>about:</c> stay as typed. Blank text is null.
    /// </summary>
    public static string? Normalize(string? text)
    {
        var trimmed = text?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return null;

        if (trimmed.StartsWith("about:", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("://", StringComparison.Ordinal))
            return trimmed;

        return "https://" + trimmed;
    }
}
