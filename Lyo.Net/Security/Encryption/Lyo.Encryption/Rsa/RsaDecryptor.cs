using System.Security.Cryptography;
using System.Text;
using Lyo.Common.Core.Extensions;
using Lyo.Encryption.Exceptions;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Encryption.Rsa;

/// <summary>
/// Decrypts with an RSA private key. Accepts single-block and chunked ciphertext from <see cref="RsaEncryptor" />. Thread-safe: each call uses its
/// own cryptographic context and shares no mutable crypto state.
/// </summary>
public sealed class RsaDecryptor : IDecryptor, IDisposable, IAsyncDisposable
{
    private const long MaxInputSize = long.MaxValue;

    private readonly RSAEncryptionPadding _padding;

    private readonly RSA _rsa;

    private Encoding _decryptionEncoding = Encoding.UTF8;

    private bool _disposed;

    /// <summary>Creates an RsaDecryptor.</summary>
    /// <param name="privatePemPath">Path to the RSA private-key PEM (PKCS#8)</param>
    /// <param name="pfxPath">Path to a PFX certificate (alternative to PEM)</param>
    /// <param name="password">PFX password</param>
    /// <param name="padding">RSA padding. Default is OAEP-SHA256.</param>
    /// <exception cref="ConfigurationException">Thrown when no key configuration is supplied.</exception>
    public RsaDecryptor(string? privatePemPath = null, string? pfxPath = null, string? password = null, RSAEncryptionPadding? padding = null)
    {
        _padding = padding ?? RSAEncryptionPadding.OaepSHA256;

        // reject PKCS1 padding — it is not recommended
        ArgumentHelpers.ThrowIf(
            _padding.Mode == RSAEncryptionPaddingMode.Pkcs1, "PKCS1 padding is not recommended for security. Use OAEP padding (e.g., OAEP-SHA256) instead.", nameof(padding));

        // keep exactly one RSA instance (avoid a throwaway RSA.Create() that would leak on reassignment).
        if (!privatePemPath.IsNullOrEmpty())
            _rsa = RsaKeyLoader.LoadPrivateFromPem(privatePemPath);
        else if (!pfxPath.IsNullOrEmpty() && !password.IsNullOrEmpty())
            _rsa = RsaKeyLoader.LoadFromPfx(pfxPath, password);
        else
            throw new ConfigurationException("No RSA key configuration provided. Specify either privatePemPath or (pfxPath, password).");

        // require at least 2048-bit RSA (3072+ preferred for new deployments)
        ArgumentHelpers.ThrowIf(
            _rsa.KeySize < 2048,
            $"RSA key size must be at least 2048 bits for security. Current key size: {_rsa.KeySize} bits. Consider using 3072 or 4096 bits for new deployments.");
    }

