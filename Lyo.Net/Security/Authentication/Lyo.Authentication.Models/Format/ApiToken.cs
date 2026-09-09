using System.Diagnostics;

namespace Lyo.Authentication.Models.Format;

/// <summary>Parsed Format-B Lyo token. Produced by <see cref="ApiTokenCodec.TryParse" />.</summary>
/// <param name="Plaintext">Original wire string. Held briefly during validation, never persisted.</param>
/// <param name="Id">11-character Crockford base32 id used as the primary key in <see cref="Services.Opaque.IApiTokenStore" />.</param>
/// <param name="Kind">Token kind (e.g. <see cref="ApiTokenKind.Pat" />).</param>
/// <param name="Ring">Deployment ring (e.g. <see cref="ApiTokenRing.Live" />).</param>
/// <param name="Secret">Raw secret segment (base64url, 43 chars, ~256 bits). Hash with <see cref="ApiTokenCodec.ComputeSecretHash(string)" /> before comparison.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ApiToken(string Plaintext, string Id, string Kind, string Ring, string Secret)
{
    /// <summary>Redacted form for logs / errors: <c>lyo_&lt;kind&gt;_&lt;ring&gt;_&lt;id&gt;_***</c>.</summary>
    public override string ToString() => $"lyo_{Kind}_{Ring}_{Id}_***";
}