using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Lyo.Common.Records;
using Lyo.Encryption.Exceptions;
using Lyo.Encryption.Streaming;
using Lyo.Exceptions;
using Lyo.Keystore;

namespace Lyo.Encryption.AesCcm;

/// <summary>
/// AES-CCM authenticated encryption (12-byte nonce, 128-bit tag). On all target frameworks the implementation uses BouncyCastle for identical wire-format behavior.
/// Single-shot buffer encrypt is capped at <see cref="AesCcmHelper.MaxPlaintextLength"/> (~16 MiB); use streaming APIs for larger payloads.
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

    protected override byte GetAlgorithmId() => (byte)EncryptionAlgorithm.AesCcm;

    /// <summary>Creates a per-stream AES-CCM cipher bound to <paramref name="key" /> for the streaming chunk loop.</summary>
    public override IAeadStreamCryptor CreateStreamCryptor(ReadOnlySpan<byte> key)
    {
        AesCcmHelper.ValidateKeyLength(key, RequiredKeyBytes);
        return new AesCcmStreamCryptor(key);
    }

    /// <inheritdoc cref="IEncryptionService.Encrypt(ReadOnlySpan{byte}, string?, byte[], byte[])" />
    public override byte[] Encrypt(ReadOnlySpan<byte> plaintext, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIfNotInRange((long)plaintext.Length, Options.MinInputSize, Options.MaxInputSize, nameof(plaintext));
        AesCcmHelper.ValidatePlaintextLength(plaintext.Length);
        return EncryptCore(plaintext, keyId, key, associatedData);
    }

    public override byte[] Encrypt(byte[] bytes, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIfNotInRange(bytes, Options.MinInputSize, Options.MaxInputSize);
        AesCcmHelper.ValidatePlaintextLength(bytes.Length);
        return EncryptCore(bytes, keyId, key, associatedData);
    }

    public override byte[] Decrypt(byte[] encryptedBytes, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        const int minEncryptedSize = 38;
        ArgumentHelpers.ThrowIfNotInRange(encryptedBytes, minEncryptedSize, Options.MaxInputSize);
        return DecryptCore(encryptedBytes, keyId, key, associatedData);
    }

    /// <inheritdoc cref="IEncryptionService.Decrypt(byte[], int, int, string?, byte[], byte[])" />
    public override byte[] Decrypt(byte[] buffer, int offset, int count, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
        => DecryptChunk(buffer, offset, count, keyId, key, associatedData);

    protected override byte[] DecryptChunk(byte[] buffer, int offset, int count, string? keyId, byte[]? key, byte[]? associatedData = null)
    {
        const int minEncryptedSize = 38;
        ArgumentHelpers.ThrowIfNotInRange((long)count, minEncryptedSize, Options.MaxInputSize, nameof(count));
        return DecryptCore(buffer.AsSpan(offset, count), keyId, key, associatedData);
    }

    private byte[] EncryptCore(ReadOnlySpan<byte> plaintext, string? keyId, byte[]? key, byte[]? associatedData)
    {
        if (key != null)
            AesCcmHelper.ValidateKeyLength(key, RequiredKeyBytes);

        byte[]? actualKey = null;
        string? keyVersion = null;
        if (key != null)
            actualKey = key;
        else if (keyId != null && KeyStore != null) {
            actualKey = KeyStore.GetCurrentKey(keyId);
            OperationHelpers.ThrowIfNull(actualKey, $"No encryption key available for key ID '{keyId}'. Ensure a key is configured.");
            AesCcmHelper.ValidateKeyLength(actualKey, RequiredKeyBytes);
            keyVersion = KeyStore.GetCurrentVersion(keyId);
        }
        else
            OperationHelpers.ThrowIf(true, "No encryption key available. Provide either a keyId or a key parameter.");

        var formatVersion = Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1;
        var keyIdBytes = keyId != null && !string.IsNullOrWhiteSpace(keyVersion) ? Encoding.UTF8.GetBytes(keyId) : [];
        var versionString = keyVersion ?? "";
        var prefixLen = 1 + 4 + keyIdBytes.Length + GetBinaryWriterStringByteCount(versionString) + 4 + AesCcmHelper.NonceSize;
        var result = new byte[prefixLen + AesCcmHelper.TagSize + plaintext.Length];

        var o = 0;
        result[o++] = formatVersion;
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(o, 4), keyIdBytes.Length);
        o += 4;
        if (keyIdBytes.Length > 0) {
            keyIdBytes.CopyTo(result.AsSpan(o));
            o += keyIdBytes.Length;
        }

        o += WriteBinaryWriterString(result.AsSpan(o), versionString);
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(o, 4), AesCcmHelper.NonceSize);
        o += 4;
        var nonceSpan = result.AsSpan(o, AesCcmHelper.NonceSize);
        CryptographicRandom.Fill(nonceSpan);
        o += AesCcmHelper.NonceSize;
        var tagSpan = result.AsSpan(o, AesCcmHelper.TagSize);
        o += AesCcmHelper.TagSize;
        var ctSpan = result.AsSpan(o, plaintext.Length);
        AesCcmHelper.Encrypt(plaintext, actualKey!, nonceSpan, ctSpan, tagSpan, associatedData);
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
            throw new InvalidDataException("Invalid encrypted data format: insufficient data for nonce length.");

        var nonceLength = BinaryPrimitives.ReadInt32LittleEndian(encrypted.Slice(o, 4));
        o += 4;
        ArgumentHelpers.ThrowIfNotInRange(
            nonceLength, AesCcmHelper.NonceSize, AesCcmHelper.NonceSize, nameof(encrypted),
            $"Invalid nonce length: {nonceLength}. Expected {AesCcmHelper.NonceSize} bytes.");

        if (encrypted.Length - o < nonceLength + AesCcmHelper.TagSize)
            throw new InvalidDataException("Invalid encrypted data format: truncated nonce or tag.");

        var nonce = encrypted.Slice(o, nonceLength);
        o += nonceLength;
        var tag = encrypted.Slice(o, AesCcmHelper.TagSize);
        o += AesCcmHelper.TagSize;
        var ciphertext = encrypted[o..];

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

                AesCcmHelper.ValidateKeyLength(actualKey, RequiredKeyBytes);
            }
            else
                OperationHelpers.ThrowIf(true, "No decryption key available. Provide either a keyId or a key parameter.");
        }

        if (key != null)
            AesCcmHelper.ValidateKeyLength(key, RequiredKeyBytes);

        var plaintext = new byte[ciphertext.Length];
        try {
            AesCcmHelper.Decrypt(ciphertext, tag, actualKey!, nonce, plaintext, associatedData);
            return plaintext;
        }
#if NET10_0_OR_GREATER
        catch (AuthenticationTagMismatchException ex) {
            throw new DecryptionFailedException("Decryption failed due to authentication tag mismatch. Possible causes: wrong key, corrupted data, or tampered data.", ex);
        }
#endif
        catch (CryptographicException ex) {
            throw new DecryptionFailedException("Decryption failed. Possible causes: wrong key, corrupted data, or authentication failure.", ex);
        }
    }
}
