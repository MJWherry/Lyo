using Lyo.Cache.Internal;
using Lyo.Compression;
using Lyo.Compression.Models;
using Lyo.Exceptions;
#if NET10_0_OR_GREATER
using Lyo.Encryption;
using Lyo.Encryption.Models;
#endif

namespace Lyo.Cache;

/// <summary>Default <see cref="ICachePayloadCodec"/> using <see cref="ICompressionService"/> and optionally <see cref="IEncryptionService"/> (net10).</summary>
public sealed class CachePayloadCodec : ICachePayloadCodec
{
    private readonly CacheOptions _cacheOptions;
    private readonly ICompressionService _compression;
#if NET10_0_OR_GREATER
    private readonly IEncryptionService? _encryption;
#endif

    public CachePayloadCodec(CacheOptions cacheOptions, ICompressionService compression
#if NET10_0_OR_GREATER
        , IEncryptionService? encryption = null
#endif
    )
    {
        _cacheOptions = cacheOptions ?? throw new ArgumentNullException(nameof(cacheOptions));
        _compression = compression ?? throw new ArgumentNullException(nameof(compression));
#if NET10_0_OR_GREATER
        _encryption = encryption;
#endif
    }

    /// <inheritdoc />
    public byte[] Encode(ReadOnlySpan<byte> plaintext)
    {
        var o = _cacheOptions.Payload;
        byte flags = 0;
        var working = plaintext.ToArray();

        if (o.AutoCompress && plaintext.Length >= o.AutoCompressMinSizeBytes) {
            _ = _compression.Compress(working, out var compressed);
            if (compressed.Length < working.Length) {
                flags |= CachePayloadFrame.FlagCompressed;
                working = compressed;
            }
        }

#if NET10_0_OR_GREATER
        if (o.AutoEncrypt) {
            OperationHelpers.ThrowIfNull(_encryption, "AutoEncrypt is enabled but no IEncryptionService is registered.");
            working = _encryption.Encrypt(working, o.EncryptionKeyId);
            flags |= CachePayloadFrame.FlagEncrypted;
        }
#endif

        return CachePayloadFrame.Create(working, flags);
    }

    /// <inheritdoc />
    public CacheEntryEnvelope Decode(byte[] framed)
    {
        ArgumentHelpers.ThrowIfNull(framed, nameof(framed));
        CachePayloadFrame.Parse(framed, out var flags, out var payloadSpan);
        var working = payloadSpan.ToArray();

        CompressionResult? compressionResult = null;
#if NET10_0_OR_GREATER
        EncryptionResult? encryptionResult = null;
        if ((flags & CachePayloadFrame.FlagEncrypted) != 0) {
            if (_encryption == null)
                throw new InvalidDataException("Payload is encrypted but no IEncryptionService is available.");

            encryptionResult = EncryptionResult.FromSuccess(working, _cacheOptions.Payload.EncryptionKeyId);
            working = _encryption.Decrypt(working, _cacheOptions.Payload.EncryptionKeyId);
        }
#else
        if ((flags & CachePayloadFrame.FlagEncrypted) != 0)
            throw new InvalidDataException("Payload is encrypted; use a net10.0 build with IEncryptionService to decode.");
#endif
        if ((flags & CachePayloadFrame.FlagCompressed) == 0) 
            return new() { Compression = compressionResult, Payload = working };

        var compressedCopy = working;
        var decInfo = _compression.Decompress(compressedCopy, out var decompressed);
        var ci = new CompressionInfo(decInfo.DecompressedSize, decInfo.CompressedSize, decInfo.DecompressionTimeMs);
        compressionResult = CompressionResult.FromSuccess(compressedCopy, ci);
        working = decompressed;

#if NET10_0_OR_GREATER
        return new() {
            Compression = compressionResult,
            Encryption = encryptionResult,
            Payload = working
        };
#else
        return new() {
            Compression = compressionResult,
            Payload = working
        };
#endif
    }
}
