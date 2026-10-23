using System.Diagnostics;

namespace Lyo.Authentication.Sso.Protocol;

/// <summary>Callback material the endpoint collected. Protocols read only the fields their binding uses.</summary>
/// <param name="Provider">Provider name from the route.</param>
/// <param name="Method">HTTP method, <c>GET</c> or <c>POST</c>.</param>
/// <param name="Query">Query string values.</param>
/// <param name="Form">Form fields. Empty for GET.</param>
/// <param name="StateCookie">Sealed OpenID Connect state cookie, when the browser sent one.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SsoCallback(string Provider, string Method, IReadOnlyDictionary<string, string?> Query, IReadOnlyDictionary<string, string?> Form, string? StateCookie)
{
    /// <summary>Provider, method, and field counts. Query, form, and state cookie values are omitted.</summary>
    public override string ToString() => $"{Provider} {Method} Query={Query.Count} Form={Form.Count} StateCookieSet={!string.IsNullOrEmpty(StateCookie)}";
}