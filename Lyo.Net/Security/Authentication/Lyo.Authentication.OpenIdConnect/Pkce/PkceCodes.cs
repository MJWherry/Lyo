using System.Security.Cryptography;
using System.Text;
using Lyo.Authentication.Models.Format;
using Lyo.Common.Core.Security;

namespace Lyo.Authentication.OpenIdConnect.Pkce;

/// <summary>An RFC 7636 PKCE verifier + S256 challenge pair.</summary>
/// <param name="Verifier">High-entropy verifier (base64url, 43 chars from 32 random bytes).</param>
/// <param name="Challenge">S256 challenge: <c>base64url(SHA-256(verifier))</c>.</param>
public sealed record PkceCodes(string Verifier, string Challenge)
{
    /// <summary>PKCE method we always use (only S256 is allowed).</summary>
    public const string Method = "S256";

    /// <summary>Generates a fresh verifier + challenge pair.</summary>
    public static PkceCodes Generate()
    {
        var verifier = Base64Url.Encode(CryptographicRandom.GetBytes(32));
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        var challenge = Base64Url.Encode(hash);
        return new(verifier, challenge);
    }
}