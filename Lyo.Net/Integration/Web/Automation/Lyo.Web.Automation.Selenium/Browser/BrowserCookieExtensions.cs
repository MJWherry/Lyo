using System.Diagnostics;
using System.Text.Json;
using Lyo.Exceptions;
using OpenQA.Selenium;

namespace Lyo.Web.Automation.Selenium.Browser;

/// <summary>Serializable cookie for export/import (e.g. auth state between runs).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record BrowserCookieRecord(
    string Name,
    string Value,
    string? Domain = null,
    string? Path = null,
    bool? Secure = null,
    bool? IsHttpOnly = null,
    long? ExpiryUnixSeconds = null)
{
    public override string ToString() => $"{Name}:{Value}";
}

/// <summary>Import/export <see cref="Cookie" /> collections as JSON.</summary>
public static class BrowserCookieExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static BrowserCookieRecord ToRecord(Cookie c)
        => new(c.Name, c.Value, c.Domain, c.Path, c.Secure, c.IsHttpOnly, c.Expiry.HasValue ? new DateTimeOffset(c.Expiry.Value).ToUnixTimeSeconds() : null);

    extension(SeleniumBrowser browser)
    {
        /// <summary>Exports all cookies for the current domain context as DTOs.</summary>
        public IReadOnlyList<BrowserCookieRecord> ExportCookieRecords()
        {
            ArgumentHelpers.ThrowIfNull(browser);
            var driver = browser.GetRequiredDriver();
            return driver.Manage().Cookies.AllCookies.Select(ToRecord).ToList();
        }

        /// <summary>Serializes cookies to JSON (UTF-8).</summary>
        public string ExportCookiesJson()
        {
            var list = browser.ExportCookieRecords();
            return JsonSerializer.Serialize(list, JsonOptions);
        }

        /// <summary>Deserializes cookies from JSON and adds them to the browser (navigate to the target origin first).</summary>
        public void ImportCookiesFromJson(string json)
        {
            ArgumentHelpers.ThrowIfNull(browser);
            ArgumentHelpers.ThrowIfNull(json);
            var list = JsonSerializer.Deserialize<List<BrowserCookieRecord>>(json);
            ArgumentHelpers.ThrowIfNull(list, nameof(json));
            browser.ImportCookieRecords(list);
        }

        /// <summary>Adds cookies from records (same rules as <see cref="OpenQA.Selenium.Cookie" />).</summary>
        public void ImportCookieRecords(IEnumerable<BrowserCookieRecord> cookies)
        {
            ArgumentHelpers.ThrowIfNull(browser);
            ArgumentHelpers.ThrowIfNull(cookies);
            var driver = browser.GetRequiredDriver();
            foreach (var c in cookies) {
                var expiry = c.ExpiryUnixSeconds is { } ux ? DateTimeOffset.FromUnixTimeSeconds(ux).UtcDateTime : (DateTime?)null;
                var sel = new Cookie(c.Name, c.Value, c.Domain ?? "", c.Path ?? "/", expiry);
                driver.Manage().Cookies.AddCookie(sel);
            }
        }
    }
}