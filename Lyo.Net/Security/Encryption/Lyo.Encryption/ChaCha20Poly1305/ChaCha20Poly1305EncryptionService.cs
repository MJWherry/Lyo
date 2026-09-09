using Lyo.Common.Metadata.Records;
using Lyo.Encryption.Models;
using Lyo.Encryption.Streaming;
using Lyo.Exceptions;
using Lyo.KeyStore;

namespace Lyo.Encryption.ChaCha20Poly1305;

/// <summary>
/// Encrypts and decrypts with ChaCha20-Poly1305, a modern AEAD that
/// combines high throughput with strong integrity. Nonce is 12 bytes and the tag is 16 bytes. Keys come from a KeyStore so
/// rotation stays outside the caller. Thread-safe: concurrent calls on one instance are safe because each call owns its cryptographic
/// context (nonce, key material) and shares no mutable crypto state. If the KeyStore itself is not thread-safe, synchronize at the KeyStore
/// level.
/// </summary>
public class ChaCha20Poly1305EncryptionService : EncryptionServiceBase, ISymmetricKeyMaterialSize
{
    /// <summary>Creates a ChaCha20Poly1305EncryptionService.</summary>
    /// <param name="keyStore">Store used to resolve encryption keys</param>
    /// <exception cref="ArgumentNullException">Thrown when keyStore is null</exception>
    /// <remarks>Default options: CurrentFormatVersion=V1, MaxInputSize=long.MaxValue, MinInputSize=1, FileExtension=".chacha"</remarks>
    public ChaCha20Poly1305EncryptionService(IKeyStore keyStore)
        : base(
            new() {
                CurrentFormatVersion = (byte)StreamFormatVersion.V1,
                MaxInputSize = long.MaxValue,
                MinInputSize = 1,
                FileExtension = FileTypeInfo.LyoChaCha20Poly1305.DefaultExtension
            }, keyStore) { }

    /// <inheritdoc cref="ISymmetricKeyMaterialSize.RequiredKeyBytes" />
    public int RequiredKeyBytes => 32;

    /// <inheritdoc />
    protected override EncryptionAlgorithmInfo AlgorithmInfo => EncryptionAlgorithmInfo.ChaCha20Poly1305;

    /// <summary>Builds a per-stream ChaCha20-Poly1305 cipher bound to <paramref name="key" /> for the allocation-free chunk loop.</summary>
    public override IAeadStreamCryptor CreateStreamCryptor(ReadOnlySpan<byte> key)
    {
        ArgumentHelpers.ThrowIfNotInRange(key.Length, 32, 32, nameof(key), $"ChaCha20-Poly1305 key must be exactly 32 bytes; got {key.Length}.");
        return new ChaCha20Poly1305StreamCryptor(key);
    }

    /// <summary>Algorithm id written into the stream-format header.</summary>
    protected override byte GetAlgorithmId() => (byte)EncryptionAlgorithm.ChaCha20Poly1305;

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
        => ChaCha20Poly1305Helper.Encrypt(plaintext, key, nonce, payload, authenticator, associatedData);

    /// <inheritdoc />
    protected override void Open(
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> authenticator,
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> body,
        byte[] key,
        Span<byte> plaintext,
        byte[]? associatedData)
        => ChaCha20Poly1305Helper.Decrypt(ciphertext, authenticator, key, nonce, plaintext, associatedData);
}
