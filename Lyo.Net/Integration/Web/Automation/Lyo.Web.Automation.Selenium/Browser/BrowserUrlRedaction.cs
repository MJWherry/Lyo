namespace Lyo.Web.Automation.Selenium.Browser;

/// <summary>Masks query strings in URLs for logging when tokens may appear.</summary>
public static class BrowserUrlRedaction
{
    /// <summary>Returns <paramref name="url" /> with the query and fragment removed when <paramref name="mask" /> is true.</summary>
    public static string ForLog(string url, bool mask)
    {
        if (!mask || string.IsNullOrWhiteSpace(url))
            return url;

        try {
            var u = new Uri(url);
            return u.GetLeftPart(UriPartial.Path);
        }
        catch (UriFormatException) {
            return url.Length > 120 ? url.Substring(0, 120) + "…" : url;
        }
    }
}