using System.Security.Cryptography;
using System.Text;
using Lyo.Common.Records;
using Lyo.Encryption.Exceptions;
using Lyo.Encryption.Security;
using Lyo.Exceptions;
using Lyo.Keystore;

namespace Lyo.Encryption.AesGcm;

/// <summary>
/// Provides secure encryption and decryption of data using AES-GCM symmetric encryption. Uses a KeyStore to manage encryption keys and supports generating a unique nonce per
/// encryption operation. Ensures confidentiality and integrity of data with authenticated encryption, making it suitable for scenarios where a shared secret key is available and
/// high-performance encryption is needed. Thread-safe: Multiple threads can safely call methods concurrently on the same instance. Each method call uses its own cryptographic context
/// (nonce, key material), so there are no shared mutable state concerns. However, if using a KeyStore that isn't thread-safe, ensure proper synchronization at the KeyStore level.
/// </summary>
public class AesGcmEncryptionService : EncryptionServiceBase, ISymmetricKeyMaterialSize
{
    /// <summary>Initializes a new instance with the specified AES-GCM key size.</summary>
    /// <param name="keyStore">The key store to use for retrieving encryption keys</param>
    /// <param name="aesGcmKeySize">AES key size (128, 192, or 256 bits).</param>
    public AesGcmEncryptionService(IKeyStore keyStore, AesGcmKeySizeBits aesGcmKeySize = AesGcmKeySizeBits.Bits256)
        : base(
            new() {
                CurrentFormatVersion = (byte)StreamFormatVersion.V1,
                MaxInputSize = long.MaxValue,
                MinInputSize = 1,
                FileExtension = FileTypeInfo.LyoAesGcm.DefaultExtension,
                AesGcmKeySize = aesGcmKeySize
            }, keyStore) { }

    /// <summary>Initializes a new instance with explicit options (must set <see cref="EncryptionServiceOptions.FileExtension"/> and <see cref="EncryptionServiceOptions.AesGcmKeySize"/> as needed).</summary>
    public AesGcmEncryptionService(EncryptionServiceOptions options, IKeyStore keyStore)
        : base(options, keyStore) { }

    /// <inheritdoc cref="ISymmetricKeyMaterialSize.RequiredKeyBytes" />
    public int RequiredKeyBytes => Options.AesGcmKeySize.GetKeyLengthBytes();

    /// <summary>Gets the algorithm identifier for stream format versioning.</summary>
    protected override byte GetAlgorithmId() => (byte)EncryptionAlgorithm.AesGcm;

    /// <inheritdoc cref="IEncryptionService.Encrypt(ReadOnlySpan{byte}, string?, byte[]?)" />
    public byte[] Encrypt(ReadOnlySpan<byte> plaintext, string? keyId = null, byte[]? key = null)
    {
        ArgumentHelpers.ThrowIfNotInRange(plaintext.Length, Options.MinInputSize, Options.MaxInputSize, nameof(plaintext));
        if (key != null)
            AesGcmHelper.ValidateKeyLength(key, RequiredKeyBytes);

        byte[]? actualKey;
        string? keyVersion = null;
        if (key != null)
            actualKey = key;
        else if (keyId != null && KeyStore != null) {
            actualKey = KeyStore.GetCurrentKey(keyId);
            OperationHelpers.ThrowIfNull(actualKey, $"No encryption key available for key ID '{keyId}'. Ensure a key is configured.");
            AesGcmHelper.ValidateKeyLength(actualKey, RequiredKeyBytes);
            keyVersion = KeyStore.GetCurrentVersion(keyId);
        }
        else
            throw new InvalidOperationException("No encryption key available. Provide either a keyId or a key parameter.");

        byte[] nonce;
        if (key != null || keyId == null || keyVersion == null)
            nonce = RandomNumberGenerator.GetBytes(AesGcmHelper.NonceSize);
        else
            nonce = NonceGenerator.GenerateNonce(KeyStore!, keyId, keyVersion);

        try {
            var (ciphertext, tag) = AesGcmHelper.Encrypt(plaintext, actualKey, nonce);
            return BuildEncryptedFormat(ciphertext, tag, nonce, keyId, keyVersion, Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1);
        }
        finally {
            SecurityUtilities.Clear(nonce);
        }
    }

    /// <inheritdoc />
    protected override byte[] EncryptChunk(byte[] buffer, int offset, int count, string? keyId, byte[]? key) => Encrypt(buffer.AsSpan(offset, count), keyId, key);

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

