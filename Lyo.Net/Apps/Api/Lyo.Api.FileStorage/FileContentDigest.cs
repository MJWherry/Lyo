using Lyo.FileMetadataStore.Models;

namespace Lyo.Api.FileStorage;

/// <summary>RFC 9530 <c>Content-Digest</c> for a full plaintext download.</summary>
internal static class FileContentDigest
{
    /// <summary>
    /// Builds <c>sha-256=:base64=:</c> (and the other stored algorithms) for a full-body response. A ranged response and a missing hash return null.
    /// </summary>
    public static string? ForDownload(bool hasRange, HashAlgorithm? algorithm, byte[]? hash)
    {
        if (hasRange || hash is not { Length: > 0 })
            return null;

        var name = (algorithm ?? HashAlgorithm.Sha256) switch {
            HashAlgorithm.Sha256 => "sha-256",
            HashAlgorithm.Sha384 => "sha-384",
            HashAlgorithm.Sha512 => "sha-512",
            HashAlgorithm.Sha1 => "sha-1",
            HashAlgorithm.Md5 => "md5",
            _ => null
        };
        return name is null ? null : $"{name}=:{Convert.ToBase64String(hash)}:";
    }
}
