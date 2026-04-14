using System.Security.Cryptography;
using System.Text;
using Lyo.Common.Records;
using Lyo.Encryption.Exceptions;
using Lyo.Encryption.Security;
using Lyo.Exceptions;
using Lyo.Keystore;

namespace Lyo.Encryption.ChaCha20Poly1305;

/// <summary>
/// Provides secure encryption and decryption of data using ChaCha20Poly1305 symmetric encryption. ChaCha20Poly1305 is a modern authenticated encryption algorithm that
/// provides high performance and strong security guarantees. It uses a 12-byte nonce and generates a 16-byte authentication tag. Uses a KeyStore to manage encryption keys, enabling
/// key rotation and secure key management. Thread-safe: Multiple threads can safely call methods concurrently on the same instance. Each method call uses its own cryptographic
/// context (nonce, key material), so there are no shared mutable state concerns. However, if using a KeyStore that isn't thread-safe, ensure proper synchronization at the KeyStore
/// level.
/// </summary>
public class ChaCha20Poly1305EncryptionService : EncryptionServiceBase, ISymmetricKeyMaterialSize
{
    // Format header: [FormatVersion: 1 byte][KeyIdLength: 4 bytes][KeyId][KeyVersionLength: 4 bytes][KeyVersion][nonceLength: 4 bytes][nonce][tag][ciphertext]

    /// <summary> Initializes a new instance of the ChaCha20Poly1305EncryptionService. </summary>
    /// <param name="keyStore">The key store to use for retrieving encryption keys</param>
    /// <exception cref="ArgumentNullException">Thrown when keyStore is null</exception>
    /// <remarks>Creates default options: CurrentFormatVersion=V1, MaxInputSize=long.MaxValue, MinInputSize=1, FileExtension=".chacha"</remarks>
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

    /// <summary>Gets the algorithm identifier for stream format versioning.</summary>
    protected override byte GetAlgorithmId() => (byte)EncryptionAlgorithm.ChaCha20Poly1305;

    /// <inheritdoc cref="IEncryptionService.Encrypt(ReadOnlySpan{byte}, string?, byte[]?)" />
    public override byte[] Encrypt(ReadOnlySpan<byte> plaintext, string? keyId = null, byte[]? key = null)
    {
        ArgumentHelpers.ThrowIfNotInRange(plaintext.Length, Options.MinInputSize, Options.MaxInputSize, nameof(plaintext));
        if (key != null)
            ArgumentHelpers.ThrowIfNotInRange(key, 32, 32, nameof(key));

        byte[]? actualKey;
        string? keyVersion = null;
        if (key != null)
            actualKey = key;
        else if (keyId != null && KeyStore != null) {
            actualKey = KeyStore.GetCurrentKey(keyId);
            OperationHelpers.ThrowIfNull(actualKey, $"No encryption key found in KeyStore for key ID '{keyId}'. Ensure the key ID is correct and a key is configured.");
            keyVersion = KeyStore.GetCurrentVersion(keyId);
        }
        else
            throw new InvalidOperationException("No encryption key available. Provide either a keyId or a key parameter.");

        byte[] nonce;
        if (key != null || keyId == null || keyVersion == null)
            nonce = CryptographicRandom.GetBytes(ChaCha20Poly1305Helper.NonceSize);
        else
            nonce = NonceGenerator.GenerateNonce(KeyStore!, keyId, keyVersion);

        try {
            var (ciphertext, tag) = ChaCha20Poly1305Helper.Encrypt(plaintext, actualKey, nonce);
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1);
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
        finally {
            SecurityUtilities.Clear(nonce);
        }
    }

    /// <inheritdoc />
    protected override byte[] EncryptChunk(byte[] buffer, int offset, int count, string? keyId, byte[]? key) => Encrypt(buffer.AsSpan(offset, count), keyId, key);

