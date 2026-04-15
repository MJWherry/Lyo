using Lyo.Cache.Internal;
using Lyo.Compression;
using Lyo.Compression.Models;
using Lyo.Encryption;
using Lyo.Encryption.Models;
using Lyo.Exceptions;

namespace Lyo.Cache;

/// <summary>Default <see cref="ICachePayloadCodec"/> using <see cref="ICompressionService"/> and optionally <see cref="IEncryptionService"/>.</summary>
public sealed class CachePayloadCodec : ICachePayloadCodec
{
    private readonly CacheOptions _cacheOptions;
    private readonly ICompressionService _compression;
    private readonly IEncryptionService? _encryption;

    public CachePayloadCodec(CacheOptions cacheOptions, ICompressionService compression, IEncryptionService? encryption = null)
    {
        _cacheOptions = cacheOptions ?? throw new ArgumentNullException(nameof(cacheOptions));
        _compression = compression ?? throw new ArgumentNullException(nameof(compression));
        _encryption = encryption;
    }

    /// <inheritdoc />
    public byte[] Encode(ReadOnlySpan<byte> plaintext)
        => EncodeReturningEnvelope(plaintext).Framed;

    /// <inheritdoc />
    public (byte[] Framed, CacheEntryEnvelope Envelope) EncodeReturningEnvelope(ReadOnlySpan<byte> plaintext)
    {
        var o = _cacheOptions.Payload;
        byte flags = 0;
        var applicationPlaintext = plaintext.ToArray();
        var working = applicationPlaintext;
        CompressionResult? compressionResult = null;
        EncryptionResult? encryptionResult = null;

        if (o.AutoCompress && plaintext.Length >= o.AutoCompressMinSizeBytes) {
            var compressInfo = _compression.Compress(working, out var compressed);
            if (compressed.Length < working.Length) {
                flags |= CachePayloadFrame.FlagCompressed;
                compressionResult = CompressionResult.FromSuccess(compressed, compressInfo);
                working = compressed;
            }
        }

        if (o.AutoEncrypt) {
            OperationHelpers.ThrowIfNull(_encryption, "AutoEncrypt is enabled but no IEncryptionService is registered.");
            working = _encryption.Encrypt(working, o.EncryptionKeyId);
            flags |= CachePayloadFrame.FlagEncrypted;
            encryptionResult = EncryptionResult.FromSuccess(working, o.EncryptionKeyId);
        }

        var framed = CachePayloadFrame.Create(working, flags);
        var envelope = new CacheEntryEnvelope(applicationPlaintext, compressionResult, encryptionResult);
        return (framed, envelope);
    }

    /// <inheritdoc />
    public CacheEntryEnvelope Decode(byte[] framed)
    {
        ArgumentHelpers.ThrowIfNull(framed, nameof(framed));
        CachePayloadFrame.Parse(framed, out var flags, out var payloadSpan);
        var working = payloadSpan.ToArray();

        CompressionResult? compressionResult = null;
        EncryptionResult? encryptionResult = null;
        if ((flags & CachePayloadFrame.FlagEncrypted) != 0) {
            if (_encryption == null)
                throw new InvalidDataException("Payload is encrypted but no IEncryptionService is available.");

            encryptionResult = EncryptionResult.FromSuccess(working, _cacheOptions.Payload.EncryptionKeyId);
            working = _encryption.Decrypt(working, _cacheOptions.Payload.EncryptionKeyId);
        }
        if ((flags & CachePayloadFrame.FlagCompressed) == 0)
            return new(working, compressionResult, encryptionResult);

        var compressedCopy = working;
        var decInfo = _compression.Decompress(compressedCopy, out var decompressed);
        var ci = new CompressionInfo(decInfo.DecompressedSize, decInfo.CompressedSize, decInfo.DecompressionTimeMs);
        compressionResult = CompressionResult.FromSuccess(compressedCopy, ci);
        working = decompressed;

        return new(working, compressionResult, encryptionResult);
    }
}
