using System.Collections.Concurrent;

namespace Lyo.Compression.Models;

/// <summary>
/// Extensible identifier for a stream compression algorithm. Use the built-in static fields (<see cref="GZip" />, <see cref="Deflate" />, <see cref="Brotli" />,
/// <see cref="ZLib" />) for algorithms shipped in the base <c>Lyo.Compression</c> package; addon packages (e.g. <c>Lyo.Compression.LZ4</c>) define their own subclass and a
/// <c>static readonly Instance</c> singleton.
/// </summary>
/// <remarks>
/// <para>
/// Replaces the closed <c>enum CompressionAlgorithm</c>. The base record carries the algorithm <see cref="Name" /> and default file <see cref="Extension" />; addon packages
/// expose their own <c>CompressionAlgorithm</c>-derived record so consumers can pattern-match by type (e.g. <c>algorithm is Lz4CompressionAlgorithm</c>) and so addons can
/// self-register without modifying the core package.
/// </para>
/// <para>
/// Each instance self-registers in the static name/extension lookup tables when its constructor runs; addons therefore become discoverable via <see cref="TryFromExtension" /> /
/// <see cref="TryFromName" /> as soon as their assembly is loaded (typically via the addon's <c>services.Add{Algo}Compressor()</c> DI extension).
/// </para>
/// </remarks>
public abstract record CompressionAlgorithm
{
    private static readonly ConcurrentDictionary<string, CompressionAlgorithm> _byExt = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, CompressionAlgorithm> _byName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Stable display name (e.g. <c>"GZip"</c>, <c>"LZ4"</c>). Used as the discriminator key for addon registrars and error messages.</summary>
    public string Name { get; }

    /// <summary>Default file extension including the leading dot (e.g. <c>".gz"</c>, <c>".lz4"</c>).</summary>
    public string Extension { get; }

    /// <param name="name">Stable algorithm name; case-insensitive lookup key.</param>
    /// <param name="extension">Default file extension including the leading dot.</param>
    protected CompressionAlgorithm(string name, string extension)
    {
        Name = name;
        Extension = extension;
        _byName.TryAdd(name, this);
        _byExt.TryAdd(extension, this);
    }

    /// <summary>Looks up a registered algorithm by file extension. Case-insensitive. Only returns algorithms whose assemblies are loaded.</summary>
    public static CompressionAlgorithm? TryFromExtension(string? extension) => !string.IsNullOrEmpty(extension) && _byExt.TryGetValue(extension!, out var algo) ? algo : null;

    /// <summary>Looks up a registered algorithm by <see cref="Name" />. Case-insensitive. Only returns algorithms whose assemblies are loaded.</summary>
    public static CompressionAlgorithm? TryFromName(string? name) => !string.IsNullOrEmpty(name) && _byName.TryGetValue(name!, out var algo) ? algo : null;

    /// <summary>All algorithms whose assemblies have been loaded into the current AppDomain.</summary>
    public static IReadOnlyCollection<CompressionAlgorithm> All => _byName.Values.ToArray();

    public sealed override string ToString() => Name;

    /// <summary>GZIP container around DEFLATE; ubiquitous <c>.gz</c> format.</summary>
    public static readonly CompressionAlgorithm GZip = new BuiltInCompressionAlgorithm("GZip", ".gz");

    /// <summary>Raw DEFLATE bitstream (no zlib/gzip wrapper).</summary>
    public static readonly CompressionAlgorithm Deflate = new BuiltInCompressionAlgorithm("Deflate", ".deflate");

#if !NETSTANDARD2_0
    /// <summary>Brotli (RFC 7932); strong ratio, common for HTTP and static assets. Not available on <c>netstandard2.0</c>.</summary>
    public static readonly CompressionAlgorithm Brotli = new BuiltInCompressionAlgorithm("Brotli", ".br");

    /// <summary>ZLIB (RFC 1950) wrapper around DEFLATE. Not available on <c>netstandard2.0</c>.</summary>
    public static readonly CompressionAlgorithm ZLib = new BuiltInCompressionAlgorithm("ZLib", ".zlib");
#endif
}

/// <summary>Concrete record used by built-in algorithms shipped in <c>Lyo.Compression</c>. Addon packages should declare their own dedicated subclass instead of reusing this.</summary>
public sealed record BuiltInCompressionAlgorithm : CompressionAlgorithm
{
    /// <param name="name">Algorithm name (e.g. <c>"GZip"</c>).</param>
    /// <param name="extension">Default file extension including the leading dot.</param>
    public BuiltInCompressionAlgorithm(string name, string extension)
        : base(name, extension) { }
}