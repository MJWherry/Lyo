using System.Security.Cryptography;
using System.Text;
using Lyo.Common.Extensions;
using Lyo.Encryption.Exceptions;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Encryption.Rsa;

/// <summary>
/// Decrypts data using an RSA private key. Handles both single-block and chunked ciphertext produced by <see cref="RsaEncryptor" />. Thread-safe: each method call uses its
/// own cryptographic context, so there are no shared mutable state concerns.
/// </summary>
public sealed class RsaDecryptor : IDecryptor, IDisposable, IAsyncDisposable
{
    private const long MaxInputSize = long.MaxValue;

    private readonly RSAEncryptionPadding _padding;

    private readonly RSA _rsa;

    private Encoding _decryptionEncoding = Encoding.UTF8;

    private bool _disposed;

    /// <summary> Initializes a new instance of the RsaDecryptor. </summary>
    /// <param name="privatePemPath">Path to the RSA private key PEM file (PKCS#8)</param>
    /// <param name="pfxPath">Path to the PFX certificate file (alternative to PEM)</param>
    /// <param name="password">Password for the PFX certificate</param>
    /// <param name="padding">RSA encryption padding. Defaults to OAEP-SHA256.</param>
    /// <exception cref="InvalidOperationException">Thrown when no key configuration is provided.</exception>
    public RsaDecryptor(string? privatePemPath = null, string? pfxPath = null, string? password = null, RSAEncryptionPadding? padding = null)
    {
        _padding = padding ?? RSAEncryptionPadding.OaepSHA256;

        // Validate padding mode - PKCS1 is not recommended for security
        ArgumentHelpers.ThrowIf(
            _padding.Mode == RSAEncryptionPaddingMode.Pkcs1, "PKCS1 padding is not recommended for security. Use OAEP padding (e.g., OAEP-SHA256) instead.", nameof(padding));

        _rsa = RSA.Create();
        if (!privatePemPath.IsNullOrEmpty())
            _rsa = RsaKeyLoader.LoadPrivateFromPem(privatePemPath);
        else if (!pfxPath.IsNullOrEmpty() && !password.IsNullOrEmpty())
            _rsa = RsaKeyLoader.LoadFromPfx(pfxPath, password);
        else
            OperationHelpers.ThrowIf(true, "No RSA key configuration provided. Specify either privatePemPath or (pfxPath, password).");

        // Validate RSA key size - minimum 2048 bits recommended (3072+ preferred for new deployments)
        ArgumentHelpers.ThrowIf(
            _rsa.KeySize < 2048,
            $"RSA key size must be at least 2048 bits for security. Current key size: {_rsa.KeySize} bits. Consider using 3072 or 4096 bits for new deployments.");
    }

    /// <summary> Asynchronously disposes of the RSA instance and releases all resources. </summary>
    public ValueTask DisposeAsync()
    {
        Dispose(true);
        return default;
    }

    /// <inheritdoc />
    public Encoding GetDecryptionEncoding() => _decryptionEncoding;

    /// <inheritdoc />
    public void SetDecryptionEncoding(Encoding encoding) => _decryptionEncoding = encoding ?? throw new ArgumentNullException(nameof(encoding));

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
    public byte[] Decrypt(byte[] encryptedBytes, string? keyId = null, byte[]? key = null)
    {
        // RSA uses keys from constructor, not parameters (for interface compliance)
        ArgumentHelpers.ThrowIf(keyId != null, "RSA decryption service uses keys from constructor. The 'keyId' parameter is not supported.", nameof(keyId));
        ArgumentHelpers.ThrowIf(key != null, "RSA decryption service uses keys from constructor. The 'key' parameter is not supported.", nameof(key));

        // Minimum size: at least one RSA encrypted block
        // Check if this is chunked data (starts with length prefix) or single encrypted block
        // Single RSA encrypted block size is deterministic based on key size
        var expectedEncryptedChunkSize = _rsa.KeySize / 8;
        ArgumentHelpers.ThrowIfNotInRange(encryptedBytes, expectedEncryptedChunkSize, MaxInputSize);
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

    /// <inheritdoc />
    public byte[] Decrypt(byte[] buffer, int offset, int count, string? keyId = null, byte[]? key = null)
    {
        var chunk = new byte[count];
        Array.Copy(buffer, offset, chunk, 0, count);
        return Decrypt(chunk, keyId, key);
    }

    /// <inheritdoc />
    public string DecryptString(byte[] encryptedBytes, string? keyId = null, byte[]? key = null, Encoding? encoding = null)
        => (encoding ?? GetDecryptionEncoding()).GetString(Decrypt(encryptedBytes, keyId, key));

    /// <inheritdoc />
    public Task DecryptToStreamAsync(Stream input, Stream output, string? keyId = null, byte[]? key = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNull(output);
        OperationHelpers.ThrowIfNotReadable(input, $"Stream '{nameof(input)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(output, $"Stream '{nameof(output)}' must be writable.");
        return RsaStreamCodec.DecryptAsync(input, output, (byte)EncryptionAlgorithm.Rsa, (byte)StreamFormatVersion.V1, DecryptChunk(keyId, key), ct);
    }

    /// <inheritdoc />
    public async Task<byte[]> DecryptFromFileAsync(string inputPath, string? keyId = null, byte[]? key = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(inputPath);
        using var inputStream = File.OpenRead(inputPath);
        using var outputStream = new MemoryStream();
        await DecryptToStreamAsync(inputStream, outputStream, keyId, key, ct).ConfigureAwait(false);
        return outputStream.ToArray();
    }

    /// <summary> Disposes of the RSA instance and releases all resources. </summary>
    public void Dispose() => Dispose(true);

    private Func<byte[], int, int, byte[]> DecryptChunk(string? keyId, byte[]? key) => (buffer, offset, count) => Decrypt(buffer, offset, count, keyId, key);

    private void Dispose(bool disposing)
    {
        if (_disposed || !disposing)
            return;

        _rsa.Dispose();
        _disposed = true;
    }
}