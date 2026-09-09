using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Metadata.Extensions;
using Lyo.Common.Core.Net;
using Lyo.Common.Metadata.Records;
using Lyo.Compression.JsonConverters;

namespace Lyo.Compression.Models;

/// <summary>
/// Extensible identifier for a stream compression algorithm. Use the built-in static fields (<see cref="GZip" />, <see cref="Deflate" />, <see cref="Brotli" />,
/// <see cref="ZLib" />) for algorithms shipped in the base <c>Lyo.Compression</c> package; addon packages (e.g. <c>Lyo.Compression.Lz4</c>) declare their own subclass and a
/// <c>static readonly Instance</c> singleton.
/// </summary>
/// <remarks>
/// <para>
/// Replaces the closed <c>enum CompressionAlgorithm</c>. The base record holds the algorithm <see cref="Name" /> and default file <see cref="Extension" />; addon packages
/// expose their own <c>CompressionAlgorithm</c>-derived record so consumers can pattern-match by type (e.g. <c>algorithm is Lz4CompressionAlgorithm</c>) and so addons can
/// self-register without changing the core package.
/// </para>
/// <para>
/// Each instance self-registers in the static name/extension lookup tables when its constructor runs; addons therefore become discoverable via <see cref="TryFromExtension" /> /
/// <see cref="TryFromName" /> as soon as their assembly is loaded (typically via the addon's <c>services.Add{Algo}Compressor()</c> DI extension).
/// </para>
/// </remarks>
[JsonConverter(typeof(CompressionAlgorithmJsonConverter))]
public abstract record CompressionAlgorithm
{
    private static readonly ConcurrentDictionary<string, CompressionAlgorithm> ByExt = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, CompressionAlgorithm> ByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>GZIP container around DEFLATE; common <c>.gz</c> format.</summary>
    public static readonly CompressionAlgorithm GZip = new BuiltInCompressionAlgorithm(
        "GZip", ".gz", LyoContentEncodings.GZip, [[0x1F, 0x8B]]); // RFC 1952 header.

    /// <summary>Raw DEFLATE bitstream (no zlib or gzip wrapper).</summary>
    public static readonly CompressionAlgorithm Deflate = new BuiltInCompressionAlgorithm("Deflate", ".deflate", LyoContentEncodings.Deflate);

    /// <summary>Stable display name (e.g. <c>"GZip"</c>, <c>"LZ4"</c>). Discriminator key for addon registrars and error messages.</summary>
    public string Name { get; }

    /// <summary>Default file extension including the leading dot (examples: <c>".gz"</c>, <c>".lz4"</c>).</summary>
    public string Extension { get; }

    /// <summary>
    /// HTTP <c>Content-Encoding</c> token for this algorithm (see <see cref="LyoContentEncodings" />), or <see langword="null" /> when the format has no registered token and must
    /// not be advertised over HTTP.
    /// </summary>
    public string? ContentEncoding { get; }

    /// <summary>
    /// Magic-number prefixes that identify this format at the head of a stream, used by content sniffing. Empty for formats with no header at all — raw DEFLATE and Brotli
    /// have none, so any "detection" for them is guesswork.
    /// </summary>
    public IReadOnlyList<byte[]> MagicPrefixes { get; }

    /// <summary>
    /// Cross-reference into the shared file-type catalog, resolved from <see cref="Extension" />. Gives callers the MIME type and extension aliases without a second lookup
    /// table; falls back to <see cref="FileTypeInfo.Unknown" /> for extensions the catalog does not list.
    /// </summary>
    public FileTypeInfo FileType => FileTypeInfo.FromExtension(Extension);

    /// <summary>Algorithms whose <see cref="MagicPrefixes" /> allow detection, longest prefix first so a more specific header wins.</summary>
    public static IReadOnlyList<CompressionAlgorithm> Detectable
        => ByName.Values.Where(static a => a.MagicPrefixes.Count > 0).OrderByDescending(static a => a.MagicPrefixes.Max(static p => p.Length)).ToArray();

    /// <summary>
    /// True when the compressor's fast binary (<c>byte[]</c>) Compress emits the same wire format its stream Decompress reads. If true, <see cref="CompressionService" /> uses
    /// the binary API for <c>byte[]</c> compression (block codecs like LZ4/Snappy/Zstd are near memcpy-speed there); if false, it falls back to the stream path so every API produces
    /// one consistent format per algorithm. Override to false only for codecs whose binary and stream formats differ (e.g. Snappy's raw block vs framed format).
    /// </summary>
    public virtual bool BinaryCompressMatchesStreamFormat => true;

