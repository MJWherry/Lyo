using System.Security.Cryptography;

namespace Lyo.FileMetadataStore.Models;

/// <summary>Helpers that build <see cref="System.Security.Cryptography.HashAlgorithm" /> instances from <see cref="HashAlgorithm" />.</summary>
public static class HashAlgorithmExtensions
{
    /// <summary>Builds a <see cref="System.Security.Cryptography.HashAlgorithm" /> for the given enum value.</summary>
    public static System.Security.Cryptography.HashAlgorithm Create(this HashAlgorithm algorithm)
        => algorithm switch {
            HashAlgorithm.Sha256 => SHA256.Create(),
            HashAlgorithm.Sha384 => SHA384.Create(),
            HashAlgorithm.Sha512 => SHA512.Create(),
            HashAlgorithm.Md5 => MD5.Create(),
            HashAlgorithm.Sha1 => SHA1.Create(),
            var _ => SHA256.Create()
        };
}