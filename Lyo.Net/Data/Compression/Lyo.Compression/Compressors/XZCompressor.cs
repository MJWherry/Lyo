using System.Reflection;
using System.Runtime.InteropServices;
using EasyCompressor;
using Joveler.Compression.XZ;

namespace Lyo.Compression.Compressors;

/// <summary>XZ compressor adapter implementing <see cref="ICompressor" /> using Joveler.Compression.XZ.</summary>
internal sealed class XZCompressor : ICompressor
{
    private static readonly object InitLock = new();
    private static bool _initialized;

    public XZCompressor(string? name = null)
    {
        Name = name;
        EnsureInitialized();
    }

    public string? Name { get; }

    public CompressionMethod Method => CompressionMethod.LZMA; // XZ uses LZMA2

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

    public void Compress(Stream inputStream, Stream outputStream)
    {
        var compOpts = new XZCompressOptions { Level = LzmaCompLevel.Default };
        using (var xz = new XZStream(outputStream, compOpts))
            inputStream.CopyTo(xz);

        outputStream.Flush();
    }

    public void Decompress(Stream inputStream, Stream outputStream)
    {
        var decompOpts = new XZDecompressOptions();
        using (var xz = new XZStream(inputStream, decompOpts))
            xz.CopyTo(outputStream);

        outputStream.Flush();
    }

    public async Task CompressAsync(Stream inputStream, Stream outputStream, CancellationToken ct = default)
    {
        var compOpts = new XZCompressOptions { Level = LzmaCompLevel.Default };
        using (var xz = new XZStream(outputStream, compOpts))
            await inputStream.CopyToAsync(xz, ct).ConfigureAwait(false);

        await outputStream.FlushAsync(ct).ConfigureAwait(false);
    }

    public async Task DecompressAsync(Stream inputStream, Stream outputStream, CancellationToken ct = default)
    {
        var decompOpts = new XZDecompressOptions();
        using (var xz = new XZStream(inputStream, decompOpts))
            await xz.CopyToAsync(outputStream, ct).ConfigureAwait(false);

        await outputStream.FlushAsync(ct).ConfigureAwait(false);
    }

    private static void EnsureInitialized()
    {
        if (_initialized)
            return;

        lock (InitLock) {
            if (_initialized)
                return;

            try {
                XZInit.GlobalInit();
            }
            catch (DllNotFoundException) {
                // Try bundled native library (Joveler ships liblzma.so in runtimes)
                var libPath = GetBundledLibPath();
                if (libPath != null && File.Exists(libPath))
                    XZInit.GlobalInit(libPath);
                else
                    throw;
            }

            _initialized = true;
        }
    }

    private static string? GetBundledLibPath()
    {
        var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
        var arch = RuntimeInformation.ProcessArchitecture switch {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            var _ => null
        };

        var (dir, lib) = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ("win", "liblzma.dll") :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ("osx", "liblzma.dylib") : ("linux", "liblzma.so");

        return arch != null ? Path.Combine(baseDir, "runtimes", $"{dir}-{arch}", "native", lib) : null;
    }
}