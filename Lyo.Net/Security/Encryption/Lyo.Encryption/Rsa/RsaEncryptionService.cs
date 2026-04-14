using System.Security.Cryptography;
using Lyo.Common.Records;
using Lyo.Encryption.Exceptions;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Encryption.Rsa;

/// <summary>
/// Provides secure encryption and decryption of data using RSA asymmetric encryption. RSA can only encrypt small amounts of data (typically up to key size minus padding
/// overhead), so data is automatically chunked for encryption and decryption. Suitable for encrypting small data directly or for key exchange scenarios. Thread-safe: Multiple threads
/// can safely call methods concurrently on the same instance. Each method call uses its own cryptographic context, so there are no shared mutable state concerns.
/// </summary>
public sealed class RsaEncryptionService : EncryptionServiceBase, IDisposable, IAsyncDisposable
{
    private readonly int _maxChunkSize;

    private readonly RSAEncryptionPadding _padding;

    private readonly RSA _rsa;

    private bool _disposed;

    /// <summary> Initializes a new instance of the RsaEncryptionService. </summary>
    /// <param name="publicPemPath">Path to the RSA public key PEM file</param>
    /// <param name="privatePemPath">Path to the RSA private key PEM file</param>
    /// <param name="pfxPath">Path to the PFX certificate file (alternative to PEM)</param>
    /// <param name="password">Password for the PFX certificate</param>
    /// <param name="padding">RSA encryption padding. Defaults to OAEP-SHA256.</param>
    /// <param name="maxChunkSize">Maximum chunk size for encryption. If null, automatically calculated based on key size and padding.</param>
    /// <exception cref="InvalidOperationException">Thrown when no key configuration is provided.</exception>
    /// <remarks>Creates default options: CurrentFormatVersion=null, MaxInputSize=long.MaxValue, MinInputSize=1, FileExtension=".rsa"</remarks>
    public RsaEncryptionService(
        string? publicPemPath = null,
        string? privatePemPath = null,
        string? pfxPath = null,
        string? password = null,
        RSAEncryptionPadding? padding = null,
        int? maxChunkSize = null)
        : base(
            new() {
                CurrentFormatVersion = null, // RSA doesn't use format versioning
                MaxInputSize = long.MaxValue,
                MinInputSize = 1,
                FileExtension = FileTypeInfo.LyoRsa.DefaultExtension
            })
    {
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

        // Calculate max chunk size based on key size and padding
        _maxChunkSize = maxChunkSize ?? CalculateMaxChunkSize(_rsa.KeySize, _padding);
    }

