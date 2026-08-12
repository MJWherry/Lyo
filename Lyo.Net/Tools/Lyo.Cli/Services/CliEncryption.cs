using Lyo.Encryption;
using Lyo.Encryption.AesCcm;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.AesSiv;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Encryption.XChaCha20Poly1305;
using Lyo.Exceptions;
using Lyo.KeyStore;
using AesSivKeySizeBits = Lyo.Encryption.AesSiv.AesSivKeySizeBits;

namespace Lyo.Cli.Services;

/// <summary>Single-key encrypt/decrypt helpers mirroring the Gateway file transformer.</summary>
internal static class CliEncryption
{
    public static EncryptionAlgorithm ParseAlgorithm(string? name)
    {
        name = string.IsNullOrWhiteSpace(name) ? "aesgcm" : name.Trim();
        return name.ToLowerInvariant() switch {
            "aesgcm" or "aes-gcm" or "gcm" => EncryptionAlgorithm.AesGcm,
            "chacha20poly1305" or "chacha" or "chacha20" => EncryptionAlgorithm.ChaCha20Poly1305,
            "xchacha20poly1305" or "xchacha" => EncryptionAlgorithm.XChaCha20Poly1305,
            "aesccm" or "aes-ccm" or "ccm" => EncryptionAlgorithm.AesCcm,
            "aessiv" or "aes-siv" or "siv" => EncryptionAlgorithm.AesSiv,
            var _ => throw new ArgumentException($"Unknown encryption algorithm '{name}'. Use aesgcm, chacha20poly1305, xchacha20poly1305, aesccm, or aessiv.")
        };
    }

    public static int RequiredKeyBytes(EncryptionAlgorithm algorithm)
        => algorithm switch {
            EncryptionAlgorithm.AesGcm => AesGcmKeySizeBits.Bits256.GetKeyLengthBytes(),
            EncryptionAlgorithm.AesCcm => AesGcmKeySizeBits.Bits256.GetKeyLengthBytes(),
            EncryptionAlgorithm.AesSiv => AesSivKeySizeBits.Bits256.GetKeyLengthBytes(),
            EncryptionAlgorithm.ChaCha20Poly1305 or EncryptionAlgorithm.XChaCha20Poly1305 => 32,
            var _ => throw new NotSupportedException($"{algorithm} is not supported by lyo crypt.")
        };

    public static IEncryptionService CreateService(EncryptionAlgorithm algorithm)
    {
        var keyStore = new LocalKeyStore();
        return algorithm switch {
            EncryptionAlgorithm.AesGcm => new AesGcmEncryptionService(keyStore),
            EncryptionAlgorithm.ChaCha20Poly1305 => new ChaCha20Poly1305EncryptionService(keyStore),
            EncryptionAlgorithm.AesCcm => new AesCcmEncryptionService(keyStore, AesGcmKeySizeBits.Bits256),
            EncryptionAlgorithm.AesSiv => new AesSivEncryptionService(keyStore, AesSivKeySizeBits.Bits256),
            EncryptionAlgorithm.XChaCha20Poly1305 => new XChaCha20Poly1305EncryptionService(keyStore),
            var _ => throw new NotSupportedException($"{algorithm} is not supported by lyo crypt.")
        };
    }

    public static async Task EncryptAsync(Stream input, Stream output, EncryptionAlgorithm algorithm, byte[] key, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNull(output);
        ArgumentHelpers.ThrowIfNull(key);
        var service = CreateService(algorithm);
        try {
            await service.EncryptToStreamAsync(input, output, key: key, ct: ct).ConfigureAwait(false);
        }
        finally {
            (service as IDisposable)?.Dispose();
        }
    }

    public static async Task DecryptAsync(Stream input, Stream output, EncryptionAlgorithm algorithm, byte[] key, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNull(output);
        ArgumentHelpers.ThrowIfNull(key);
        var service = CreateService(algorithm);
        try {
            await service.DecryptToStreamAsync(input, output, key: key, ct: ct).ConfigureAwait(false);
        }
        finally {
            (service as IDisposable)?.Dispose();
        }
    }

    public static string FileExtension(EncryptionAlgorithm algorithm)
    {
        var service = CreateService(algorithm);
        try {
            return service.FileExtension;
        }
        finally {
            (service as IDisposable)?.Dispose();
        }
    }
}