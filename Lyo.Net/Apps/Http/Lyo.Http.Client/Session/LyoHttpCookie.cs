using System.Diagnostics;

namespace Lyo.Http.Client.Session;

/// <summary>HTTP cookie used by <see cref="ILyoHttpCookieJar" />. Same shape as automation's browser cookie, without referencing that package.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class LyoHttpCookie
{
    /// <summary>Cookie name.</summary>
    public string Name { get; init; } = "";

    /// <summary>Cookie value.</summary>
    public string Value { get; init; } = "";

    /// <summary>Optional domain.</summary>
    public string? Domain { get; init; }

    /// <summary>Optional path. Defaults to <c>/</c> when applied to a <see cref="System.Net.Cookie" />.</summary>
    public string? Path { get; init; }

    /// <summary>When true, the cookie is marked Secure.</summary>
    public bool? Secure { get; init; }

    /// <summary>When true, the cookie is HttpOnly.</summary>
    public bool? HttpOnly { get; init; }

    /// <summary>Optional expiry.</summary>
    public DateTimeOffset? Expiry { get; init; }

    /// <inheritdoc />
    public override string ToString() => $"{Name}={Value}";
}
