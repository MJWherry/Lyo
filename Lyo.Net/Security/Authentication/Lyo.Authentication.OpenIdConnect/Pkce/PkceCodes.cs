using System.Security.Cryptography;
using System.Text;
using Lyo.Authentication.Models.Format;

namespace Lyo.Authentication.OpenIdConnect.Pkce;

/// <summary>An RFC 7636 PKCE verifier + S256 challenge pair.</summary>
/// <param name="Verifier">The high-entropy verifier (base64url, 43 chars from 32 random bytes).</param>
/// <param name="Challenge">The S256 challenge: <c>base64url(SHA-256(verifier))</c>.</param>
public sealed record PkceCodes(string Verifier, string Challenge)
{
    /// <summary>The PKCE method we always use (only S256 is allowed).</summary>
    public const string Method = "S256";

    /// <summary>Generates a fresh verifier + challenge pair.</summary>
    public static PkceCodes Generate()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var verifier = Base64Url.Encode(bytes);
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        var challenge = Base64Url.Encode(hash);
        return new(verifier, challenge);
    }
}