    /// <summary>Asynchronously disposes the RSA instance and releases resources.</summary>
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
    /// Decrypts with RSA. Thread-safe: concurrent calls on one instance are safe because each call owns its
    /// cryptographic context and shares no mutable crypto state.
    /// </summary>
    /// <param name="encryptedBytes">Ciphertext to decrypt</param>
    /// <param name="keyId">Ignored. RSA keys come from the constructor; present only for the interface.</param>
    /// <param name="key">Ignored. RSA keys come from the constructor; present only for the interface.</param>
    /// <param name="associatedData">Unsupported by RSA-OAEP; must be null or a <see cref="NotSupportedException" /> is thrown.</param>
    /// <returns>Plaintext</returns>
    /// <exception cref="ArgumentException">Thrown when keyId or key is supplied</exception>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when encryptedBytes is empty (length &lt; 1) or shorter than one RSA block</exception>
    /// <exception cref="InvalidDataException">Thrown when the ciphertext layout is invalid, a chunk length is illegal, or the data is corrupted</exception>
    /// <exception cref="NotSupportedException">Thrown when associatedData is supplied (RSA-OAEP has no AAD input)</exception>
    /// <exception cref="DecryptionFailedException">Thrown when decrypt fails (wrong key, damaged data, bad padding, or authentication failure)</exception>
    public byte[] Decrypt(byte[] encryptedBytes, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        // RSA keys come from the constructor, not these parameters (interface compliance)
        ArgumentHelpers.ThrowIf(keyId != null, "RSA decryption service uses keys from constructor. The 'keyId' parameter is not supported.", nameof(keyId));
        ArgumentHelpers.ThrowIf(key != null, "RSA decryption service uses keys from constructor. The 'key' parameter is not supported.", nameof(key));
        if (associatedData != null)
            throw new NotSupportedException("RSA decryption does not support associated data.");

        // smallest legal size is one RSA ciphertext block
        // decide whether this is length-prefixed chunks or a single block
        // a single RSA ciphertext block has a fixed size from the key
        var expectedEncryptedChunkSize = _rsa.KeySize / 8;
        ArgumentHelpers.ThrowIfNotInRange(encryptedBytes, expectedEncryptedChunkSize, MaxInputSize);
        // if length equals one block, it may still be chunked or a lone block
        if (encryptedBytes.Length == expectedEncryptedChunkSize) {
            // try a single-block decrypt first
            try {
                return _rsa.Decrypt(encryptedBytes, _padding);
            }
            catch (CryptographicException ex) {
                // more specific error context
                var errorMsg = ex.Message.Contains("padding", StringComparison.OrdinalIgnoreCase)
                    ?
                    "Decryption failed: Invalid padding. Possible causes: wrong key, corrupted data, or incorrect padding mode."
                    : ex.Message.Contains("key", StringComparison.OrdinalIgnoreCase)
                        ? "Decryption failed: Key-related error. Possible causes: wrong private key or key size mismatch."
                        : "Decryption failed: Cryptographic error. Possible causes: wrong key, corrupted data, or invalid format.";

                throw new DecryptionFailedException(errorMsg, ex);
            }
        }

        // shorter than one block is invalid
        if (encryptedBytes.Length < expectedEncryptedChunkSize)
            throw new InvalidDataException($"Encrypted data size ({encryptedBytes.Length}) is smaller than expected RSA block size ({expectedEncryptedChunkSize}).");

        // longer than one block must be chunked
        // chunked layout: [length][encrypted_chunk][length][encrypted_chunk]...
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
    public byte[] Decrypt(byte[] buffer, int offset, int count, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        var chunk = new byte[count];
        Array.Copy(buffer, offset, chunk, 0, count);
        return Decrypt(chunk, keyId, key, associatedData);
    }

    /// <inheritdoc />
    public string DecryptString(byte[] encryptedBytes, string? keyId = null, byte[]? key = null, Encoding? encoding = null)
        => (encoding ?? GetDecryptionEncoding()).GetString(Decrypt(encryptedBytes, keyId, key));

    /// <inheritdoc />
    public Task DecryptToStreamAsync(Stream input, Stream output, string? keyId = null, byte[]? key = null, byte[]? associatedData = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNull(output);
        OperationHelpers.ThrowIfNotReadable(input, $"Stream '{nameof(input)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(output, $"Stream '{nameof(output)}' must be writable.");
        if (associatedData != null)
            throw new NotSupportedException("RSA decryption does not support associated data.");

        return RsaStreamCodec.DecryptAsync(input, output, (byte)EncryptionAlgorithm.Rsa, (byte)StreamFormatVersion.V1, DecryptChunk(keyId, key), ct);
    }

    /// <inheritdoc />
    public async Task<byte[]> DecryptFromFileAsync(string inputPath, string? keyId = null, byte[]? key = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(inputPath);
        using var inputStream = File.OpenRead(inputPath);
        using var outputStream = new MemoryStream();
        await DecryptToStreamAsync(inputStream, outputStream, keyId, key, ct: ct).ConfigureAwait(false);
        return outputStream.ToArray();
    }

    /// <summary>Disposes the RSA instance and releases resources.</summary>
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