using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Metadata.Records;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.Exceptions;
using Lyo.Encryption.Rsa;
using Lyo.Encryption.Security;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Encryption.AesGcmRsa;

/// <summary>
/// Hybrid encrypt/decrypt: AES-GCM for the payload, RSA for key exchange. Each encrypt mints a random AES key and wraps it with RSA.
/// Confidentiality, integrity, and key handling are covered; a good fit for file encryption. Thread-safe: concurrent calls on one instance are safe because each
/// call owns its cryptographic context (nonce, key material) and shares no mutable crypto state. If the RSA keys themselves are not thread-safe, synchronize
/// at the RSA key
/// level.
/// </summary>
public sealed class AesGcmRsaEncryptionService : IEncryptionService, IEncryptionAlgorithmProvider, IDisposable, IAsyncDisposable
{
    private const long MinInputSize = 1;
    private const long MaxInputSize = long.MaxValue;

    private readonly int _aesKeyLengthBytes;
    private readonly RSAEncryptionPadding _padding;

    private readonly RSA _rsa;
    private Encoding _decryptionEncoding = Encoding.UTF8;

    private bool _disposed;

    private Encoding _encryptionEncoding = Encoding.UTF8;

    /// <summary>Creates an AesGcmRsaEncryptionService.</summary>
    /// <param name="publicPemPath">Path to the RSA public-key PEM</param>
    /// <param name="privatePemPath">Path to the RSA private-key PEM</param>
    /// <param name="pfxPath">Path to a PFX certificate (alternative to PEM)</param>
    /// <param name="password">PFX password</param>
    /// <param name="padding">RSA padding. Default is OAEP-SHA256.</param>
    /// <param name="aesGcmKeySize">AES-GCM width for the data key (default 256-bit).</param>
    /// <exception cref="ConfigurationException">Thrown when no key configuration is supplied.</exception>
    /// <remarks>Default options: CurrentFormatVersion=null, MaxInputSize=long.MaxValue, MinInputSize=1, FileExtension=".agr"</remarks>
    public AesGcmRsaEncryptionService(
        string? publicPemPath = null,
        string? privatePemPath = null,
        string? pfxPath = null,
        string? password = null,
        RSAEncryptionPadding? padding = null,
        AesGcmKeySizeBits aesGcmKeySize = AesGcmKeySizeBits.Bits256)
    {
        _aesKeyLengthBytes = aesGcmKeySize.GetKeyLengthBytes();
        _padding = padding ?? RSAEncryptionPadding.OaepSHA256;

        // reject PKCS1 padding — it is not recommended
        ArgumentHelpers.ThrowIf(
            _padding.Mode == RSAEncryptionPaddingMode.Pkcs1, "PKCS1 padding is not recommended for security. Use OAEP padding (e.g., OAEP-SHA256) instead.", nameof(padding));

        _rsa = RSA.Create();
        if (!publicPemPath.IsNullOrEmpty() && !privatePemPath.IsNullOrEmpty())
            _rsa = RsaKeyLoader.LoadFromPemFiles(publicPemPath, privatePemPath);
        else if (!pfxPath.IsNullOrEmpty() && !password.IsNullOrEmpty())
            _rsa = RsaKeyLoader.LoadFromPfx(pfxPath, password);
        else
            ConfigurationHelpers.ThrowIf(true, "No RSA key configuration provided. Specify either (publicPemPath, privatePemPath) or (pfxPath, password).");

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

    /// <summary>Disposes the RSA instance and releases resources.</summary>
    public void Dispose() => Dispose(true);

    /// <inheritdoc />
    public EncryptionAlgorithm AlgorithmKind => EncryptionAlgorithm.AesGcmRsa;

    /// <inheritdoc />
    public string FileExtension => FileTypeInfo.LyoAesGcmRsa.DefaultExtension;

    /// <inheritdoc />
    public Encoding GetEncryptionEncoding() => _encryptionEncoding;

    /// <inheritdoc />
    public void SetEncryptionEncoding(Encoding encoding) => _encryptionEncoding = encoding ?? throw new ArgumentNullException(nameof(encoding));

    /// <inheritdoc />
    public Encoding GetDecryptionEncoding() => _decryptionEncoding;

    /// <inheritdoc />
    public void SetDecryptionEncoding(Encoding encoding) => _decryptionEncoding = encoding ?? throw new ArgumentNullException(nameof(encoding));

    /// <summary>
    /// Encrypts with AES-GCM using a random key (or a caller key) that is wrapped with RSA. Throughput is typically about 100-500 MB/s on typical
    /// hardware depending on payload size. For large files, prefer EncryptToStreamAsync to keep memory use down.
    /// </summary>
    /// <param name="plaintext">Plaintext. Must not be null or empty.</param>
    /// <param name="keyId">Ignored. AES-GCM-RSA uses RSA keys from the constructor; present only for the interface.</param>
    /// <param name="key">Optional AES key. Null means mint a random key and wrap it with RSA.</param>
    /// <param name="associatedData">Optional AAD authenticated (not encrypted) by the AES-GCM tag; the same bytes must be passed on decrypt.</param>
    /// <returns>Ciphertext</returns>
    /// <exception cref="ArgumentException">Thrown when keyId is supplied</exception>
    /// <exception cref="ArgumentOutsideRangeException">
    /// Thrown when plaintext is empty (below MinInputSize), exceeds MaxInputSize, or the key length is
    /// not 32 bytes
    /// </exception>
    public byte[] Encrypt(byte[] plaintext, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIfNotInRange(plaintext, MinInputSize, MaxInputSize);
        return EncryptCore(plaintext, keyId, key, associatedData);
    }

    /// <summary>Decrypts AES-GCM-RSA ciphertext.</summary>
    /// <param name="encryptedData">Ciphertext</param>
    /// <param name="keyId">Ignored. AES-GCM-RSA uses RSA keys from the constructor; present only for the interface.</param>
    /// <param name="key">Optional AES key. Used only when the payload was encrypted with an external key (not embedded).</param>
    /// <param name="associatedData">
    /// Optional AAD authenticated at encrypt time; must equal the bytes passed to
    /// <see cref="Encrypt(byte[], string, byte[], byte[])" />.
    /// </param>
    /// <returns>Plaintext</returns>
    /// <exception cref="ArgumentException">Thrown when keyId is supplied</exception>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when encryptedData is empty (length &lt; 1) or shorter than the minimum size</exception>
    /// <exception cref="InvalidDataException">Thrown when layout is invalid, encrypted key+nonce length is illegal, nonce length is illegal, or data is corrupted</exception>
    /// <exception cref="InvalidOperationException">Thrown when no decrypt key is supplied and the payload used an external key</exception>
    /// <exception cref="DecryptionFailedException">
    /// Thrown when decrypt fails: wrong RSA key (embedded key), wrong AES key (external key), damaged data, authentication
    /// failure, or tampering
    /// </exception>
    public byte[] Decrypt(byte[] encryptedData, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        // AES-GCM-RSA keys come from the constructor, not keyId
        ArgumentHelpers.ThrowIf(keyId != null, "AES-GCM-RSA decryption service uses RSA keys from constructor. The 'keyId' parameter is not supported.", nameof(keyId));
        // smallest size: hasEmbeddedKey flag (1) plus some payload
        const int minEncryptedSize = 1 + 4 + AesGcmHelper.NonceSize + AesGcmHelper.TagSize;
        ArgumentHelpers.ThrowIfNotInRange(encryptedData, minEncryptedSize, MaxInputSize);
        ReadOnlySpan<byte> encrypted = encryptedData;
        var o = 0;
        var hasEmbeddedKey = encrypted[o++] == 1;
        byte[]? aesKey = null;
        byte[]? keyNonce = null;
        ReadOnlySpan<byte> nonce = default;
        try {
            if (hasEmbeddedKey) {
                if (encrypted.Length - o < 4)
                    throw new InvalidDataException("Invalid encrypted data format: insufficient data for encrypted key+nonce length.");

                var encryptedKeyNonceLength = BinaryPrimitives.ReadInt32LittleEndian(encrypted.Slice(o, 4));
                o += 4;

                // reject illegal encrypted key+nonce lengths to limit DoS
                // RSA ciphertext length equals the RSA key size in bytes
                var expectedRsaEncryptedSize = _rsa.KeySize / 8;
                if (encryptedKeyNonceLength <= 0)
                    throw new InvalidDataException($"Invalid encrypted key+nonce length: {encryptedKeyNonceLength}. Length must be positive.");

                if (encryptedKeyNonceLength > expectedRsaEncryptedSize) {
                    throw new InvalidDataException(
                        $"Invalid encrypted key+nonce length: {encryptedKeyNonceLength} bytes. Maximum allowed: {expectedRsaEncryptedSize} bytes (RSA key size: {_rsa.KeySize} bits).");
                }

                if (encrypted.Length - o < encryptedKeyNonceLength) {
                    throw new InvalidDataException(
                        $"Invalid encrypted data format: encrypted key+nonce length ({encryptedKeyNonceLength} bytes) exceeds remaining stream size ({encrypted.Length - o} bytes).");
                }

                var encryptedKeyNonce = encrypted.Slice(o, encryptedKeyNonceLength).ToArray();
                o += encryptedKeyNonceLength;
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
                // split keyNonce into key and nonce
                // keyNonce = [key bytes][nonce bytes]
                // nonce is always 12 bytes; the rest is the key
                var keySize = keyNonce.Length - AesGcmHelper.NonceSize;
                if (keySize != _aesKeyLengthBytes)
                    throw new DecryptionFailedException($"Invalid decrypted AES key size: {keySize} bytes. Expected {_aesKeyLengthBytes} bytes for this service configuration.");

                aesKey = new byte[keySize];
                Buffer.BlockCopy(keyNonce, 0, aesKey, 0, keySize);
                nonce = keyNonce.AsSpan(keySize, AesGcmHelper.NonceSize);
            }
            else {
                if (encrypted.Length - o < 4)
                    throw new InvalidDataException("Invalid encrypted data format: insufficient data for nonce length.");

                var nonceLength = BinaryPrimitives.ReadInt32LittleEndian(encrypted.Slice(o, 4));
                o += 4;

                // reject illegal nonce lengths to limit DoS
                ArgumentHelpers.ThrowIfNotInRange(
                    nonceLength, AesGcmHelper.NonceSize, AesGcmHelper.NonceSize, nameof(encryptedData),
                    $"Invalid nonce length: {nonceLength}. Expected {AesGcmHelper.NonceSize} bytes.");

                if (encrypted.Length - o < nonceLength)
                    throw new InvalidDataException(
                        $"Invalid encrypted data format: nonce length ({nonceLength} bytes) exceeds remaining stream size ({encrypted.Length - o} bytes).");

                nonce = encrypted.Slice(o, nonceLength);
                o += nonceLength;
                OperationHelpers.ThrowIfNull(key, "No decryption key provided. Data was encrypted with external key.");
                AesGcmHelper.ValidateKeyLength(key, _aesKeyLengthBytes);
                aesKey = key;
            }

            if (encrypted.Length - o < AesGcmHelper.TagSize)
                throw new InvalidDataException("Invalid encrypted data format: truncated tag or ciphertext.");

            var tag = encrypted.Slice(o, AesGcmHelper.TagSize);
            o += AesGcmHelper.TagSize;
            var ciphertext = encrypted[o..];
            var plaintext = new byte[ciphertext.Length];
            try {
                AesGcmHelper.Decrypt(ciphertext, tag, aesKey!, nonce, plaintext, associatedData);
                return plaintext;
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
            // wipe secrets after decrypt
            if (keyNonce != null)
                SecurityUtilities.Clear(keyNonce);

            // only wipe aesKey when it was unwrapped (not supplied by the caller)
            if (hasEmbeddedKey && aesKey != null)
                SecurityUtilities.Clear(aesKey);
        }
    }

    /// <inheritdoc />
    public byte[] Encrypt(ReadOnlySpan<byte> plaintext, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIfNotInRange((long)plaintext.Length, MinInputSize, MaxInputSize, nameof(plaintext));
        return EncryptCore(plaintext, keyId, key, associatedData);
    }

    /// <inheritdoc />
    public byte[] EncryptString(string text, string? keyId = null, byte[]? key = null, Encoding? encoding = null)
        => Encrypt((encoding ?? GetEncryptionEncoding()).GetBytes(text), keyId, key);

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
    public Task EncryptToStreamAsync(
        Stream input,
        Stream output,
        string? keyId = null,
        byte[]? key = null,
        int chunkSize = 1024 * 1024,
        byte[]? associatedData = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNull(output);
        OperationHelpers.ThrowIfNotReadable(input, $"Stream '{nameof(input)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(output, $"Stream '{nameof(output)}' must be writable.");
        if (associatedData != null)
            throw new NotSupportedException("AES-GCM-RSA streaming does not support associated data.");

        return RsaStreamCodec.EncryptAsync(input, output, (byte)EncryptionAlgorithm.AesGcmRsa, (byte)StreamFormatVersion.V1, chunkSize, EncryptChunkTransform(keyId, key), ct);
    }

    /// <inheritdoc />
    public async Task EncryptToFileAsync(byte[] data, string outputPath, string? keyId = null, byte[]? key = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        using var inputStream = new MemoryStream(data);
        using var outputStream = File.Create(outputPath);
        await EncryptToStreamAsync(inputStream, outputStream, keyId, key, ct: ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task EncryptToFileAsync(Stream input, string outputPath, string? keyId = null, byte[]? key = null, int chunkSize = 1024 * 1024, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentHelpers.ThrowIfNegative(chunkSize);
        using var outputStream = File.Create(outputPath);
        await EncryptToStreamAsync(input, outputStream, keyId, key, chunkSize, ct: ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task DecryptToStreamAsync(Stream input, Stream output, string? keyId = null, byte[]? key = null, byte[]? associatedData = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNull(output);
        OperationHelpers.ThrowIfNotReadable(input, $"Stream '{nameof(input)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(output, $"Stream '{nameof(output)}' must be writable.");
        if (associatedData != null)
            throw new NotSupportedException("AES-GCM-RSA streaming does not support associated data.");

        return RsaStreamCodec.DecryptAsync(input, output, (byte)EncryptionAlgorithm.AesGcmRsa, (byte)StreamFormatVersion.V1, DecryptChunkTransform(keyId, key), ct);
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

    private byte[] EncryptCore(ReadOnlySpan<byte> plaintext, string? keyId, byte[]? key, byte[]? associatedData)
    {
        // AES-GCM-RSA keys come from the constructor, not keyId
        ArgumentHelpers.ThrowIf(keyId != null, "AES-GCM-RSA encryption service uses RSA keys from constructor. The 'keyId' parameter is not supported.", nameof(keyId));

        // if a key is passed in, validate and use it (backward compatible)
        // otherwise mint a random key (default)
        if (key != null)
            AesGcmHelper.ValidateKeyLength(key, _aesKeyLengthBytes);

        var aesKey = key ?? CryptographicRandom.GetBytes(_aesKeyLengthBytes);
        var hasExternalKey = key != null;
        try {
            if (!hasExternalKey) {
                // [1][encryptedKeyNonceLen:i32][encryptedKeyNonce][tag][ciphertext]
                var keyNonce = new byte[_aesKeyLengthBytes + AesGcmHelper.NonceSize];
                try {
                    aesKey.CopyTo(keyNonce.AsSpan());
                    CryptographicRandom.Fill(keyNonce.AsSpan(_aesKeyLengthBytes, AesGcmHelper.NonceSize));
                    var encryptedKeyNonce = _rsa.Encrypt(keyNonce, _padding);
                    var prefixLen = 1 + 4 + encryptedKeyNonce.Length + AesGcmHelper.TagSize;
                    var result = new byte[prefixLen + plaintext.Length];
                    var o = 0;
                    result[o++] = 1;
                    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(o, 4), encryptedKeyNonce.Length);
                    o += 4;
                    encryptedKeyNonce.CopyTo(result.AsSpan(o));
                    o += encryptedKeyNonce.Length;
                    var tagSpan = result.AsSpan(o, AesGcmHelper.TagSize);
                    o += AesGcmHelper.TagSize;
                    var ctSpan = result.AsSpan(o, plaintext.Length);
                    AesGcmHelper.Encrypt(plaintext, aesKey, keyNonce.AsSpan(_aesKeyLengthBytes, AesGcmHelper.NonceSize), ctSpan, tagSpan, associatedData);
                    return result;
                }
                finally {
                    SecurityUtilities.Clear(keyNonce);
                }
            }
            else {
                // [0][nonceLen:i32][nonce][tag][ciphertext]
                var prefixLen = 1 + 4 + AesGcmHelper.NonceSize + AesGcmHelper.TagSize;
                var result = new byte[prefixLen + plaintext.Length];
                var o = 0;
                result[o++] = 0;
                BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(o, 4), AesGcmHelper.NonceSize);
                o += 4;
                var nonceSpan = result.AsSpan(o, AesGcmHelper.NonceSize);
                CryptographicRandom.Fill(nonceSpan);
                o += AesGcmHelper.NonceSize;
                var tagSpan = result.AsSpan(o, AesGcmHelper.TagSize);
                o += AesGcmHelper.TagSize;
                var ctSpan = result.AsSpan(o, plaintext.Length);
                AesGcmHelper.Encrypt(plaintext, aesKey, nonceSpan, ctSpan, tagSpan, associatedData);
                return result;
            }
        }
        finally {
            // wipe the randomly generated AES key
            if (!hasExternalKey)
                SecurityUtilities.Clear(aesKey);
        }
    }

    private Func<byte[], int, int, byte[]> EncryptChunkTransform(string? keyId, byte[]? key)
        => (buffer, offset, count) => {
            var chunk = new byte[count];
            Array.Copy(buffer, offset, chunk, 0, count);
            return Encrypt(chunk, keyId, key);
        };

    private Func<byte[], int, int, byte[]> DecryptChunkTransform(string? keyId, byte[]? key) => (buffer, offset, count) => Decrypt(buffer, offset, count, keyId, key);

    private void Dispose(bool disposing)
    {
        if (_disposed || !disposing)
            return;

        _rsa.Dispose();
        _disposed = true;
    }
}