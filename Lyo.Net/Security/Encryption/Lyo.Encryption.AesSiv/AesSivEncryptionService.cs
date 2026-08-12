using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Lyo.Common.Records;
using Lyo.Encryption.Exceptions;
using Lyo.Encryption.Streaming;
using Lyo.Exceptions;
using Lyo.KeyStore;

namespace Lyo.Encryption.AesSiv;

/// <summary>
/// AES-SIV (RFC 5297) via Dorssel.Security.Cryptography.AesExtra. The 16-byte synthetic IV is stored in the header "nonce" field; ciphertext is the CTR payload only (no
/// separate tag). V1 uses empty associated data (<see cref="ReadOnlySpan{T}.Empty" />).
/// </summary>
public class AesSivEncryptionService : EncryptionServiceBase, ISymmetricKeyMaterialSize
{
    private const int SivSize = 16;

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

    protected override byte GetAlgorithmId() => (byte)EncryptionAlgorithm.AesSiv;

    /// <summary>Creates a per-stream AES-SIV cipher bound to <paramref name="key" /> for the deterministic streaming chunk loop.</summary>
    public override IAeadStreamCryptor CreateStreamCryptor(ReadOnlySpan<byte> key)
    {
        ArgumentHelpers.ThrowIf(key.Length != RequiredKeyBytes, $"AES-SIV key must be exactly {RequiredKeyBytes} bytes for the configured key size.", nameof(key));
        return new AesSivStreamCryptor(key);
    }

    /// <inheritdoc cref="IEncryptionService.Encrypt(ReadOnlySpan{byte}, string?, byte[], byte[])" />
    public override byte[] Encrypt(ReadOnlySpan<byte> plaintext, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIfNotInRange((long)plaintext.Length, Options.MinInputSize, Options.MaxInputSize, nameof(plaintext));
        return EncryptCore(plaintext, keyId, key, associatedData);
    }

    public override byte[] Encrypt(byte[] bytes, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIfNotInRange(bytes, Options.MinInputSize, Options.MaxInputSize);
        return EncryptCore(bytes, keyId, key, associatedData);
    }

    public override byte[] Decrypt(byte[] encryptedBytes, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        const int minEncryptedSize = 27;
        ArgumentHelpers.ThrowIfNotInRange(encryptedBytes, minEncryptedSize, Options.MaxInputSize);
        return DecryptCore(encryptedBytes, keyId, key, associatedData);
    }

    /// <inheritdoc cref="IEncryptionService.Decrypt(byte[], int, int, string?, byte[], byte[])" />
    public override byte[] Decrypt(byte[] buffer, int offset, int count, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
        => DecryptChunk(buffer, offset, count, keyId, key, associatedData);

    protected override byte[] DecryptChunk(byte[] buffer, int offset, int count, string? keyId, byte[]? key, byte[]? associatedData = null)
    {
        const int minEncryptedSize = 27;
        ArgumentHelpers.ThrowIfNotInRange((long)count, minEncryptedSize, Options.MaxInputSize, nameof(count));
        return DecryptCore(buffer.AsSpan(offset, count), keyId, key, associatedData);
    }

    private byte[] EncryptCore(ReadOnlySpan<byte> plaintext, string? keyId, byte[]? key, byte[]? associatedData)
    {
        if (key != null)
            ValidateKey(key);

        byte[]? actualKey = null;
        string? keyVersion = null;
        if (key != null)
            actualKey = key;
        else if (keyId != null && KeyStore != null) {
            actualKey = KeyStore.GetCurrentKey(keyId);
            OperationHelpers.ThrowIfNull(actualKey, $"No encryption key available for key ID '{keyId}'. Ensure a key is configured.");
            ValidateKey(actualKey);
            keyVersion = KeyStore.GetCurrentVersion(keyId);
        }
        else
            OperationHelpers.ThrowIf(true, "No encryption key available. Provide either a keyId or a key parameter.");

        var formatVersion = Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1;
        var keyIdBytes = keyId != null && !string.IsNullOrWhiteSpace(keyVersion) ? Encoding.UTF8.GetBytes(keyId) : [];
        var versionString = keyVersion ?? "";
        // prefix through nonceLen; SIV‖ciphertext written contiguously after that (tagSize=0).
        var prefixLen = 1 + 4 + keyIdBytes.Length + GetBinaryWriterStringByteCount(versionString) + 4;
        var result = new byte[prefixLen + SivSize + plaintext.Length];
        var o = 0;
        result[o++] = formatVersion;
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(o, 4), keyIdBytes.Length);
        o += 4;
        if (keyIdBytes.Length > 0) {
            keyIdBytes.CopyTo(result.AsSpan(o));
            o += keyIdBytes.Length;
        }

        o += WriteBinaryWriterString(result.AsSpan(o), versionString);
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(o, 4), SivSize);
        o += 4;
        // Dorssel writes [SIV(16)][ciphertext] into this contiguous region.
        using (var siv = new Dorssel.Security.Cryptography.AesSiv(actualKey!))
            siv.Encrypt(plaintext, result.AsSpan(o, SivSize + plaintext.Length), associatedData is { Length: > 0 } ? associatedData : ReadOnlySpan<byte>.Empty);