    /// <inheritdoc />
    /// <remarks>
    /// Performance: Encrypts approximately 200-800 MB/s on typical hardware depending on data size. For large files, consider using EncryptToStreamAsync for better memory
    /// efficiency.
    /// </remarks>
    public override byte[] Encrypt(byte[] bytes, string? keyId = null, byte[]? key = null)
    {
        ArgumentHelpers.ThrowIfNotInRange(bytes, Options.MinInputSize, Options.MaxInputSize, nameof(bytes));
        if (key != null)
            AesGcmHelper.ValidateKeyLength(key, RequiredKeyBytes);

        byte[]? actualKey;
        string? keyVersion = null;
        if (key != null)
            actualKey = key;
        else if (keyId != null && KeyStore != null) {
            actualKey = KeyStore.GetCurrentKey(keyId);
            OperationHelpers.ThrowIfNull(actualKey, $"No encryption key available for key ID '{keyId}'. Ensure a key is configured.");
            AesGcmHelper.ValidateKeyLength(actualKey, RequiredKeyBytes);
            keyVersion = KeyStore.GetCurrentVersion(keyId);
        }
        else
            throw new InvalidOperationException("No encryption key available. Provide either a keyId or a key parameter.");

        // Use hybrid nonce generator (random IV + counter) to prevent nonce reuse
        // If key was provided directly (not from KeyStore), fall back to random nonce
        byte[] nonce;
        if (key != null || keyId == null || keyVersion == null)
            nonce = RandomNumberGenerator.GetBytes(AesGcmHelper.NonceSize); // Fallback for direct key usage
        else
            nonce = NonceGenerator.GenerateNonce(KeyStore!, keyId, keyVersion);

        try {
            var (ciphertext, tag) = AesGcmHelper.Encrypt(bytes, actualKey, nonce);
            return BuildEncryptedFormat(ciphertext, tag, nonce, keyId, keyVersion, Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1);
        }
        finally {
            // Securely clear the nonce from memory after encryption
            SecurityUtilities.Clear(nonce);
        }
    }

    /// <inheritdoc />
    public override byte[] Decrypt(byte[] encryptedBytes, string? keyId = null, byte[]? key = null)
    {
        const int minEncryptedSize = 38;
        ArgumentHelpers.ThrowIfNotInRange(encryptedBytes, minEncryptedSize, Options.MaxInputSize, nameof(encryptedBytes));
        using var ms = new MemoryStream(encryptedBytes);
        return DecryptFromStream(ms, keyId, key);
    }

    /// <inheritdoc cref="IEncryptionService.Decrypt(byte[], int, int, string?, byte[]?)" />
    public byte[] Decrypt(byte[] buffer, int offset, int count, string? keyId = null, byte[]? key = null) => DecryptChunk(buffer, offset, count, keyId, key);

    /// <inheritdoc />
    protected override byte[] DecryptChunk(byte[] buffer, int offset, int count, string? keyId, byte[]? key)
    {
        const int minEncryptedSize = 38;
        ArgumentHelpers.ThrowIfNotInRange(count, minEncryptedSize, Options.MaxInputSize, nameof(count));
        using var ms = new MemoryStream(buffer, offset, count, false);
        return DecryptFromStream(ms, keyId, key);
    }

    private byte[] DecryptFromStream(MemoryStream ms, string? keyId, byte[]? key)
    {
        using var br = new BinaryReader(ms);
        var firstByte = br.ReadByte();
        var expectedFormatVersion = Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1;
        if (firstByte != expectedFormatVersion)
            throw new InvalidDataException($"Invalid encrypted data format: expected format version {expectedFormatVersion}, got {firstByte}.");

        var formatVersion = (StreamFormatVersion)firstByte;
        var maxSupportedVersion = Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1;
        if (formatVersion > (StreamFormatVersion)maxSupportedVersion)
            throw new InvalidDataException($"Unsupported format version: {formatVersion}. Maximum supported version: {(StreamFormatVersion)maxSupportedVersion}.");

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
            nonceLength, AesGcmHelper.NonceSize, AesGcmHelper.NonceSize, nameof(ms), $"Invalid nonce length: {nonceLength}. Expected {AesGcmHelper.NonceSize} bytes.");

        var nonce = br.ReadBytes(nonceLength);
        var tag = br.ReadBytes(AesGcmHelper.TagSize);
        var ciphertext = br.ReadBytes((int)(ms.Length - ms.Position));
        byte[]? actualKey;
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

                AesGcmHelper.ValidateKeyLength(actualKey, RequiredKeyBytes);
            }
            else
                throw new InvalidOperationException("No decryption key available. Provide either a keyId or a key parameter.");
        }

        if (key != null)
            AesGcmHelper.ValidateKeyLength(key, RequiredKeyBytes);

        try {
            return AesGcmHelper.Decrypt(ciphertext, tag, actualKey, nonce);
        }
        catch (AuthenticationTagMismatchException ex) {
            throw new DecryptionFailedException("Decryption failed due to authentication tag mismatch. Possible causes: wrong key, corrupted data, or tampered data.", ex);
        }
        catch (CryptographicException ex) {
            throw new DecryptionFailedException("Decryption failed. Possible causes: wrong key, corrupted data, or authentication failure.", ex);
        }
        finally {
            SecurityUtilities.Clear(nonce);
        }
    }
}