using Lyo.Common.Metadata.Records;
using Lyo.Encryption.Models;
using Lyo.Encryption.Streaming;
using Lyo.Exceptions;
using Lyo.KeyStore;

namespace Lyo.Encryption.AesSiv;

/// <summary>
/// AES-SIV (RFC 5297) via Dorssel.Security.Cryptography.AesExtra. The 16-byte synthetic IV lives in the header "nonce" field; ciphertext is CTR payload only (no
/// separate tag). V1 uses empty associated data (<see cref="ReadOnlySpan{T}.Empty" />).
/// </summary>
public class AesSivEncryptionService : EncryptionServiceBase, ISymmetricKeyMaterialSize
{
    public AesSivKeySizeBits AesSivKeySize { get; }

    public AesSivEncryptionService(IKeyStore keyStore)
        : this(keyStore, AesSivKeySizeBits.Bits256) { }

    public AesSivEncryptionService(IKeyStore keyStore, AesSivKeySizeBits keySize)
        : base(
            new() {
                CurrentFormatVersion = (byte)StreamFormatVersion.V1,
                MaxInputSize = long.MaxValue,
                MinInputSize = 1,
                FileExtension = FileTypeInfo.LyoAesSiv.DefaultExtension
            }, keyStore)
        => AesSivKeySize = keySize;

    public AesSivEncryptionService(EncryptionServiceOptions options, IKeyStore keyStore)
        : this(options, keyStore, AesSivKeySizeBits.Bits256) { }

    public AesSivEncryptionService(EncryptionServiceOptions options, IKeyStore keyStore, AesSivKeySizeBits keySize)
        : base(options, keyStore)
        => AesSivKeySize = keySize;

    public int RequiredKeyBytes => AesSivKeySize.GetKeyLengthBytes();

    /// <inheritdoc />
    protected override EncryptionAlgorithmInfo AlgorithmInfo => EncryptionAlgorithmInfo.AesSiv;

    protected override byte GetAlgorithmId() => (byte)EncryptionAlgorithm.AesSiv;

    /// <summary>Builds a per-stream AES-SIV cipher bound to <paramref name="key" /> for the deterministic chunk loop.</summary>
    public override IAeadStreamCryptor CreateStreamCryptor(ReadOnlySpan<byte> key)
    {
        ArgumentHelpers.ThrowIf(key.Length != RequiredKeyBytes, $"AES-SIV key must be exactly {RequiredKeyBytes} bytes for the configured key size.", nameof(key));
        return new AesSivStreamCryptor(key);
    }

    /// <inheritdoc />
    protected override void ValidateSymmetricKey(byte[] key)
        => ArgumentHelpers.ThrowIf(key.Length != RequiredKeyBytes, $"AES-SIV key must be exactly {RequiredKeyBytes} bytes for the configured key size.", nameof(key));

    /// <inheritdoc />
    protected override void Seal(
        ReadOnlySpan<byte> plaintext,
        byte[] key,
        Span<byte> nonce,
        Span<byte> authenticator,
        Span<byte> payload,
        Span<byte> body,
        byte[]? associatedData)
    {
        using var siv = new Dorssel.Security.Cryptography.AesSiv(key);
        siv.Encrypt(plaintext, body, associatedData is { Length: > 0 } ? associatedData : ReadOnlySpan<byte>.Empty);
    }

    /// <inheritdoc />
    protected override void Open(
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> authenticator,
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> body,
        byte[] key,
        Span<byte> plaintext,
        byte[]? associatedData)
    {
        using var siv = new Dorssel.Security.Cryptography.AesSiv(key);
        siv.Decrypt(body, plaintext, associatedData is { Length: > 0 } ? associatedData : ReadOnlySpan<byte>.Empty);
    }
}
