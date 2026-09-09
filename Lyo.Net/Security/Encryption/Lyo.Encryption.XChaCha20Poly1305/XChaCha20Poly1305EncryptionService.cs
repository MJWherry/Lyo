using Lyo.Common.Metadata.Records;
using Lyo.Encryption.Models;
using Lyo.Encryption.Streaming;
using Lyo.Exceptions;
using Lyo.KeyStore;

namespace Lyo.Encryption.XChaCha20Poly1305;

/// <summary>XChaCha20-Poly1305 (24-byte nonce, 32-byte key, 128-bit tag). Portable path uses HChaCha20 subkey derivation and BouncyCastle ChaCha20-Poly1305 (IETF).</summary>
public class XChaCha20Poly1305EncryptionService : EncryptionServiceBase, ISymmetricKeyMaterialSize
{
    public XChaCha20Poly1305EncryptionService(IKeyStore keyStore)
        : base(
            new() {
                CurrentFormatVersion = (byte)StreamFormatVersion.V1,
                MaxInputSize = long.MaxValue,
                MinInputSize = 1,
                FileExtension = FileTypeInfo.LyoXChaCha20Poly1305.DefaultExtension
            }, keyStore) { }

    public XChaCha20Poly1305EncryptionService(EncryptionServiceOptions options, IKeyStore keyStore)
        : base(options, keyStore) { }

    public int RequiredKeyBytes => 32;

    /// <inheritdoc />
    protected override EncryptionAlgorithmInfo AlgorithmInfo => EncryptionAlgorithmInfo.XChaCha20Poly1305;

    protected override byte GetAlgorithmId() => (byte)EncryptionAlgorithm.XChaCha20Poly1305;

    /// <summary>Builds a per-stream XChaCha20-Poly1305 cipher bound to <paramref name="key" /> for the chunk loop.</summary>
    public override IAeadStreamCryptor CreateStreamCryptor(ReadOnlySpan<byte> key)
    {
        ArgumentHelpers.ThrowIfNotInRange(key.Length, 32, 32, nameof(key), $"XChaCha20-Poly1305 key must be exactly 32 bytes; got {key.Length}.");
        return new XChaCha20Poly1305StreamCryptor(key);
    }

    /// <inheritdoc />
    protected override void ValidateSymmetricKey(byte[] key) => ArgumentHelpers.ThrowIfNotInRange(key, RequiredKeyBytes, RequiredKeyBytes);

    /// <inheritdoc />
    protected override void Seal(
        ReadOnlySpan<byte> plaintext,
        byte[] key,
        Span<byte> nonce,
        Span<byte> authenticator,
        Span<byte> payload,
        Span<byte> body,
        byte[]? associatedData)
        => XChaCha20Poly1305Helper.Encrypt(plaintext, key, nonce, payload, authenticator, associatedData);

    /// <inheritdoc />
    protected override void Open(
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> authenticator,
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> body,
        byte[] key,
        Span<byte> plaintext,
        byte[]? associatedData)
        => XChaCha20Poly1305Helper.Decrypt(ciphertext, authenticator, key, nonce, plaintext, associatedData);
}
