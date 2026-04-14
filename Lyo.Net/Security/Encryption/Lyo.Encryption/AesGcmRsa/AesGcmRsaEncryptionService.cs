using System.Security.Cryptography;
using Lyo.Common.Records;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.Exceptions;
using Lyo.Encryption.Rsa;
using Lyo.Encryption.Security;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Encryption.AesGcmRsa;

/// <summary>
/// Provides secure encryption and decryption of data using a hybrid cryptographic approach that combines AES-GCM for high-performance symmetric encryption with RSA for
/// secure key exchange. Generates a random AES key for each encryption operation and encrypts it with RSA, ensuring maximum security. The class ensures data confidentiality,
/// integrity, and secure key handling, making it well-suited for encrypting files. Thread-safe: Multiple threads can safely call methods concurrently on the same instance. Each
/// method call uses its own cryptographic context (nonce, key material), so there are no shared mutable state concerns. However, if using RSA keys that aren't thread-safe, ensure
/// proper synchronization at the RSA key level.
/// </summary>
public sealed class AesGcmRsaEncryptionService : EncryptionServiceBase, IDisposable
{
    private readonly RSAEncryptionPadding _padding;

    private readonly RSA _rsa;

    private readonly int _aesKeyLengthBytes;

    private bool _disposed;

    /// <summary>Initializes a new instance of the AesGcmRsaEncryptionService.</summary>
    /// <param name="publicPemPath">Path to the RSA public key PEM file</param>
    /// <param name="privatePemPath">Path to the RSA private key PEM file</param>
    /// <param name="pfxPath">Path to the PFX certificate file (alternative to PEM)</param>
    /// <param name="password">Password for the PFX certificate</param>
    /// <param name="padding">RSA encryption padding. Defaults to OAEP-SHA256.</param>
    /// <param name="aesGcmKeySize">AES-GCM key size for the data encryption key (default 256-bit).</param>
    /// <exception cref="InvalidOperationException">Thrown when no key configuration is provided.</exception>
    /// <remarks>Creates default options: CurrentFormatVersion=null, MaxInputSize=long.MaxValue, MinInputSize=1, FileExtension=".agr"</remarks>
    public AesGcmRsaEncryptionService(
        string? publicPemPath = null,
        string? privatePemPath = null,
        string? pfxPath = null,
        string? password = null,
        RSAEncryptionPadding? padding = null,
        AesGcmKeySizeBits aesGcmKeySize = AesGcmKeySizeBits.Bits256)
        : base(
            new() {
                CurrentFormatVersion = null, // AES-GCM-RSA doesn't use format versioning
                MaxInputSize = long.MaxValue,
                MinInputSize = 1,
                FileExtension = FileTypeInfo.LyoAesGcmRsa.DefaultExtension,
                AesGcmKeySize = aesGcmKeySize
            })
    {
        _aesKeyLengthBytes = aesGcmKeySize.GetKeyLengthBytes();
        _padding = padding ?? RSAEncryptionPadding.OaepSHA256;

        // Validate padding mode - PKCS1 is not recommended for security
        ArgumentHelpers.ThrowIf(
            _padding.Mode == RSAEncryptionPaddingMode.Pkcs1, "PKCS1 padding is not recommended for security. Use OAEP padding (e.g., OAEP-SHA256) instead.", nameof(padding));

        _rsa = RSA.Create();
        if (!string.IsNullOrEmpty(publicPemPath) && !string.IsNullOrEmpty(privatePemPath))
            _rsa = RsaKeyLoader.LoadFromPemFiles(publicPemPath, privatePemPath);
        else if (!string.IsNullOrEmpty(pfxPath) && !string.IsNullOrEmpty(password))
            _rsa = RsaKeyLoader.LoadFromPfx(pfxPath, password);
        else
            throw new InvalidOperationException("No RSA key configuration provided. Specify either (publicPemPath, privatePemPath) or (pfxPath, password).");

        // Validate RSA key size - minimum 2048 bits recommended (3072+ preferred for new deployments)
        ArgumentHelpers.ThrowIf(
            _rsa.KeySize < 2048,
            $"RSA key size must be at least 2048 bits for security. Current key size: {_rsa.KeySize} bits. Consider using 3072 or 4096 bits for new deployments.");
    }

    /// <summary> Disposes of the RSA instance and releases all resources. </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Gets the algorithm identifier for stream format versioning.</summary>
    protected override byte GetAlgorithmId() => (byte)EncryptionAlgorithm.AesGcmRsa;

