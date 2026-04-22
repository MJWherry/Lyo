using System.Diagnostics;

namespace Lyo.Web.Automation.Models;

/// <summary>Engine-neutral cookie representation used by <see cref="Lyo.Web.Automation.Abstractions.IBrowserCookies" />.</summary>
[DebuggerDisplay("{Name}={Value}")]
public sealed class BrowserCookie
{
    public string Name { get; init; } = "";

    public string Value { get; init; } = "";

    public string? Domain { get; init; }

    public string? Path { get; init; }

    public bool? Secure { get; init; }

    public bool? HttpOnly { get; init; }

    public DateTimeOffset? Expiry { get; init; }

    public override string ToString() => $"{Name}={Value}";
}