        return result;
    }

    private byte[] DecryptCore(ReadOnlySpan<byte> encrypted, string? keyId, byte[]? key, byte[]? associatedData)
    {
        var o = 0;
        if (encrypted.Length < 1)
            throw new InvalidDataException("Invalid encrypted data format: insufficient data for format version.");

        var firstByte = encrypted[o++];
        var expectedFormatVersion = Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1;
        if (firstByte != expectedFormatVersion)
            throw new InvalidDataException($"Invalid encrypted data format: expected format version {expectedFormatVersion}, got {firstByte}.");

        if (encrypted.Length - o < 4)
            throw new InvalidDataException("Invalid encrypted data format: insufficient data for keyId length.");

        var keyIdLength = BinaryPrimitives.ReadInt32LittleEndian(encrypted.Slice(o, 4));
        o += 4;
        if (keyIdLength < 0 || keyIdLength > 1024)
            throw new InvalidDataException($"Invalid key ID length: {keyIdLength}. Maximum allowed: 1024 bytes.");

        string? headerKeyId = null;
        if (keyIdLength > 0) {
            if (encrypted.Length - o < keyIdLength)
                throw new InvalidDataException("Invalid encrypted data format: keyId length exceeds remaining data.");

            headerKeyId = Encoding.UTF8.GetString(encrypted.Slice(o, keyIdLength).ToArray());
            o += keyIdLength;
        }

        if (o >= encrypted.Length)
            throw new InvalidDataException("Invalid encrypted data format: insufficient data for keyVersion.");

        var headerKeyVersion = ReadBinaryWriterString(encrypted[o..], out var versionBytes);
        o += versionBytes;
        if (string.IsNullOrWhiteSpace(headerKeyVersion))
            headerKeyVersion = null;

        if (encrypted.Length - o < 4)
            throw new InvalidDataException("Invalid encrypted data format: insufficient data for synthetic IV length.");

        var nonceLength = BinaryPrimitives.ReadInt32LittleEndian(encrypted.Slice(o, 4));
        o += 4;
        ArgumentHelpers.ThrowIfNotInRange(nonceLength, SivSize, SivSize, nameof(encrypted), $"Invalid synthetic IV length: {nonceLength}. Expected {SivSize} bytes.");
        if (encrypted.Length - o < SivSize)
            throw new InvalidDataException("Invalid encrypted data format: truncated synthetic IV or ciphertext.");

        // Contiguous SIV‖body as required by Dorssel.
        var combined = encrypted[o..];
        var bodyLength = combined.Length - SivSize;
        byte[]? actualKey = null;
        if (key != null)
            actualKey = key;
        else {
            var actualKeyId = headerKeyId ?? keyId;
            var actualKeyVersion = headerKeyVersion;
            if (actualKeyId != null && KeyStore != null) {
                if (!string.IsNullOrWhiteSpace(actualKeyVersion)) {
                    actualKey = KeyStore.GetKey(actualKeyId, actualKeyVersion);
                    OperationHelpers.ThrowIfNull(
                        actualKey, $"No decryption key available for key ID '{actualKeyId}' version {actualKeyVersion}. Ensure the key version exists in KeyStore.");
                }
                else {
                    actualKey = KeyStore.GetCurrentKey(actualKeyId);
                    OperationHelpers.ThrowIfNull(actualKey, $"No decryption key available for key ID '{actualKeyId}'. Ensure a key is configured.");
                }

                ValidateKey(actualKey);
            }
            else
                OperationHelpers.ThrowIf(true, "No decryption key available. Provide either a keyId or a key parameter.");
        }

        if (key != null)
            ValidateKey(key);

        var plaintext = new byte[bodyLength];
        try {
            using var siv = new Dorssel.Security.Cryptography.AesSiv(actualKey!);
            siv.Decrypt(combined, plaintext, associatedData is { Length: > 0 } ? associatedData : ReadOnlySpan<byte>.Empty);
            return plaintext;
        }
        catch (CryptographicException ex) {
            throw new DecryptionFailedException("Decryption failed. Possible causes: wrong key, corrupted data, or authentication failure.", ex);
        }
    }

    private void ValidateKey(byte[] k)
        => ArgumentHelpers.ThrowIf(k.Length != RequiredKeyBytes, $"AES-SIV key must be exactly {RequiredKeyBytes} bytes for the configured key size.", nameof(k));
}