    /// <summary> Asynchronously disposes of the RSA instance and releases all resources. </summary>
    public ValueTask DisposeAsync()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
        return default;
    }

    /// <summary> Disposes of the RSA instance and releases all resources. </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Gets the algorithm identifier for stream format versioning.</summary>
    protected override byte GetAlgorithmId() => (byte)EncryptionAlgorithm.Rsa;

    /// <summary>Calculates the maximum chunk size that can be encrypted with RSA based on key size and padding.</summary>
    private static int CalculateMaxChunkSize(int keySizeBits, RSAEncryptionPadding padding)
    {
        var keySizeBytes = keySizeBits / 8;
        // Calculate padding overhead
        // OAEP padding overhead: 2 + hashSize + labelLength (typically 0) + padding
        // For OAEP-SHA256: ~66 bytes overhead
        // For OAEP-SHA1: ~42 bytes overhead
        // For PKCS1: 11 bytes overhead
        var overhead = padding.Mode switch {
            RSAEncryptionPaddingMode.Oaep => padding.OaepHashAlgorithm.Name switch {
                "SHA1" => 42,
                "SHA256" => 66,
                "SHA384" => 98,
                "SHA512" => 130,
                var _ => 66 // Default to SHA256 estimate
            },
            RSAEncryptionPaddingMode.Pkcs1 => 11,
            var _ => 11 // Default fallback
        };

        return keySizeBytes - overhead;
    }

    /// <summary>
    /// Encrypts data using RSA encryption. Thread-safe: Multiple threads can safely call this method concurrently on the same instance. Each method call uses its own
    /// cryptographic context, so there are no shared mutable state concerns.
    /// </summary>
    /// <param name="bytes">The data to encrypt. Must not be null or empty.</param>
    /// <param name="keyId">This parameter is ignored. RSA uses keys from constructor. Provided for interface compliance only.</param>
    /// <param name="key">This parameter is ignored. RSA uses keys from constructor. Provided for interface compliance only.</param>
    /// <returns>Encrypted data</returns>
    /// <exception cref="ArgumentException">Thrown when keyId or key parameters are provided</exception>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when bytes is empty (length is less than MinInputSize) or exceeds maximum allowed size (MaxInputSize)</exception>
    public override byte[] Encrypt(byte[] bytes, string? keyId = null, byte[]? key = null)
    {
        ArgumentHelpers.ThrowIfNotInRange(bytes, Options.MinInputSize, Options.MaxInputSize, nameof(bytes));
        // RSA uses keys from constructor, not parameters (for interface compliance)
        ArgumentHelpers.ThrowIf(keyId != null, "RSA encryption service uses keys from constructor. The 'keyId' parameter is not supported.", nameof(keyId));
        ArgumentHelpers.ThrowIf(key != null, "RSA encryption service uses keys from constructor. The 'key' parameter is not supported.", nameof(key));

        // If data fits in one chunk, encrypt directly
        if (bytes.Length <= _maxChunkSize)
            return _rsa.Encrypt(bytes, _padding);

        // Otherwise, chunk the data
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        var offset = 0;
        while (offset < bytes.Length) {
            var chunkSize = Math.Min(_maxChunkSize, bytes.Length - offset);
            var chunk = new byte[chunkSize];
            Array.Copy(bytes, offset, chunk, 0, chunkSize);
            var encryptedChunk = _rsa.Encrypt(chunk, _padding);
            bw.Write(encryptedChunk.Length);
            bw.Write(encryptedChunk);
            offset += chunkSize;
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Decrypts data using RSA decryption. Thread-safe: Multiple threads can safely call this method concurrently on the same instance. Each method call uses its own
    /// cryptographic context, so there are no shared mutable state concerns.
    /// </summary>
    /// <param name="encryptedBytes">The encrypted data to decrypt</param>
    /// <param name="keyId">This parameter is ignored. RSA uses keys from constructor. Provided for interface compliance only.</param>
    /// <param name="key">This parameter is ignored. RSA uses keys from constructor. Provided for interface compliance only.</param>
    /// <returns>Decrypted data</returns>
    /// <exception cref="ArgumentException">Thrown when keyId or key parameters are provided</exception>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when encryptedBytes is empty (length is less than 1) or too small (below minimum required size based on RSA key size)</exception>
    /// <exception cref="InvalidDataException">Thrown when encrypted data format is invalid, invalid chunk length, or corrupted</exception>
    /// <exception cref="DecryptionFailedException">Thrown when decryption fails due to wrong key, corrupted data, invalid padding, or authentication failure</exception>
    public override byte[] Decrypt(byte[] encryptedBytes, string? keyId = null, byte[]? key = null)
    {
        // RSA uses keys from constructor, not parameters (for interface compliance)
        ArgumentHelpers.ThrowIf(keyId != null, "RSA decryption service uses keys from constructor. The 'keyId' parameter is not supported.", nameof(keyId));
        ArgumentHelpers.ThrowIf(key != null, "RSA decryption service uses keys from constructor. The 'key' parameter is not supported.", nameof(key));

        // Minimum size: at least one RSA encrypted block
        // Check if this is chunked data (starts with length prefix) or single encrypted block
        // Single RSA encrypted block size is deterministic based on key size
        var expectedEncryptedChunkSize = _rsa.KeySize / 8;
        ArgumentHelpers.ThrowIfNotInRange(encryptedBytes, expectedEncryptedChunkSize, Options.MaxInputSize, nameof(encryptedBytes));
        // If data is exactly one encrypted chunk size, check if it's chunked or single block
        if (encryptedBytes.Length == expectedEncryptedChunkSize) {
            // Try direct decryption first (single block)
            try {
                return _rsa.Decrypt(encryptedBytes, _padding);
            }
            catch (CryptographicException ex) {
                // Provide more specific error context
                var errorMsg = ex.Message.Contains("padding", StringComparison.OrdinalIgnoreCase)
                    ?
                    "Decryption failed: Invalid padding. Possible causes: wrong key, corrupted data, or incorrect padding mode."
                    : ex.Message.Contains("key", StringComparison.OrdinalIgnoreCase)
                        ? "Decryption failed: Key-related error. Possible causes: wrong private key or key size mismatch."
                        : "Decryption failed: Cryptographic error. Possible causes: wrong key, corrupted data, or invalid format.";

                throw new DecryptionFailedException(errorMsg, ex);
            }
        }

        // If data is smaller than expected chunk size, it's invalid
        if (encryptedBytes.Length < expectedEncryptedChunkSize)
            throw new InvalidDataException($"Encrypted data size ({encryptedBytes.Length}) is smaller than expected RSA block size ({expectedEncryptedChunkSize}).");

        // Data is larger than one chunk, so it must be chunked
        // Chunked data format: [length][encrypted_chunk][length][encrypted_chunk]...
        using var ms = new MemoryStream(encryptedBytes);
        using var br = new BinaryReader(ms);
        using var decryptedMs = new MemoryStream();
        while (ms.Position < ms.Length) {
            if (ms.Length - ms.Position < 4)
                throw new InvalidDataException("Invalid encrypted data format: incomplete length prefix.");

            var chunkLength = br.ReadInt32();
            if (chunkLength <= 0 || chunkLength > expectedEncryptedChunkSize)
                throw new InvalidDataException($"Invalid encrypted chunk length: {chunkLength}. Expected <= {expectedEncryptedChunkSize}.");

            if (ms.Length - ms.Position < chunkLength)
                throw new InvalidDataException("Invalid encrypted data format: incomplete chunk.");

            var encryptedChunk = br.ReadBytes(chunkLength);
            byte[] decryptedChunk;
            try {
                decryptedChunk = _rsa.Decrypt(encryptedChunk, _padding);
            }
            catch (CryptographicException ex) {
                var errorMsg = ex.Message.Contains("padding", StringComparison.OrdinalIgnoreCase)
                    ? $"Failed to decrypt RSA chunk at position {ms.Position - chunkLength}: Invalid padding. Possible causes: wrong key or corrupted data."
                    : $"Failed to decrypt RSA chunk at position {ms.Position - chunkLength}: Cryptographic error. Possible causes: wrong key, corrupted data, or invalid format.";

                throw new DecryptionFailedException(errorMsg, ex);
            }

            decryptedMs.Write(decryptedChunk, 0, decryptedChunk.Length);
        }

        return decryptedMs.ToArray();
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed && disposing) {
            _rsa?.Dispose();
            _disposed = true;
        }
    }
}