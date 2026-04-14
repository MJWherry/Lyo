using EasyCompressor;
using ICSharpCode.SharpZipLib.BZip2;

namespace Lyo.Compression.Compressors;

/// <summary>BZip2 compressor adapter implementing <see cref="ICompressor" /> using SharpZipLib.</summary>
internal sealed class BZip2Compressor : ICompressor
{
    private const int DefaultCompressionLevel = 9;

    private readonly int _level;

    public BZip2Compressor(string? name = null, int level = DefaultCompressionLevel)
    {
        Name = name;
        _level = level < 1 ? 1 : level > 9 ? 9 : level;
    }

    public string? Name { get; }

    public CompressionMethod Method => CompressionMethod.Deflate; // BZip2 - no enum value, use closest

    public byte[] Compress(byte[] bytes)
    {
        using var input = new MemoryStream(bytes);
        using var output = new MemoryStream();
        Compress(input, output);
        return output.ToArray();
    }

    public byte[] Decompress(byte[] compressedBytes)
    {
        using var input = new MemoryStream(compressedBytes);
        using var output = new MemoryStream();
        Decompress(input, output);
        return output.ToArray();
    }

    public void Compress(Stream inputStream, Stream outputStream) => BZip2.Compress(inputStream, outputStream, false, _level);

    public void Decompress(Stream inputStream, Stream outputStream) => BZip2.Decompress(inputStream, outputStream, false);

    public Task CompressAsync(Stream inputStream, Stream outputStream, CancellationToken ct = default)
    {
        Compress(inputStream, outputStream);
        return Task.CompletedTask;
    }

    public Task DecompressAsync(Stream inputStream, Stream outputStream, CancellationToken ct = default)
    {
        Decompress(inputStream, outputStream);
        return Task.CompletedTask;
    }
}