    /// <inheritdoc />
    /// <remarks>
    /// Performance: Encrypts approximately 300-1000 MB/s on typical hardware depending on data size. For large files, consider using EncryptToStreamAsync for better memory
    /// efficiency.
    /// </remarks>
    public override byte[] Encrypt(byte[] bytes, string? keyId = null, byte[]? key = null)
    {
        ArgumentHelpers.ThrowIfNotInRange(bytes, Options.MinInputSize, Options.MaxInputSize, nameof(bytes));
        // Validate key size if provided
        if (key != null)
            ArgumentHelpers.ThrowIfNotInRange(key, 32, 32, nameof(key)); // ChaCha20-Poly1305 requires 32-byte key

        byte[]? actualKey;
        string? keyVersion = null;
        if (key != null)
            actualKey = key;
        else if (keyId != null && KeyStore != null) {
            actualKey = KeyStore.GetCurrentKey(keyId);
            OperationHelpers.ThrowIfNull(actualKey, $"No encryption key found in KeyStore for key ID '{keyId}'. Ensure the key ID is correct and a key is configured.");
            keyVersion = KeyStore.GetCurrentVersion(keyId);
        }
        else
            throw new InvalidOperationException("No encryption key available. Provide either a keyId or a key parameter.");

        // Use hybrid nonce generator (random IV + counter) to prevent nonce reuse
        // If key was provided directly (not from KeyStore), fall back to random nonce
        byte[] nonce;
        if (key != null || keyId == null || keyVersion == null)
            nonce = CryptographicRandom.GetBytes(ChaCha20Poly1305Helper.NonceSize); // Fallback for direct key usage
        else
            nonce = NonceGenerator.GenerateNonce(KeyStore!, keyId, keyVersion);

        try {
            var (ciphertext, tag) = ChaCha20Poly1305Helper.Encrypt(bytes, actualKey, nonce);
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            // Write format header with keyId and keyVersion
            bw.Write(Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1); // Format version

            // Write keyId (UTF-8 encoded)
            var keyIdBytes = keyId != null && !string.IsNullOrWhiteSpace(keyVersion) ? Encoding.UTF8.GetBytes(keyId) : [];
            bw.Write(keyIdBytes.Length);
            if (keyIdBytes.Length > 0)
                bw.Write(keyIdBytes);

            // Write keyVersion (0 if not using KeyStore)
            bw.Write(keyVersion ?? "");

            // Write existing format: nonce, tag, ciphertext
            bw.Write(nonce.Length);
            bw.Write(nonce);
            bw.Write(tag);
            bw.Write(ciphertext);
            return ms.ToArray();
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
    public override byte[] Decrypt(byte[] buffer, int offset, int count, string? keyId = null, byte[]? key = null) => DecryptChunk(buffer, offset, count, keyId, key);

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

        // Read keyId
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

        // Read keyVersion
        if (ms.Position >= ms.Length)
            throw new InvalidDataException("Invalid encrypted data format: insufficient data for keyVersion.");

        var headerKeyVersion = br.ReadString();
        if (string.IsNullOrWhiteSpace(headerKeyVersion))
            headerKeyVersion = null;

        // Read nonce, tag, ciphertext
        var nonceLength = br.ReadInt32();
        ArgumentHelpers.ThrowIfNotInRange(
            nonceLength, ChaCha20Poly1305Helper.NonceSize, ChaCha20Poly1305Helper.NonceSize, nameof(ms),
            $"Invalid nonce length: {nonceLength}. Expected {ChaCha20Poly1305Helper.NonceSize} bytes.");

        var nonce = br.ReadBytes(nonceLength);
        var tag = br.ReadBytes(ChaCha20Poly1305Helper.TagSize);
        var ciphertext = br.ReadBytes((int)(ms.Length - ms.Position));

        // Determine which key to use
        byte[]? actualKey;
        if (key != null)
            actualKey = key;
        else {
            // Use keyId from header if available, otherwise use provided keyId parameter
            var actualKeyId = headerKeyId ?? keyId;
            var actualKeyVersion = headerKeyVersion;
            if (actualKeyId != null && KeyStore != null) {
                if (!string.IsNullOrWhiteSpace(actualKeyVersion)) {
                    // Use specific version from header
                    actualKey = KeyStore.GetKey(actualKeyId, actualKeyVersion);
                    OperationHelpers.ThrowIfNull(
                        actualKey, $"No decryption key found in KeyStore for key ID '{actualKeyId}' version {actualKeyVersion}. Ensure the key version exists in KeyStore.");
                }
                else {
                    // Use current version
                    actualKey = KeyStore.GetCurrentKey(actualKeyId);
                    OperationHelpers.ThrowIfNull(
                        actualKey, $"No decryption key found in KeyStore for key ID '{actualKeyId}'. Ensure the key ID is correct and a key is configured.");
                }
            }
            else
                throw new InvalidOperationException("No decryption key available. Provide either a keyId or a key parameter.");
        }

        try {
            return ChaCha20Poly1305Helper.Decrypt(ciphertext, tag, actualKey, nonce);
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
            // Securely clear the nonce from memory after decryption
            SecurityUtilities.Clear(nonce);
        }
    }
}