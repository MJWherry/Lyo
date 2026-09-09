using System.Security.Cryptography;
using System.Text;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Encryption.Rsa;

/// <summary>
/// Encrypts with an RSA public key. RSA only accepts small payloads (roughly key size minus padding), so data is chunked automatically.
/// Use for small bodies or key exchange. Decrypt side is <see cref="RsaDecryptor" />. Thread-safe: each call owns its
/// cryptographic context and shares no mutable crypto state.
/// </summary>
public sealed class RsaEncryptor : IEncryptor, IDisposable, IAsyncDisposable
{
    private const long MinInputSize = 1;
    private const long MaxInputSize = long.MaxValue;

    private readonly int _maxChunkSize;

    private readonly RSAEncryptionPadding _padding;

    private readonly RSA _rsa;

    private bool _disposed;

    private Encoding _encryptionEncoding = Encoding.UTF8;

    /// <summary>Creates an RsaEncryptor.</summary>
    /// <param name="publicPemPath">Path to the RSA public-key PEM (SubjectPublicKeyInfo)</param>
    /// <param name="pfxPath">Path to a PFX certificate (alternative to PEM)</param>
    /// <param name="password">PFX password</param>
    /// <param name="padding">RSA padding. Default is OAEP-SHA256.</param>
    /// <param name="maxChunkSize">Largest plaintext chunk. Null means compute from key size and padding.</param>
    /// <exception cref="ConfigurationException">Thrown when no key configuration is supplied.</exception>
    public RsaEncryptor(string? publicPemPath = null, string? pfxPath = null, string? password = null, RSAEncryptionPadding? padding = null, int? maxChunkSize = null)
    {
        _padding = padding ?? RSAEncryptionPadding.OaepSHA256;

        // reject PKCS1 padding — it is not recommended
        ArgumentHelpers.ThrowIf(
            _padding.Mode == RSAEncryptionPaddingMode.Pkcs1, "PKCS1 padding is not recommended for security. Use OAEP padding (e.g., OAEP-SHA256) instead.", nameof(padding));

        // keep exactly one RSA instance (avoid a throwaway RSA.Create() that would leak on reassignment).
        if (!publicPemPath.IsNullOrEmpty())
            _rsa = RsaKeyLoader.LoadPublicFromPem(publicPemPath);
        else if (!pfxPath.IsNullOrEmpty() && !password.IsNullOrEmpty())
            _rsa = RsaKeyLoader.LoadFromPfx(pfxPath, password);
        else
            throw new ConfigurationException("No RSA key configuration provided. Specify either publicPemPath or (pfxPath, password).");

        // require at least 2048-bit RSA (3072+ preferred for new deployments)
        ArgumentHelpers.ThrowIf(
            _rsa.KeySize < 2048,
            $"RSA key size must be at least 2048 bits for security. Current key size: {_rsa.KeySize} bits. Consider using 3072 or 4096 bits for new deployments.");

        _maxChunkSize = maxChunkSize ?? CalculateMaxChunkSize(_rsa.KeySize, _padding);
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
    public string FileExtension => FileTypeInfo.LyoRsa.DefaultExtension;

    /// <inheritdoc />
    public Encoding GetEncryptionEncoding() => _encryptionEncoding;

    /// <inheritdoc />
    public void SetEncryptionEncoding(Encoding encoding) => _encryptionEncoding = encoding ?? throw new ArgumentNullException(nameof(encoding));

    /// <summary>
    /// Encrypts with RSA. Thread-safe: concurrent calls on one instance are safe because each call owns its
    /// cryptographic context and shares no mutable crypto state.
    /// </summary>
    /// <param name="bytes">Plaintext. Must not be null or empty.</param>
    /// <param name="keyId">Ignored. RSA keys come from the constructor; present only for the interface.</param>
    /// <param name="key">Ignored. RSA keys come from the constructor; present only for the interface.</param>
    /// <param name="associatedData">Not supported by RSA; must be null.</param>
    /// <returns>Ciphertext</returns>
    /// <exception cref="ArgumentException">Thrown when keyId or key is supplied</exception>
    /// <exception cref="NotSupportedException">Thrown when associatedData is supplied (RSA-OAEP has no AAD input)</exception>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when bytes is empty (below MinInputSize) or exceeds MaxInputSize</exception>
    public byte[] Encrypt(byte[] bytes, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIfNotInRange(bytes, MinInputSize, MaxInputSize);
        // RSA keys come from the constructor, not these parameters (interface compliance)
        ArgumentHelpers.ThrowIf(keyId != null, "RSA encryption service uses keys from constructor. The 'keyId' parameter is not supported.", nameof(keyId));
        ArgumentHelpers.ThrowIf(key != null, "RSA encryption service uses keys from constructor. The 'key' parameter is not supported.", nameof(key));
        if (associatedData != null)
            throw new NotSupportedException("RSA encryption does not support associated data.");

        // single-chunk encrypt when the payload fits
        if (bytes.Length <= _maxChunkSize)
            return _rsa.Encrypt(bytes, _padding);

        // otherwise split into chunks
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
    public byte[] Encrypt(ReadOnlySpan<byte> plaintext, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
        => Encrypt(plaintext.ToArray(), keyId, key, associatedData);

    /// <inheritdoc />
    public byte[] EncryptString(string text, string? keyId = null, byte[]? key = null, Encoding? encoding = null)
        => Encrypt((encoding ?? GetEncryptionEncoding()).GetBytes(text), keyId, key);

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
            throw new NotSupportedException("RSA encryption does not support associated data.");

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
    public async Task EncryptToFileAsync(Stream input, string outputPath, string? keyId = null, byte[]? key = null, int chunkSize = 1024 * 1024, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentHelpers.ThrowIfNegative(chunkSize);
        using var outputStream = File.Create(outputPath);
        await EncryptToStreamAsync(input, outputStream, keyId, key, chunkSize, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Largest plaintext chunk RSA can encrypt for the current key size and padding.</summary>
    private static int CalculateMaxChunkSize(int keySizeBits, RSAEncryptionPadding padding)
    {
        var keySizeBytes = keySizeBits / 8;
        // padding overhead
        // OAEP overhead: 2 + hashSize + labelLength (usually 0) + padding
        // OAEP-SHA256: about 66 bytes
        // OAEP-SHA1: about 42 bytes
        // PKCS1: 11 bytes
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