using System.Security.Cryptography;
using System.Text;
using Lyo.Common.Extensions;
using Lyo.Common.Records;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Encryption.Rsa;

/// <summary>
/// Encrypts data using RSA with a public key. RSA can only encrypt small amounts of data (typically up to key size minus padding overhead), so data is automatically chunked.
/// Suitable for encrypting small data directly or for key exchange scenarios. Pairs with <see cref="RsaDecryptor" /> for the decrypt side. Thread-safe: each method call uses its
/// own cryptographic context, so there are no shared mutable state concerns.
/// </summary>
public sealed class RsaEncryptor : IEncryptor, IDisposable, IAsyncDisposable
{
    private const long MinInputSize = 1;
    private const long MaxInputSize = long.MaxValue;

    private readonly int _maxChunkSize;

    private readonly RSAEncryptionPadding _padding;

    private readonly RSA _rsa;

    private bool _disposed;

    /// <summary> Initializes a new instance of the RsaEncryptor. </summary>
    /// <param name="publicPemPath">Path to the RSA public key PEM file (SubjectPublicKeyInfo)</param>
    /// <param name="pfxPath">Path to the PFX certificate file (alternative to PEM)</param>
    /// <param name="password">Password for the PFX certificate</param>
    /// <param name="padding">RSA encryption padding. Defaults to OAEP-SHA256.</param>
    /// <param name="maxChunkSize">Maximum chunk size for encryption. If null, automatically calculated based on key size and padding.</param>
    /// <exception cref="InvalidOperationException">Thrown when no key configuration is provided.</exception>
    public RsaEncryptor(
        string? publicPemPath = null,
        string? pfxPath = null,
        string? password = null,
        RSAEncryptionPadding? padding = null,
        int? maxChunkSize = null)
    {
        _padding = padding ?? RSAEncryptionPadding.OaepSHA256;

        // Validate padding mode - PKCS1 is not recommended for security
        ArgumentHelpers.ThrowIf(
            _padding.Mode == RSAEncryptionPaddingMode.Pkcs1, "PKCS1 padding is not recommended for security. Use OAEP padding (e.g., OAEP-SHA256) instead.", nameof(padding));

        _rsa = RSA.Create();
        if (!publicPemPath.IsNullOrEmpty())
            _rsa = RsaKeyLoader.LoadPublicFromPem(publicPemPath);
        else if (!pfxPath.IsNullOrEmpty() && !password.IsNullOrEmpty())
            _rsa = RsaKeyLoader.LoadFromPfx(pfxPath, password);
        else
            OperationHelpers.ThrowIf(true, "No RSA key configuration provided. Specify either publicPemPath or (pfxPath, password).");

        // Validate RSA key size - minimum 2048 bits recommended (3072+ preferred for new deployments)
        ArgumentHelpers.ThrowIf(
            _rsa.KeySize < 2048,
            $"RSA key size must be at least 2048 bits for security. Current key size: {_rsa.KeySize} bits. Consider using 3072 or 4096 bits for new deployments.");

        _maxChunkSize = maxChunkSize ?? CalculateMaxChunkSize(_rsa.KeySize, _padding);
    }

    /// <inheritdoc />
    public string FileExtension => FileTypeInfo.LyoRsa.DefaultExtension;

    private Encoding _encryptionEncoding = Encoding.UTF8;

    /// <inheritdoc />
    public Encoding GetEncryptionEncoding() => _encryptionEncoding;

    /// <inheritdoc />
    public void SetEncryptionEncoding(Encoding encoding) => _encryptionEncoding = encoding ?? throw new ArgumentNullException(nameof(encoding));

    /// <summary> Asynchronously disposes of the RSA instance and releases all resources. </summary>
    public ValueTask DisposeAsync()
    {
        Dispose(true);
        return default;
    }

    /// <summary> Disposes of the RSA instance and releases all resources. </summary>
    public void Dispose() => Dispose(true);

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
    public byte[] Encrypt(byte[] bytes, string? keyId = null, byte[]? key = null)
    {
        ArgumentHelpers.ThrowIfNotInRange(bytes, MinInputSize, MaxInputSize);
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

    /// <inheritdoc />
    public byte[] Encrypt(ReadOnlySpan<byte> plaintext, string? keyId = null, byte[]? key = null) => Encrypt(plaintext.ToArray(), keyId, key);

    /// <inheritdoc />
    public byte[] EncryptString(string text, string? keyId = null, byte[]? key = null, Encoding? encoding = null)
        => Encrypt((encoding ?? GetEncryptionEncoding()).GetBytes(text), keyId, key);

    /// <inheritdoc />
    public Task EncryptToStreamAsync(Stream input, Stream output, string? keyId = null, byte[]? key = null, int chunkSize = 1024 * 1024, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNull(output);
        OperationHelpers.ThrowIfNotReadable(input, $"Stream '{nameof(input)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(output, $"Stream '{nameof(output)}' must be writable.");
        return RsaStreamCodec.EncryptAsync(input, output, (byte)EncryptionAlgorithm.Rsa, (byte)StreamFormatVersion.V1, chunkSize, EncryptChunk(keyId, key), ct);
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
    public async Task EncryptToFileAsync(
        Stream input,
        string outputPath,
        string? keyId = null,
        byte[]? key = null,
        int chunkSize = 1024 * 1024,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentHelpers.ThrowIfNegative(chunkSize);
        using var outputStream = File.Create(outputPath);
        await EncryptToStreamAsync(input, outputStream, keyId, key, chunkSize, ct).ConfigureAwait(false);
    }

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

    private Func<byte[], int, int, byte[]> EncryptChunk(string? keyId, byte[]? key)
        => (buffer, offset, count) => {
            var chunk = new byte[count];
            Array.Copy(buffer, offset, chunk, 0, count);
            return Encrypt(chunk, keyId, key);
        };

    private void Dispose(bool disposing)
    {
        if (_disposed || !disposing)
            return;

        _rsa.Dispose();
        _disposed = true;
    }
}