    /// <summary>All algorithms whose assemblies are loaded into the current AppDomain.</summary>
    public static IReadOnlyCollection<CompressionAlgorithm> All => ByName.Values.ToArray();

    /// <param name="name">Stable algorithm name; case-insensitive lookup key.</param>
    /// <param name="extension">Default file extension including the leading dot.</param>
    /// <param name="contentEncoding">HTTP <c>Content-Encoding</c> token, or <see langword="null" /> when the format has none.</param>
    /// <param name="magicPrefixes">Magic-number prefixes identifying the format at the head of a stream; omit for headerless formats.</param>
    protected CompressionAlgorithm(string name, string extension, string? contentEncoding = null, IReadOnlyList<byte[]>? magicPrefixes = null)
    {
        Name = name;
        Extension = extension;
        ContentEncoding = contentEncoding;
        MagicPrefixes = magicPrefixes ?? [];
        ByName.TryAdd(name, this);
        ByExt.TryAdd(extension, this);
    }

    /// <summary>Looks up a registered algorithm by HTTP <c>Content-Encoding</c> token. Case-insensitive. Only algorithms whose assemblies are loaded.</summary>
    public static CompressionAlgorithm? TryFromContentEncoding(string? contentEncoding)
        => contentEncoding.IsNullOrWhitespace()
            ? null
            : ByName.Values.FirstOrDefault(a => a.ContentEncoding != null && a.ContentEncoding.Equals(contentEncoding!.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>True when <paramref name="data" /> begins with any of this algorithm's <see cref="MagicPrefixes" />.</summary>
    public bool MatchesMagic(ReadOnlySpan<byte> data)
    {
        foreach (var prefix in MagicPrefixes) {
            if (data.Length >= prefix.Length && data[..prefix.Length].SequenceEqual(prefix))
                return true;
        }

        return false;
    }

    /// <summary>Looks up a registered algorithm by file extension. Case-insensitive. Only algorithms whose assemblies are loaded.</summary>
    public static CompressionAlgorithm? TryFromExtension(string? extension) => !extension.IsNullOrEmpty() && ByExt.TryGetValue(extension, out var algo) ? algo : null;

    /// <summary>Looks up a registered algorithm by <see cref="Name" />. Case-insensitive. Only algorithms whose assemblies are loaded.</summary>
    public static CompressionAlgorithm? TryFromName(string? name) => !name.IsNullOrEmpty() && ByName.TryGetValue(name, out var algo) ? algo : null;

    public sealed override string ToString() => Name;

#if !NETSTANDARD2_0
    /// <summary>Brotli (RFC 7932); strong ratio, common for HTTP and static assets. Unavailable on <c>netstandard2.0</c>.</summary>
    /// <remarks>Brotli streams carry no magic number, so <see cref="MagicPrefixes" /> is empty and Brotli payloads cannot be sniffed.</remarks>
    public static readonly CompressionAlgorithm Brotli = new BuiltInCompressionAlgorithm("Brotli", ".br", LyoContentEncodings.Brotli);

    /// <summary>ZLIB (RFC 1950) wrapper around DEFLATE. Unavailable on <c>netstandard2.0</c>.</summary>
    public static readonly CompressionAlgorithm ZLib = new BuiltInCompressionAlgorithm(
        // 0x78 CMF plus the four standard FLG values (level 1/fast/default/best), each keeping (CMF<<8|FLG) % 31 == 0.
        "ZLib", ".zlib", null, [[0x78, 0x01], [0x78, 0x5E], [0x78, 0x9C], [0x78, 0xDA]]);
#endif
}

/// <summary>Concrete record used by built-in algorithms shipped in <c>Lyo.Compression</c>. Addon packages should declare their own dedicated subclass instead of reusing this type.</summary>
public sealed record BuiltInCompressionAlgorithm : CompressionAlgorithm
{
    /// <param name="name">Algorithm name (e.g. <c>"GZip"</c>).</param>
    /// <param name="extension">Default file extension including the leading dot.</param>
    /// <param name="contentEncoding">HTTP <c>Content-Encoding</c> token, or <see langword="null" /> when the format has none.</param>
    /// <param name="magicPrefixes">Magic-number prefixes identifying the format at the head of a stream; omit for headerless formats.</param>
    public BuiltInCompressionAlgorithm(string name, string extension, string? contentEncoding = null, IReadOnlyList<byte[]>? magicPrefixes = null)
        : base(name, extension, contentEncoding, magicPrefixes) { }
}