    /// <summary>
    /// Encrypts data using AES-GCM with a randomly generated key (or provided key) that is encrypted with RSA. Performance: Encrypts approximately 100-500 MB/s on typical
    /// hardware depending on data size. For large files, consider using EncryptToStreamAsync for better memory efficiency.
    /// </summary>
    /// <param name="plaintext">The data to encrypt. Must not be null or empty.</param>
    /// <param name="keyId">This parameter is ignored. AES-GCM-RSA uses RSA keys from constructor. Provided for interface compliance only.</param>
    /// <param name="key">Optional AES key. If null, a random key is generated and encrypted with RSA.</param>
    /// <returns>Encrypted data</returns>
    /// <exception cref="ArgumentException">Thrown when keyId parameter is provided</exception>
    /// <exception cref="ArgumentOutsideRangeException">
    /// Thrown when plaintext is empty (length is less than MinInputSize) or exceeds maximum allowed size (MaxInputSize), or key size is
    /// not 32 bytes
    /// </exception>
    public override byte[] Encrypt(byte[] plaintext, string? keyId = null, byte[]? key = null)
    {
        ArgumentHelpers.ThrowIfNotInRange(plaintext, Options.MinInputSize, Options.MaxInputSize, nameof(plaintext));
        // AES-GCM-RSA uses RSA keys from constructor, not keyId
        ArgumentHelpers.ThrowIf(keyId != null, "AES-GCM-RSA encryption service uses RSA keys from constructor. The 'keyId' parameter is not supported.", nameof(keyId));

        // If key is provided via parameter, validate and use it (for backward compatibility)
        // Otherwise, always generate random key (default behavior)
        if (key != null)
            AesGcmHelper.ValidateKeyLength(key, _aesKeyLengthBytes);

        var aesKey = key ?? CryptographicRandom.GetBytes(_aesKeyLengthBytes);
        var hasExternalKey = key != null;
        var nonce = CryptographicRandom.GetBytes(AesGcmHelper.NonceSize);
        try {
            var (ciphertext, tag) = AesGcmHelper.Encrypt(plaintext, aesKey, nonce);
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            if (!hasExternalKey) {
                // Embed the key encrypted with RSA (default behavior)
                bw.Write((byte)1); // hasEmbeddedKey flag
                var keyNonce = aesKey.Concat(nonce).ToArray();
                var encryptedKeyNonce = _rsa.Encrypt(keyNonce, _padding);
                bw.Write(encryptedKeyNonce.Length);
                bw.Write(encryptedKeyNonce);
                // Clear the keyNonce array after encryption
                SecurityUtilities.Clear(keyNonce);
            }
            else {
                // Only store the nonce (key provided externally)
                bw.Write((byte)0);
                bw.Write(nonce.Length);
                bw.Write(nonce);
            }

            bw.Write(tag);
            bw.Write(ciphertext);
            return ms.ToArray();
        }
        finally {
            // Securely clear the randomly generated AES key and nonce from memory
            if (!hasExternalKey)
                SecurityUtilities.Clear(aesKey);

            SecurityUtilities.Clear(nonce);
        }
    }

