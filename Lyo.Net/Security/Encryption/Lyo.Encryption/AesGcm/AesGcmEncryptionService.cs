using Lyo.Common.Metadata.Records;
using Lyo.Encryption.Models;
using Lyo.Encryption.Streaming;
using Lyo.KeyStore;

namespace Lyo.Encryption.AesGcm;

/// <summary>
/// Encrypts and decrypts with AES-GCM. Keys come from a KeyStore; each encrypt draws a fresh nonce.
/// Authenticated encryption covers confidentiality and integrity when a shared secret is available and throughput matters.
/// Thread-safe: concurrent calls on one instance are safe because each call owns its own cryptographic context
/// (nonce, key material). If the KeyStore itself is not thread-safe, synchronize at the KeyStore level.
/// </summary>
public class AesGcmEncryptionService : EncryptionServiceBase, ISymmetricKeyMaterialSize
{
    /// <summary>Creates a service for the given AES-GCM key width.</summary>
    /// <param name="keyStore">Store used to resolve encryption keys</param>
    /// <param name="aesGcmKeySize">AES key width (128, 192, or 256 bits).</param>
    public AesGcmEncryptionService(IKeyStore keyStore, AesGcmKeySizeBits aesGcmKeySize = AesGcmKeySizeBits.Bits256)
        : base(
            new() {
                CurrentFormatVersion = (byte)StreamFormatVersion.V1,
                MaxInputSize = long.MaxValue,
                MinInputSize = 1,
                FileExtension = FileTypeInfo.LyoAesGcm.DefaultExtension,
                AesGcmKeySize = aesGcmKeySize
            }, keyStore) { }

    /// <summary>
    /// Creates a service from explicit options (set <see cref="EncryptionServiceOptions.FileExtension" /> and <see cref="EncryptionServiceOptions.AesGcmKeySize" />
    /// as needed).
    /// </summary>
    public AesGcmEncryptionService(EncryptionServiceOptions options, IKeyStore keyStore)
        : base(options, keyStore) { }

    /// <inheritdoc cref="ISymmetricKeyMaterialSize.RequiredKeyBytes" />
    public int RequiredKeyBytes => Options.AesGcmKeySize.GetKeyLengthBytes();

    /// <inheritdoc />
    protected override EncryptionAlgorithmInfo AlgorithmInfo => EncryptionAlgorithmInfo.AesGcm;

    /// <summary>Builds a per-stream AES-GCM cipher bound to <paramref name="key" /> for the allocation-free chunk loop.</summary>
    public override IAeadStreamCryptor CreateStreamCryptor(ReadOnlySpan<byte> key)
    {
        AesGcmHelper.ValidateKeyLength(key, RequiredKeyBytes);
        return new AesGcmStreamCryptor(key);
    }

    /// <summary>Algorithm id written into the stream-format header.</summary>
    protected override byte GetAlgorithmId() => (byte)EncryptionAlgorithm.AesGcm;

    /// <inheritdoc />
    protected override void ValidateSymmetricKey(byte[] key) => AesGcmHelper.ValidateKeyLength(key, RequiredKeyBytes);

    /// <inheritdoc />
    protected override void Seal(
        ReadOnlySpan<byte> plaintext,
        byte[] key,
        Span<byte> nonce,
        Span<byte> authenticator,
        Span<byte> payload,
        Span<byte> body,
        byte[]? associatedData)
        => AesGcmHelper.Encrypt(plaintext, key, nonce, payload, authenticator, associatedData);

    /// <inheritdoc />
    protected override void Open(
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> authenticator,
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> body,
        byte[] key,
        Span<byte> plaintext,
        byte[]? associatedData)
        => AesGcmHelper.Decrypt(ciphertext, authenticator, key, nonce, plaintext, associatedData);
}
