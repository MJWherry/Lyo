using System.Security.Cryptography;
using System.Text;
using Lyo.Common.Records;
using Lyo.Encryption.Exceptions;
using Lyo.Encryption.Security;
using Lyo.Encryption.Streaming;
using Lyo.Exceptions;
using Lyo.Keystore;

namespace Lyo.Encryption.AesCcm;

/// <summary>AES-CCM authenticated encryption (12-byte nonce, 128-bit tag). On all target frameworks the implementation uses BouncyCastle for identical wire-format behavior.</summary>
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
        ArgumentHelpers.ThrowIfNotInRange(plaintext.Length, Options.MinInputSize, Options.MaxInputSize, nameof(plaintext));
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

        // A fresh random nonce per call keeps Encrypt stateless and thread-safe (no shared counter).
        var nonce = CryptographicRandom.GetBytes(AesCcmHelper.NonceSize);
        try {
            var (ciphertext, tag) = AesCcmHelper.Encrypt(plaintext, actualKey!, nonce, associatedData);
            return BuildEncryptedFormat(ciphertext, tag, nonce, keyId, keyVersion, Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1);
        }
        finally {
            SecurityUtilities.Clear(nonce);
        }
    }

    private static byte[] BuildEncryptedFormat(byte[] ciphertext, byte[] tag, byte[] nonce, string? keyId, string? keyVersion, byte formatVersion)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(formatVersion);
        var keyIdBytes = keyId != null && !string.IsNullOrWhiteSpace(keyVersion) ? Encoding.UTF8.GetBytes(keyId) : [];
        bw.Write(keyIdBytes.Length);
        if (keyIdBytes.Length > 0)
            bw.Write(keyIdBytes);

        bw.Write(keyVersion ?? "");
        bw.Write(nonce.Length);
        bw.Write(nonce);
        bw.Write(tag);
        bw.Write(ciphertext);
        return ms.ToArray();
    }

    public override byte[] Encrypt(byte[] bytes, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIfNotInRange(bytes, Options.MinInputSize, Options.MaxInputSize);
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

        // A fresh random nonce per call keeps Encrypt stateless and thread-safe (no shared counter).
        var nonce = CryptographicRandom.GetBytes(AesCcmHelper.NonceSize);
        try {
            var (ciphertext, tag) = AesCcmHelper.Encrypt(bytes, actualKey!, nonce, associatedData);
            return BuildEncryptedFormat(ciphertext, tag, nonce, keyId, keyVersion, Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1);
        }
        finally {
            SecurityUtilities.Clear(nonce);
        }
    }

    public override byte[] Decrypt(byte[] encryptedBytes, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        const int minEncryptedSize = 38;
        ArgumentHelpers.ThrowIfNotInRange(encryptedBytes, minEncryptedSize, Options.MaxInputSize);
        using var ms = new MemoryStream(encryptedBytes);
        return DecryptFromStream(ms, keyId, key, associatedData);
    }

    /// <inheritdoc cref="IEncryptionService.Decrypt(byte[], int, int, string?, byte[], byte[])" />
    public override byte[] Decrypt(byte[] buffer, int offset, int count, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
        => DecryptChunk(buffer, offset, count, keyId, key, associatedData);

    protected override byte[] DecryptChunk(byte[] buffer, int offset, int count, string? keyId, byte[]? key, byte[]? associatedData = null)
    {
        const int minEncryptedSize = 38;
        ArgumentHelpers.ThrowIfNotInRange(count, minEncryptedSize, Options.MaxInputSize);
        using var ms = new MemoryStream(buffer, offset, count, false);
        return DecryptFromStream(ms, keyId, key, associatedData);
    }

    private byte[] DecryptFromStream(MemoryStream ms, string? keyId, byte[]? key, byte[]? associatedData)
    {
        using var br = new BinaryReader(ms);
        var firstByte = br.ReadByte();
        var expectedFormatVersion = Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1;
        if (firstByte != expectedFormatVersion)
            throw new InvalidDataException($"Invalid encrypted data format: expected format version {expectedFormatVersion}, got {firstByte}.");

        var keyIdLength = br.ReadInt32();
        if (keyIdLength < 0 || keyIdLength > 1024)
            throw new InvalidDataException($"Invalid key ID length: {keyIdLength}. Maximum allowed: 1024 bytes.");

        string? headerKeyId = null;
        if (keyIdLength > 0) {
            if (ms.Position + keyIdLength > ms.Length)
                throw new InvalidDataException("Invalid encrypted data format: keyId length exceeds remaining data.");

            var keyIdBytes = br.ReadBytes(keyIdLength);
            headerKeyId = Encoding.UTF8.GetString(keyIdBytes);
        }

        if (ms.Position >= ms.Length)
            throw new InvalidDataException("Invalid encrypted data format: insufficient data for keyVersion.");

        var headerKeyVersion = br.ReadString();
        if (string.IsNullOrWhiteSpace(headerKeyVersion))
            headerKeyVersion = null;

        var nonceLength = br.ReadInt32();
        ArgumentHelpers.ThrowIfNotInRange(
            nonceLength, AesCcmHelper.NonceSize, AesCcmHelper.NonceSize, nameof(ms), $"Invalid nonce length: {nonceLength}. Expected {AesCcmHelper.NonceSize} bytes.");

        var nonce = br.ReadBytes(nonceLength);
        var tag = br.ReadBytes(AesCcmHelper.TagSize);
        var ciphertext = br.ReadBytes((int)(ms.Length - ms.Position));
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

        try {
            return AesCcmHelper.Decrypt(ciphertext, tag, actualKey!, nonce, associatedData);
        }
#if NET10_0_OR_GREATER
        catch (AuthenticationTagMismatchException ex) {
            throw new DecryptionFailedException("Decryption failed due to authentication tag mismatch. Possible causes: wrong key, corrupted data, or tampered data.", ex);
        }
#endif
        catch (CryptographicException ex) {
            throw new DecryptionFailedException("Decryption failed. Possible causes: wrong key, corrupted data, or authentication failure.", ex);
        }
        finally {
            SecurityUtilities.Clear(nonce);
        }
    }
}