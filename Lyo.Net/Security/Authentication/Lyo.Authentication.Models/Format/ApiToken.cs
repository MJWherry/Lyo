using System.Diagnostics;

namespace Lyo.Authentication.Models.Format;

/// <summary>A parsed Format-B Lyo token. Produced by <see cref="ApiTokenCodec.TryParse" />.</summary>
/// <param name="Plaintext">The original wire string. Held briefly during validation, never persisted.</param>
/// <param name="Id">The 11-character Crockford base32 id used as the primary key in <see cref="Services.Opaque.IApiTokenStore" />.</param>
/// <param name="Kind">The token kind (e.g. <see cref="ApiTokenKind.Pat" />).</param>
/// <param name="Ring">The deployment ring (e.g. <see cref="ApiTokenRing.Live" />).</param>
/// <param name="Secret">The raw secret segment (base64url, 43 chars, ~256 bits of entropy). Hash with <see cref="ApiTokenCodec.ComputeSecretHash(string)" /> before comparison.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ApiToken(string Plaintext, string Id, string Kind, string Ring, string Secret)
{
    /// <summary>Returns a redacted form suitable for logs / error messages: <c>lyo_&lt;kind&gt;_&lt;ring&gt;_&lt;id&gt;_***</c>.</summary>
    public override string ToString() => $"lyo_{Kind}_{Ring}_{Id}_***";
}