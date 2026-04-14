namespace Lyo.Encryption;

/// <summary>Validates two-key DEK header fields and plaintext DEK length after KEK unwrap.</summary>
public static class TwoKeyDekValidation
{
    /// <summary>Throws if <paramref name="dekKeyMaterialBytes"/> is not valid for <paramref name="dekAlgorithmId"/>.</summary>
    public static void ValidateHeader(byte dekAlgorithmId, byte dekKeyMaterialBytes)
    {
        var alg = (EncryptionAlgorithm)dekAlgorithmId;
        switch (alg) {
            case EncryptionAlgorithm.AesGcm:
                if (dekKeyMaterialBytes is not (16 or 24 or 32))
                    throw new InvalidDataException(
                        $"Invalid DEK key material length {dekKeyMaterialBytes} for AES-GCM. Expected 16, 24, or 32 bytes.");
                break;
            case EncryptionAlgorithm.ChaCha20Poly1305:
                if (dekKeyMaterialBytes != 32)
                    throw new InvalidDataException($"Invalid DEK key material length {dekKeyMaterialBytes} for ChaCha20-Poly1305. Expected 32 bytes.");
                break;
            case EncryptionAlgorithm.AesCcm:
                if (dekKeyMaterialBytes is not (16 or 24 or 32))
                    throw new InvalidDataException(
                        $"Invalid DEK key material length {dekKeyMaterialBytes} for AES-CCM. Expected 16, 24, or 32 bytes.");
                break;
            case EncryptionAlgorithm.AesSiv:
                if (dekKeyMaterialBytes is not (32 or 48 or 64))
                    throw new InvalidDataException(
                        $"Invalid DEK key material length {dekKeyMaterialBytes} for AES-SIV. Expected 32, 48, or 64 bytes.");
                break;
            case EncryptionAlgorithm.XChaCha20Poly1305:
                if (dekKeyMaterialBytes != 32)
                    throw new InvalidDataException($"Invalid DEK key material length {dekKeyMaterialBytes} for XChaCha20-Poly1305. Expected 32 bytes.");
                break;
            default:
                throw new InvalidDataException($"Unsupported DEK algorithm for two-key envelope: {(EncryptionAlgorithm)dekAlgorithmId} ({dekAlgorithmId}).");
        }
    }

    /// <summary>Throws if plaintext DEK length does not match the header-declared length.</summary>
    public static void ValidatePlaintextDekLength(byte[] plaintextDek, int expectedBytes)
    {
        if (plaintextDek.Length != expectedBytes)
            throw new InvalidDataException(
                $"Decrypted DEK length {plaintextDek.Length} does not match header-declared DEK key material length {expectedBytes}.");
    }
    
    /// <summary>
    ///     Describes valid plaintext key material sizes for a symmetric <see cref="EncryptionAlgorithm"/> (same rules as DEK/KEK when
    ///     both are symmetric). KEK material size is not stored in the two-key header; this lists what the unwrap key must be.
    /// </summary>
    public static string DescribeValidSymmetricKeyMaterialSizes(byte algorithmId)
    {
        var alg = (EncryptionAlgorithm)algorithmId;
        return alg switch {
            EncryptionAlgorithm.AesGcm => "16, 24, or 32 (AES-GCM)",
            EncryptionAlgorithm.ChaCha20Poly1305 => "32 (ChaCha20-Poly1305)",
            EncryptionAlgorithm.AesCcm => "16, 24, or 32 (AES-CCM)",
            EncryptionAlgorithm.AesSiv => "32, 48, or 64 (AES-SIV)",
            EncryptionAlgorithm.XChaCha20Poly1305 => "32 (XChaCha20-Poly1305)",
            EncryptionAlgorithm.Rsa =>
                "N/A in header — RSA wraps the DEK; ciphertext length varies with key size (plaintext DEK length is given by DEK key material bytes).",
            EncryptionAlgorithm.AesGcmRsa =>
                "Hybrid RSA + AES-GCM — KEK material follows AES-GCM key sizes when using the symmetric portion; see keystore.",
            _ => $"Unsupported or non-symmetric algorithm id: {(EncryptionAlgorithm)algorithmId} ({algorithmId})."
        };
    }
}
