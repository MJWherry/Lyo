using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Lyo.Common.Records;
using Lyo.Encryption.Exceptions;
using Lyo.Encryption.Streaming;
using Lyo.Exceptions;
using Lyo.KeyStore;

namespace Lyo.Encryption.XChaCha20Poly1305;

/// <summary>XChaCha20-Poly1305 (24-byte nonce, 32-byte key, 128-bit tag). Portable implementation uses HChaCha20 subkey derivation and BouncyCastle ChaCha20-Poly1305 (IETF).</summary>
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

    protected override byte GetAlgorithmId() => (byte)EncryptionAlgorithm.XChaCha20Poly1305;

    /// <summary>Creates a per-stream XChaCha20-Poly1305 cipher bound to <paramref name="key" /> for the streaming chunk loop.</summary>
    public override IAeadStreamCryptor CreateStreamCryptor(ReadOnlySpan<byte> key)
    {
        ArgumentHelpers.ThrowIfNotInRange(key.Length, 32, 32, nameof(key), $"XChaCha20-Poly1305 key must be exactly 32 bytes; got {key.Length}.");
        return new XChaCha20Poly1305StreamCryptor(key);
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
        const int minEncryptedSize = 50;
        ArgumentHelpers.ThrowIfNotInRange(encryptedBytes, minEncryptedSize, Options.MaxInputSize);
        return DecryptCore(encryptedBytes, keyId, key, associatedData);
    }

    /// <inheritdoc cref="IEncryptionService.Decrypt(byte[], int, int, string?, byte[], byte[])" />
    public override byte[] Decrypt(byte[] buffer, int offset, int count, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
        => DecryptChunk(buffer, offset, count, keyId, key, associatedData);

    protected override byte[] DecryptChunk(byte[] buffer, int offset, int count, string? keyId, byte[]? key, byte[]? associatedData = null)
    {
        const int minEncryptedSize = 50;
        ArgumentHelpers.ThrowIfNotInRange((long)count, minEncryptedSize, Options.MaxInputSize, nameof(count));
        return DecryptCore(buffer.AsSpan(offset, count), keyId, key, associatedData);
    }

    private byte[] EncryptCore(ReadOnlySpan<byte> plaintext, string? keyId, byte[]? key, byte[]? associatedData)
    {
        if (key != null)
            ArgumentHelpers.ThrowIfNotInRange(key, 32, 32);

        byte[]? actualKey = null;
        string? keyVersion = null;
        if (key != null)
            actualKey = key;
        else if (keyId != null && KeyStore != null) {
            actualKey = KeyStore.GetCurrentKey(keyId);
            OperationHelpers.ThrowIfNull(actualKey, $"No encryption key found in KeyStore for key ID '{keyId}'. Ensure the key ID is correct and a key is configured.");
            keyVersion = KeyStore.GetCurrentVersion(keyId);
        }
        else
            OperationHelpers.ThrowIf(true, "No encryption key available. Provide either a keyId or a key parameter.");

        var formatVersion = Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1;
        var keyIdBytes = keyId != null && !string.IsNullOrWhiteSpace(keyVersion) ? Encoding.UTF8.GetBytes(keyId) : [];
        var versionString = keyVersion ?? "";
        var prefixLen = 1 + 4 + keyIdBytes.Length + GetBinaryWriterStringByteCount(versionString) + 4 + XChaCha20Poly1305Helper.NonceSize;
        var result = new byte[prefixLen + XChaCha20Poly1305Helper.TagSize + plaintext.Length];

        var o = 0;
        result[o++] = formatVersion;
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(o, 4), keyIdBytes.Length);
        o += 4;
        if (keyIdBytes.Length > 0) {
            keyIdBytes.CopyTo(result.AsSpan(o));
            o += keyIdBytes.Length;
        }

        o += WriteBinaryWriterString(result.AsSpan(o), versionString);
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(o, 4), XChaCha20Poly1305Helper.NonceSize);
        o += 4;
        var nonceSpan = result.AsSpan(o, XChaCha20Poly1305Helper.NonceSize);
        CryptographicRandom.Fill(nonceSpan);
        o += XChaCha20Poly1305Helper.NonceSize;
        var tagSpan = result.AsSpan(o, XChaCha20Poly1305Helper.TagSize);
        o += XChaCha20Poly1305Helper.TagSize;
        var ctSpan = result.AsSpan(o, plaintext.Length);
        XChaCha20Poly1305Helper.Encrypt(plaintext, actualKey!, nonceSpan, ctSpan, tagSpan, associatedData);
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
            nonceLength, XChaCha20Poly1305Helper.NonceSize, XChaCha20Poly1305Helper.NonceSize, nameof(encrypted),
            $"Invalid nonce length: {nonceLength}. Expected {XChaCha20Poly1305Helper.NonceSize} bytes.");

        if (encrypted.Length - o < nonceLength + XChaCha20Poly1305Helper.TagSize)
            throw new InvalidDataException("Invalid encrypted data format: truncated nonce or tag.");

        var nonce = encrypted.Slice(o, nonceLength);
        o += nonceLength;
        var tag = encrypted.Slice(o, XChaCha20Poly1305Helper.TagSize);
        o += XChaCha20Poly1305Helper.TagSize;
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
                        actualKey, $"No decryption key found in KeyStore for key ID '{actualKeyId}' version {actualKeyVersion}. Ensure the key version exists in KeyStore.");
                }
                else {
                    actualKey = KeyStore.GetCurrentKey(actualKeyId);
                    OperationHelpers.ThrowIfNull(
                        actualKey, $"No decryption key found in KeyStore for key ID '{actualKeyId}'. Ensure the key ID is correct and a key is configured.");
                }
            }
            else
                OperationHelpers.ThrowIf(true, "No decryption key available. Provide either a keyId or a key parameter.");
        }

        var plaintext = new byte[ciphertext.Length];
        try {
            XChaCha20Poly1305Helper.Decrypt(ciphertext, tag, actualKey!, nonce, plaintext, associatedData);
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