    /// <summary>Decrypts data encrypted with AES-GCM-RSA.</summary>
    /// <param name="encryptedData">The encrypted data to decrypt</param>
    /// <param name="keyId">This parameter is ignored. AES-GCM-RSA uses RSA keys from constructor. Provided for interface compliance only.</param>
    /// <param name="key">Optional AES key. Only used if data was encrypted with external key (not embedded).</param>
    /// <returns>Decrypted data</returns>
    /// <exception cref="ArgumentException">Thrown when keyId parameter is provided</exception>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when encryptedData is empty (length is less than 1) or too small (below minimum required size)</exception>
    /// <exception cref="InvalidDataException">Thrown when encrypted data format is invalid, invalid encrypted key+nonce length, invalid nonce length, or corrupted</exception>
    /// <exception cref="InvalidOperationException">Thrown when no decryption key is provided and data was encrypted with external key</exception>
    /// <exception cref="DecryptionFailedException">
    /// Thrown when decryption fails due to wrong RSA key (for embedded key), wrong AES key (for external key), corrupted data, authentication
    /// failure, or tampered data
    /// </exception>
    public override byte[] Decrypt(byte[] encryptedData, string? keyId = null, byte[]? key = null)
    {
        // AES-GCM-RSA uses RSA keys from constructor, not keyId
        ArgumentHelpers.ThrowIf(keyId != null, "AES-GCM-RSA decryption service uses RSA keys from constructor. The 'keyId' parameter is not supported.", nameof(keyId));
        using var ms = new MemoryStream(encryptedData);
        using var br = new BinaryReader(ms);
        // Minimum size: hasEmbeddedKey flag (1) + at least some data
        const int minEncryptedSize = 1 + 4 + AesGcmHelper.NonceSize + AesGcmHelper.TagSize;
        ArgumentHelpers.ThrowIfNotInRange(encryptedData, minEncryptedSize, Options.MaxInputSize, nameof(encryptedData));
        var hasEmbeddedKey = br.ReadByte() == 1;
        byte[]? aesKey = null;
        byte[]? nonce = null;
        byte[]? keyNonce = null;
        try {
            if (hasEmbeddedKey) {
                // Decrypt the embedded key using RSA
                var encryptedKeyNonceLength = br.ReadInt32();

                // Validate encrypted key+nonce length to prevent DoS attacks
                // RSA encrypted data size is exactly the RSA key size in bytes
                var expectedRsaEncryptedSize = _rsa.KeySize / 8;
                if (encryptedKeyNonceLength <= 0)
                    throw new InvalidDataException($"Invalid encrypted key+nonce length: {encryptedKeyNonceLength}. Length must be positive.");

                if (encryptedKeyNonceLength > expectedRsaEncryptedSize) {
                    throw new InvalidDataException(
                        $"Invalid encrypted key+nonce length: {encryptedKeyNonceLength} bytes. Maximum allowed: {expectedRsaEncryptedSize} bytes (RSA key size: {_rsa.KeySize} bits).");
                }

                // Check if stream has enough remaining data for this encrypted block
                var remainingBytes = ms.Length - ms.Position;
                if (remainingBytes < encryptedKeyNonceLength) {
                    throw new InvalidDataException(
                        $"Invalid encrypted data format: encrypted key+nonce length ({encryptedKeyNonceLength} bytes) exceeds remaining stream size ({remainingBytes} bytes).");
                }

                var encryptedKeyNonce = br.ReadBytes(encryptedKeyNonceLength);
                try {
                    keyNonce = _rsa.Decrypt(encryptedKeyNonce, _padding);
                }
                catch (CryptographicException ex) {
                    var errorMsg = ex.Message.Contains("padding", StringComparison.OrdinalIgnoreCase)
                        ? "Failed to decrypt embedded AES key: Invalid RSA padding. Possible causes: wrong RSA private key or corrupted encrypted key."
                        : "Failed to decrypt embedded AES key: Cryptographic error. Possible causes: wrong RSA private key or corrupted data.";

                    throw new DecryptionFailedException(errorMsg, ex);
                }

                OperationHelpers.ThrowIfNull(keyNonce, "Failed to decrypt embedded key: keyNonce is null.");
                // Split keyNonce into key and nonce
                // keyNonce = [key bytes][nonce bytes]
                // Nonce is always 12 bytes, so key is the rest
                var keySize = keyNonce.Length - AesGcmHelper.NonceSize;
                if (keySize != _aesKeyLengthBytes)
                    throw new DecryptionFailedException(
                        $"Invalid decrypted AES key size: {keySize} bytes. Expected {_aesKeyLengthBytes} bytes for this service configuration.");

                aesKey = new byte[keySize];
                Buffer.BlockCopy(keyNonce, 0, aesKey, 0, keySize);
                nonce = new byte[AesGcmHelper.NonceSize];
                Buffer.BlockCopy(keyNonce, keySize, nonce, 0, AesGcmHelper.NonceSize);
            }
            else {
                // Read nonce and use external key (if provided)
                var nonceLength = br.ReadInt32();

                // Validate nonce length to prevent DoS attacks
                ArgumentHelpers.ThrowIfNotInRange(
                    nonceLength, AesGcmHelper.NonceSize, AesGcmHelper.NonceSize, nameof(encryptedData),
                    $"Invalid nonce length: {nonceLength}. Expected {AesGcmHelper.NonceSize} bytes.");

                // Check if stream has enough remaining data for the nonce
                var remainingBytes = ms.Length - ms.Position;
                if (remainingBytes < nonceLength)
                    throw new InvalidDataException($"Invalid encrypted data format: nonce length ({nonceLength} bytes) exceeds remaining stream size ({remainingBytes} bytes).");

                nonce = br.ReadBytes(nonceLength);
                OperationHelpers.ThrowIfNull(key, "No decryption key provided. Data was encrypted with external key.");
                AesGcmHelper.ValidateKeyLength(key, _aesKeyLengthBytes);
                aesKey = key;
            }

            var tag = br.ReadBytes(AesGcmHelper.TagSize);
            var ciphertext = br.ReadBytes((int)(ms.Length - ms.Position));
            try {
                return AesGcmHelper.Decrypt(ciphertext, tag, aesKey, nonce);
            }
#if NET10_0_OR_GREATER
            catch (AuthenticationTagMismatchException ex) {
                throw new DecryptionFailedException("Decryption failed due to authentication tag mismatch. Possible causes: wrong AES key, corrupted data, or tampered data.", ex);
            }
#endif
            catch (CryptographicException ex) {
                var errorMsg = ex.Message.Contains("padding", StringComparison.OrdinalIgnoreCase)
                    ? "Decryption failed: RSA padding error when decrypting AES key. Possible causes: wrong RSA private key or corrupted encrypted key."
                    : "Decryption failed: Cryptographic error. Possible causes: wrong key, corrupted data, or authentication failure.";

                throw new DecryptionFailedException(errorMsg, ex);
            }
        }
        finally {
            // Securely clear sensitive data from memory after decryption
            if (keyNonce != null)
                SecurityUtilities.Clear(keyNonce);

            // Only clear aesKey if it was decrypted (not provided externally)
            if (hasEmbeddedKey && aesKey != null)
                SecurityUtilities.Clear(aesKey);

            if (nonce != null)
                SecurityUtilities.Clear(nonce);
        }
    }

    /// <summary> Asynchronously disposes of the RSA instance and releases all resources. </summary>
    public ValueTask DisposeAsync()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
        return default;
    }

    private void Dispose(bool disposing)
    {
        if (_disposed || !disposing)
            return;

        _rsa?.Dispose();
        _disposed = true;
    }
}