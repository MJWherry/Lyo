using Lyo.Common.Metadata.Records;
using Lyo.Encryption.Models;
using Lyo.Encryption.Streaming;
using Lyo.KeyStore;

namespace Lyo.Encryption.AesCcm;

/// <summary>
/// AES-CCM authenticated encryption (12-byte nonce, 128-bit tag). Every target framework uses BouncyCastle so the wire format stays identical.
/// Single-shot buffer encrypt stops at <see cref="AesCcmHelper.MaxPlaintextLength" /> (~16 MiB); larger payloads go through the streaming APIs.
/// </summary>
public class AesCcmEncryptionService : EncryptionServiceBase, ISymmetricKeyMaterialSize
{
    public AesCcmEncryptionService(IKeyStore keyStore)
        : this(keyStore, AesGcmKeySizeBits.Bits256) { }

    public AesCcmEncryptionService(IKeyStore keyStore, AesGcmKeySizeBits aesKeySize)
        : base(
            new() {
                CurrentFormatVersion = (byte)StreamFormatVersion.V1,
                MaxInputSize = long.MaxValue,
                MinInputSize = 1,
                FileExtension = FileTypeInfo.LyoAesCcm.DefaultExtension,
                AesGcmKeySize = aesKeySize
            }, keyStore) { }

    public AesCcmEncryptionService(EncryptionServiceOptions options, IKeyStore keyStore)
        : base(options, keyStore) { }

    public int RequiredKeyBytes => Options.AesGcmKeySize.GetKeyLengthBytes();

    /// <inheritdoc />
    protected override EncryptionAlgorithmInfo AlgorithmInfo => EncryptionAlgorithmInfo.AesCcm;

    protected override byte GetAlgorithmId() => (byte)EncryptionAlgorithm.AesCcm;

    /// <summary>Builds a per-stream AES-CCM cipher bound to <paramref name="key" /> for the chunk loop.</summary>
    public override IAeadStreamCryptor CreateStreamCryptor(ReadOnlySpan<byte> key)
    {
        AesCcmHelper.ValidateKeyLength(key, RequiredKeyBytes);
        return new AesCcmStreamCryptor(key);
    }

    /// <inheritdoc />
    protected override void ValidateEncryptPlaintext(int length, string paramName) => AesCcmHelper.ValidatePlaintextLength(length, paramName);

    /// <inheritdoc />
    protected override void ValidateSymmetricKey(byte[] key) => AesCcmHelper.ValidateKeyLength(key, RequiredKeyBytes);

    /// <inheritdoc />
    protected override void Seal(
        ReadOnlySpan<byte> plaintext,
        byte[] key,
        Span<byte> nonce,
        Span<byte> authenticator,
        Span<byte> payload,
        Span<byte> body,
        byte[]? associatedData)
        => AesCcmHelper.Encrypt(plaintext, key, nonce, payload, authenticator, associatedData);

    /// <inheritdoc />
    protected override void Open(
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> authenticator,
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> body,
        byte[] key,
        Span<byte> plaintext,
        byte[]? associatedData)
        => AesCcmHelper.Decrypt(ciphertext, authenticator, key, nonce, plaintext, associatedData);
}
