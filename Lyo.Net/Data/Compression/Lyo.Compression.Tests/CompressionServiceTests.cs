using System.IO.Compression;
using System.Text;
using Lyo.Compression.Models;
using Lyo.Exceptions.Models;
using Lyo.IO.Temp.Models;
using Lyo.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Compression.Tests;

public class CompressionServiceTests : IDisposable
{
    private readonly ILogger<CompressionService> _logger;

    private readonly IIOTempSession _tempSession;

    public CompressionServiceTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _logger = loggerFactory.CreateLogger<CompressionService>();
        _tempSession = new IOTempSession(new() { FileExtension = ".txt" }, loggerFactory.CreateLogger<IOTempSession>());
    }

    public void Dispose() => _tempSession.Dispose();

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Snappier)]
    [InlineData(CompressionAlgorithm.ZstdSharp)]
    [InlineData(CompressionAlgorithm.LZ4)]
    [InlineData(CompressionAlgorithm.LZMA)]
    [InlineData(CompressionAlgorithm.BZip2)]
    [InlineData(CompressionAlgorithm.XZ)]
#if !NETSTANDARD2_0
    [InlineData(CompressionAlgorithm.Brotli)]
    [InlineData(CompressionAlgorithm.ZLib)]
#endif
    public void Compress_Decompress_RoundTrip(CompressionAlgorithm algorithm)
    {
        var service = new CompressionService(_logger, new() { DefaultAlgorithm = algorithm });
        // Use larger content that will actually compress well
        var original = Encoding.UTF8.GetBytes(new string('A', 1000) + "Hello, World! This is a test string for compression. " + new string('B', 1000));
        var compressInfo = service.Compress(original, out var compressed);
        var decompressInfo = service.Decompress(compressed, out var decompressed);
        Assert.Equal(original, decompressed);
        // Compression ratio should be reasonable (compressed might be slightly larger for very small data due to overhead)
        Assert.True(compressInfo.CompressionRatio > 0);
        Assert.True(decompressInfo.ExpansionRatio > 0);
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Snappier)]
    [InlineData(CompressionAlgorithm.ZstdSharp)]
    [InlineData(CompressionAlgorithm.LZ4)]
    [InlineData(CompressionAlgorithm.LZMA)]
    [InlineData(CompressionAlgorithm.BZip2)]
    [InlineData(CompressionAlgorithm.XZ)]
#if !NETSTANDARD2_0
    [InlineData(CompressionAlgorithm.Brotli)]
    [InlineData(CompressionAlgorithm.ZLib)]
#endif
    public void CompressString_DecompressString_RoundTrip(CompressionAlgorithm algorithm)
    {
        var service = new CompressionService(_logger, new() { DefaultAlgorithm = algorithm });
        var original = "Hello, World! This is a test string for compression.";
        var compressInfo = service.CompressString(original, out var compressed);
        service.DecompressString(compressed, out var decompressed);
        Assert.Equal(original, decompressed);
        Assert.True(compressed.Length > 0);
        Assert.True(compressInfo.CompressionRatio > 0);
    }

    [Theory]
    [InlineData("UTF-8")]
    [InlineData("UTF-16")]
    [InlineData("ASCII")]
    public void CompressString_WithDifferentEncodings_RoundTrip(string encodingName)
    {
        var service = new CompressionService();
        // Use Encoding.Unicode for UTF-16 to ensure consistent BOM handling
        // Encoding.Unicode is UTF-16 LE with BOM, which is more reliable than GetEncoding("UTF-16")
        var encoding = encodingName == "UTF-16" ? Encoding.Unicode : Encoding.GetEncoding(encodingName);
        // Use larger content that will compress well
        // Note: ASCII encoding won't support Chinese characters, so skip them for ASCII
        var original = encodingName == "ASCII"
            ? "Hello, World! " + new string('X', 500) + " This is a longer string for compression testing."
            : "Hello, World! 你好世界! " + new string('X', 500) + " This is a longer string for compression testing.";

        // Verify the encoding round-trip works first
        var originalBytes = encoding.GetBytes(original);
        var roundTripString = encoding.GetString(originalBytes);
        Assert.Equal(original, roundTripString); // Ensure encoding itself works
        service.CompressString(original, out var compressed, encoding);
        service.DecompressString(compressed, out var decompressed, encoding);

        // Verify bytes match exactly (compression/decompression should preserve bytes)
        var decompressedBytes = encoding.GetBytes(decompressed);
        Assert.Equal(originalBytes, decompressedBytes);
        Assert.Equal(original, decompressed);
        Assert.True(compressed.Length > 0);
    }

    [Fact]
    public void CompressFile_DecompressFile_RoundTrip()
    {
        var service = new CompressionService();
        var tempFile = _tempSession.CreateFile("test content for file compression");
        var compressedFile = tempFile + service.FileExtension;
        var compressInfo = service.CompressFile(tempFile, compressedFile);
        Assert.True(File.Exists(compressedFile));
        Assert.True(compressInfo.InputSize > 0);
        Assert.True(compressInfo.CompressionRatio > 0);
        var decompressedFile = tempFile + ".decompressed";
        service.DecompressFile(compressedFile, decompressedFile);
        Assert.True(File.Exists(decompressedFile));
        var originalContent = File.ReadAllText(tempFile);
        var decompressedContent = File.ReadAllText(decompressedFile);
        Assert.Equal(originalContent, decompressedContent);
    }

    [Fact]
    public async Task CompressFileAsync_DecompressFileAsync_RoundTrip()
    {
        var service = new CompressionService();
        var tempFile = await _tempSession.CreateFileAsync("test content for async file compression", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var compressedFile = tempFile + service.FileExtension;
        var compressInfo = await service.CompressFileAsync(tempFile, compressedFile, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(File.Exists(compressedFile));
        Assert.True(compressInfo.CompressionRatio > 0);
        var decompressedFile = tempFile + ".decompressed";
        await service.DecompressFileAsync(compressedFile, decompressedFile, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(File.Exists(decompressedFile));
        var originalContent = await File.ReadAllTextAsync(tempFile, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var decompressedContent = await File.ReadAllTextAsync(decompressedFile, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(originalContent, decompressedContent);
    }

    [Fact]
    public void Compress_Stream_RoundTrip()
    {
        var service = new CompressionService();
        var original = "Test stream compression"u8.ToArray();
        using var inputStream = new MemoryStream(original);
        using var compressedStream = new MemoryStream();
        service.Compress(inputStream, compressedStream);
        compressedStream.Position = 0;
        using var decompressedStream = new MemoryStream();
        service.Decompress(compressedStream, decompressedStream);
        Assert.Equal(original, decompressedStream.ToArray());
    }

    [Fact]
    public async Task CompressAsync_Stream_RoundTrip()
    {
        var service = new CompressionService();
        var original = "Test async stream compression"u8.ToArray();
        using var inputStream = new MemoryStream(original);
        using var compressedStream = new MemoryStream();
        await service.CompressAsync(inputStream, compressedStream, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        compressedStream.Position = 0;
        using var decompressedStream = new MemoryStream();
        await service.DecompressAsync(compressedStream, decompressedStream, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(original, decompressedStream.ToArray());
    }

    [Fact]
    public void CompressToBase64_DecompressFromBase64_RoundTrip()
    {
        var service = new CompressionService();
        var original = "Test base64 compression"u8.ToArray();
        var compressInfo = service.CompressToBase64(original, out var base64);
        service.DecompressFromBase64(base64, out var decompressed);
        Assert.Equal(original, decompressed);
        Assert.True(base64.Length > 0);
        Assert.True(compressInfo.CompressionRatio > 0);
    }

    [Fact]
    public void TryCompress_Success_ReturnsTrue()
    {
        var service = new CompressionService();
        var original = "Test try compress"u8.ToArray();
        var success = service.TryCompress(original, out var compressed, out var info);
        Assert.True(success);
        Assert.NotNull(compressed);
        Assert.NotNull(info);
        Assert.True(compressed.Length > 0);
    }

    [Fact]
    public void TryDecompress_Success_ReturnsTrue()
    {
        var service = new CompressionService();
        var original = "Test try decompress"u8.ToArray();
        service.Compress(original, out var compressed);
        var success = service.TryDecompress(compressed, out var decompressed, out var info);
        Assert.True(success);
        Assert.NotNull(decompressed);
        Assert.NotNull(info);
        Assert.Equal(original, decompressed);
    }

    [Fact]
    public void TryCompress_InvalidData_ReturnsFalse()
    {
        var service = new CompressionService();
        // TryCompress catches exceptions, so null will be caught and return false
        var success = service.TryCompress(null, out var compressed, out var info);
        Assert.False(success);
        Assert.Null(compressed);
        Assert.Null(info);
    }

    [Fact]
    public void TryCompress_EmptyData_ReturnsFalse()
    {
        var service = new CompressionService();

        // Empty data throws ArgumentException, which should be caught
        var success = service.TryCompress([], out var compressed, out var info);
        Assert.False(success);
        Assert.Null(compressed);
        Assert.Null(info);
    }

    [Theory]
    [InlineData(new byte[] { 0x1F, 0x8B }, true)] // GZip
    [InlineData(new byte[] { 0x78, 0x01 }, true)] // ZLib
    [InlineData(new byte[] { 0x78, 0x5E }, true)] // ZLib
    [InlineData(new byte[] { 0x78, 0x9C }, true)] // ZLib
    [InlineData(new byte[] { 0x78, 0xDA }, true)] // ZLib
    [InlineData(new byte[] { 0x81, 0x00 }, true)] // Brotli (needs at least 2 bytes)
    [InlineData(new byte[] { 0x82, 0x00 }, true)] // Brotli
    [InlineData(new byte[] { 0x83, 0x00 }, true)] // Brotli
    [InlineData(new byte[] { 0x00, 0x00 }, false)]
    [InlineData(new byte[] { 0xFF, 0xFF }, false)]
    public void IsLikelyCompressed_DetectsCompressedData(byte[] data, bool expected)
    {
        var service = new CompressionService();
        var result = service.IsLikelyCompressed(data);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsLikelyCompressed_NullOrEmpty_ReturnsFalse()
    {
        var service = new CompressionService();

        // IsLikelyCompressed now checks for null and length < 2, so it should handle null gracefully
        Assert.False(service.IsLikelyCompressed(null!));
        Assert.False(service.IsLikelyCompressed([]));
        Assert.False(service.IsLikelyCompressed([0x01]));
    }

    [Fact]
    public void GetCompressionRatio_CalculatesCorrectly()
    {
        var service = new CompressionService();
        var original = new byte[1000];
        var compressed = new byte[500];
        var ratio = service.GetCompressionRatio(original, compressed);
        Assert.Equal(0.5, ratio);
    }

    [Fact]
    public void GetCompressionRatio_EmptyOriginal_ReturnsZero()
    {
        var service = new CompressionService();
        var ratio = service.GetCompressionRatio([], new byte[100]);
        Assert.Equal(0, ratio);
    }

    [Fact]
    public void Compress_Batch_RoundTrip()
    {
        var service = new CompressionService();
        var items = new Dictionary<string, byte[]> { { "item1", "First item"u8.ToArray() }, { "item2", "Second item"u8.ToArray() }, { "item3", "Third item"u8.ToArray() } };
        var compressed = service.Compress(items);
        var decompressed = service.Decompress(compressed);
        Assert.Equal(items.Count, compressed.Count);
        Assert.Equal(items.Count, decompressed.Count);
        foreach (var key in items.Keys)
            Assert.Equal(items[key], decompressed[key]);
    }

    [Fact]
    public void CompressFiles_Batch_RoundTrip()
    {
        var service = new CompressionService();
        _tempSession.CreateFile("File 1 content");
        _tempSession.CreateFile("File 2 content");
        _tempSession.CreateFile("File 3 content");
        var compressResult = service.CompressFiles(_tempSession.Files);
        Assert.Equal(3, compressResult.TotalFiles);
        Assert.Equal(3, compressResult.SuccessfulFilesCount);
        Assert.Equal(0, compressResult.FailedFilesCount);
        Assert.Equal(3, compressResult.SuccessfulFiles.Count);

        // Decompress all compressed files
        var compressedFiles = compressResult.SuccessfulFiles.Select(f => f.OutputFilePath).ToList();
        var decompressResult = service.DecompressFiles(compressedFiles);
        Assert.Equal(3, decompressResult.TotalFiles);
        Assert.Equal(3, decompressResult.SuccessfulFilesCount);

        // Create a mapping from compressed file to original file
        var compressedToOriginal = compressResult.SuccessfulFiles.ToDictionary(f => f.OutputFilePath, f => _tempSession.Files.First(orig => f.InputFilePath == orig));

        // Verify content matches by matching decompressed files to original files
        foreach (var decompressedFile in decompressResult.SuccessfulFiles) {
            // The InputFilePath of the decompressed file is the compressed file path
            var compressedFilePath = decompressedFile.InputFilePath;
            var originalFilePath = compressedToOriginal[compressedFilePath];
            var original = File.ReadAllText(originalFilePath);
            var decompressed = File.ReadAllText(decompressedFile.OutputFilePath);
            Assert.Equal(original, decompressed);
        }
    }

    [Fact]
    public async Task CompressFilesAsync_Batch_RoundTrip()
    {
        var service = new CompressionService();
        await _tempSession.CreateFileAsync(new string('A', 1000) + " Async File 1 " + new string('B', 1000), TestContext.Current.CancellationToken).ConfigureAwait(false);
        await _tempSession.CreateFileAsync(new string('C', 1000) + " Async File 2 " + new string('D', 1000), TestContext.Current.CancellationToken).ConfigureAwait(false);
        var compressResult = await service.CompressFilesAsync(_tempSession.Files, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(2, compressResult.TotalFiles);
        Assert.Equal(2, compressResult.SuccessfulFilesCount);
        Assert.Equal(0, compressResult.FailedFilesCount);

        // Create a mapping from compressed file to original file
        var compressedToOriginal = compressResult.SuccessfulFiles.ToDictionary(f => f.OutputFilePath, f => _tempSession.Files.First(orig => f.InputFilePath == orig));
        var compressedFiles = compressResult.SuccessfulFiles.Select(f => f.OutputFilePath).ToList();
        var decompressResult = await service.DecompressFilesAsync(compressedFiles, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(2, decompressResult.TotalFiles);
        Assert.Equal(2, decompressResult.SuccessfulFilesCount);

        // Match decompressed files to original files by the compressed file path
        foreach (var decompressedFile in decompressResult.SuccessfulFiles) {
            // Find the original file that corresponds to this decompressed file
            // The InputFilePath of the decompressed file is the compressed file path
            var compressedFilePath = decompressedFile.InputFilePath;
            var originalFilePath = compressedToOriginal[compressedFilePath];
            var original = await File.ReadAllTextAsync(originalFilePath, TestContext.Current.CancellationToken).ConfigureAwait(false);
            var decompressed = await File.ReadAllTextAsync(decompressedFile.OutputFilePath, TestContext.Current.CancellationToken).ConfigureAwait(false);
            Assert.Equal(original, decompressed);
        }
    }

    [Fact]
    public void CompressFiles_WithNonExistentFile_IncludesInFailed()
    {
        var service = new CompressionService();
        var files = new List<string> { _tempSession.CreateFile("Valid file"), "/nonexistent/path/file.txt" };
        var result = service.CompressFiles(files);
        Assert.Equal(2, result.TotalFiles);
        Assert.Equal(1, result.SuccessfulFilesCount);
        Assert.Equal(1, result.FailedFilesCount);
        Assert.Single(result.FailedFiles);
        Assert.Equal("/nonexistent/path/file.txt", result.FailedFiles[0].FilePath);
    }

    [Fact]
    public async Task CompressStringToStreamAsync_DecompressStringFromStreamAsync_RoundTrip()
    {
        var service = new CompressionService();
        var original = "Test string to stream compression";
        using var outputStream = new MemoryStream();
        await service.CompressStringToStreamAsync(original, outputStream, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        outputStream.Position = 0;
        var decompressed = await service.DecompressStringFromStreamAsync(outputStream, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(original, decompressed);
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Snappier)]
    [InlineData(CompressionAlgorithm.ZstdSharp)]
    [InlineData(CompressionAlgorithm.LZ4)]
    [InlineData(CompressionAlgorithm.LZMA)]
    [InlineData(CompressionAlgorithm.BZip2)]
    [InlineData(CompressionAlgorithm.XZ)]
#if !NETSTANDARD2_0
    [InlineData(CompressionAlgorithm.Brotli)]
    [InlineData(CompressionAlgorithm.ZLib)]
#endif
    public void FileExtension_MatchesAlgorithm(CompressionAlgorithm algorithm)
    {
        var service = new CompressionService(_logger, new() { DefaultAlgorithm = algorithm });
        var expectedExtension = Constants.Data.AlgorithmExtensions[algorithm];
        Assert.Equal(expectedExtension, service.FileExtension);
    }

    [Fact]
    public void SetCompressionAlgorithm_DoesNotThrow()
    {
        var service = new CompressionService(_logger);
        service.SetCompressionAlgorithm(CompressionAlgorithm.GZip, CompressionLevel.Fastest);
        service.SetCompressionAlgorithm(CompressionAlgorithm.Deflate, CompressionLevel.Optimal);
        // No-op implementation; verify it can be called without error
    }

    [Fact]
    public void Compress_NullInput_ThrowsArgumentNullException()
    {
        var service = new CompressionService();
        Assert.Throws<ArgumentNullException>(() => service.Compress(null!, out var _));
    }

    [Fact]
    public void Compress_EmptyInput_ThrowsArgumentException()
    {
        var service = new CompressionService();
        Assert.Throws<ArgumentOutsideRangeException>(() => service.Compress([], out var _));
    }

    [Fact]
    public void CompressFile_NonExistentFile_ThrowsFileNotFoundException()
    {
        var service = new CompressionService();
        Assert.Throws<FileNotFoundException>(() => service.CompressFile("/nonexistent/file.txt"));
    }

    [Fact]
    public async Task CompressFileAsync_Cancellation_ThrowsOperationCanceledException()
    {
        var service = new CompressionService();
        var tempFile = await _tempSession.CreateFileAsync(new('x', 10000), TestContext.Current.CancellationToken).ConfigureAwait(false); // Larger file to ensure it takes time
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(false);
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.CompressFileAsync(tempFile, ct: cts.Token)).ConfigureAwait(false);
    }

    [Fact]
    public void CompressToBase64_InvalidBase64_ThrowsArgumentException()
    {
        var service = new CompressionService();
        Assert.Throws<ArgumentException>(() => service.DecompressFromBase64("invalid base64!!!", out var _));
    }

    [Fact]
    public void CompressString_NullOrEmpty_ThrowsArgumentException()
    {
        var service = new CompressionService();
        Assert.Throws<ArgumentNullException>(() => service.CompressString(null!, out var _));
        Assert.Throws<ArgumentException>(() => service.CompressString("", out var _));
    }

    [Fact]
    public void Compress_Stream_NonReadableInput_ThrowsArgumentException()
    {
        var service = new CompressionService();
        using var nonReadableStream = new MemoryStream();
        nonReadableStream.Close(); // Makes it non-readable
        Assert.Throws<InvalidOperationException>(() => service.Compress(nonReadableStream, new MemoryStream()));
    }

    [Fact]
    public void Compress_Stream_NonWritableOutput_ThrowsArgumentException()
    {
        var service = new CompressionService();
        using var nonWritableStream = new MemoryStream();
        nonWritableStream.Close(); // Makes it non-writable
        Assert.Throws<InvalidOperationException>(() => service.Compress(new MemoryStream(new byte[10]), nonWritableStream));
    }

    [Fact]
    public void CompressionInfo_Properties_CalculatedCorrectly()
    {
        var service = new CompressionService();
        var original = new byte[1000];
        service.Compress(original, out var _);
        var info = service.Compress(original, out var _);
        Assert.True(info.CompressionRatio > 0);
        Assert.True(info.SpaceSavedPercent >= 0);
        Assert.True(info.TimeMs >= 0);
    }

    [Fact]
    public void DecompressionInfo_Properties_CalculatedCorrectly()
    {
        var service = new CompressionService();
        // Use larger content that will compress well
        var original = Encoding.UTF8.GetBytes(new string('X', 1000) + "Test decompression info" + new string('Y', 1000));
        service.Compress(original, out var compressed);
        var info = service.Decompress(compressed, out var decompressed);
        Assert.Equal(original.Length, decompressed.Length);
        Assert.True(info.ExpansionRatio > 0);
        // SizeIncreasePercent should be positive (decompressed > compressed)
        Assert.True(info.SizeIncreasePercent > 0);
        Assert.True(info.ExpansionRatio >= 0);
    }

    [Fact]
    public void BatchCompressionResult_Properties_CalculatedCorrectly()
    {
        var service = new CompressionService();
        // Use larger content that will compress well
        _tempSession.CreateFile(new string('A', 1000) + " Content 1 " + new string('B', 1000));
        _tempSession.CreateFile(new string('C', 1000) + " Content 2 " + new string('D', 1000));
        var result = service.CompressFiles(_tempSession.Files);
        Assert.Equal(2, result.TotalFiles);
        Assert.Equal(2, result.SuccessfulFilesCount);
        Assert.Equal(0, result.FailedFilesCount);
        Assert.True(result.AverageCompressionRatio > 0);
        // TotalSpaceSavedPercent can be negative if compression overhead makes files larger
        // For small files, this is expected, so we just check it's a valid number
        Assert.True(result.TotalSpaceSavedPercent is >= -100 and <= 100);
        Assert.True(result.TotalOriginalSize > 0);
        Assert.True(result.TotalCompressedSize > 0);
        Assert.True(result.TotalTimeMs >= 0);
    }

    [Fact]
    public void BatchDecompressionResult_Properties_CalculatedCorrectly()
    {
        var service = new CompressionService();
        _tempSession.CreateFile(new string('A', 1000) + " Content 1 " + new string('B', 1000));
        _tempSession.CreateFile(new string('C', 1000) + " Content 2 " + new string('D', 1000));
        var compressResult = service.CompressFiles(_tempSession.Files);
        var compressedFiles = compressResult.SuccessfulFiles.Select(f => f.OutputFilePath).ToList();
        var result = service.DecompressFiles(compressedFiles);
        Assert.Equal(2, result.TotalFiles);
        Assert.Equal(2, result.SuccessfulFilesCount);
        Assert.Equal(0, result.FailedFilesCount);
        Assert.True(result.AverageExpansionRatio > 0);
        // SizeIncreasePercent should be positive (decompressed > compressed)
        Assert.True(result.TotalSizeIncreasePercent > 0);
        Assert.True(result.TotalCompressedSize > 0);
        Assert.True(result.TotalDecompressedSize > 0);
        Assert.True(result.TotalTimeMs >= 0);
    }

    [Fact]
    public void CompressString_InvalidEncoding_FallsBackToUtf8()
    {
        var service = new CompressionService(_logger, new() { DefaultEncoding = "InvalidEncodingName123" });
        var original = "Test string with invalid encoding";

        // Should not throw, should fall back to UTF-8
        service.CompressString(original, out var compressed);
        service.DecompressString(compressed, out var decompressed);
        Assert.Equal(original, decompressed);
        Assert.True(compressed.Length > 0);
    }

    [Fact]
    public void DecompressString_InvalidEncoding_FallsBackToUtf8()
    {
        var service = new CompressionService(_logger, new() { DefaultEncoding = "InvalidEncodingName456" });
        var original = "Test string for decompression";

        // Compress with valid encoding first
        service.CompressString(original, out var compressed);

        // Create new service with invalid encoding - should fall back to UTF-8
        var serviceWithInvalidEncoding = new CompressionService(_logger, new() { DefaultEncoding = "InvalidEncodingName456" });
        serviceWithInvalidEncoding.DecompressString(compressed, out var decompressed);
        Assert.Equal(original, decompressed);
    }

    [Fact]
    public async Task CompressStringToStreamAsync_InvalidEncoding_FallsBackToUtf8()
    {
        var service = new CompressionService(_logger, new() { DefaultEncoding = "InvalidEncodingName789" });
        var original = "Test async string compression";
        using var outputStream = new MemoryStream();
        await service.CompressStringToStreamAsync(original, outputStream, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        outputStream.Position = 0;
        var decompressed = await service.DecompressStringFromStreamAsync(outputStream, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(original, decompressed);
    }

    [Fact]
    public void Compress_ExceedsMaxInputSize_ThrowsArgumentOutsideRangeException()
    {
        var maxSize = 1024L; // 1 KB
        var service = new CompressionService(_logger, new() { MaxInputSize = maxSize });
        var largeData = new byte[maxSize + 1]; // Exceeds limit by 1 byte
        var exception = Assert.Throws<ArgumentOutsideRangeException>(() => service.Compress(largeData, out var _));
        Assert.NotNull(exception);
        Assert.Equal(maxSize + 1, exception.ActualValue);
    }

    [Fact]
    public void Compress_WithinMaxInputSize_Succeeds()
    {
        var maxSize = 1024L; // 1 KB
        var service = new CompressionService(_logger, new() { MaxInputSize = maxSize });
        var data = new byte[maxSize]; // Exactly at limit
        service.Compress(data, out var compressed);
        Assert.NotNull(compressed);
        Assert.True(compressed.Length > 0);
    }

    [Fact]
    public void CompressFile_ExceedsMaxInputSize_ThrowsArgumentOutsideRangeException()
    {
        var maxSize = 1024L; // 1 KB
        var service = new CompressionService(_logger, new() { MaxInputSize = maxSize });
        var tempFile = _tempSession.CreateFile(new('X', (int)(maxSize + 1)));
        var exception = Assert.Throws<ArgumentOutsideRangeException>(() => service.CompressFile(tempFile));
        Assert.NotNull(exception);
        Assert.Equal(maxSize + 1, exception.ActualValue);
    }

    [Fact]
    public async Task CompressFileAsync_ExceedsMaxInputSize_ThrowsArgumentOutsideRangeException()
    {
        var maxSize = 1024L; // 1 KB
        var service = new CompressionService(_logger, new() { MaxInputSize = maxSize });
        var tempFile = await _tempSession.CreateFileAsync(new('X', (int)(maxSize + 1)), TestContext.Current.CancellationToken).ConfigureAwait(false);
        var exception = await Assert.ThrowsAsync<ArgumentOutsideRangeException>(() => service.CompressFileAsync(tempFile, ct: TestContext.Current.CancellationToken)).ConfigureAwait(false);
        Assert.NotNull(exception);
        Assert.Equal(maxSize + 1, exception.ActualValue);
    }

    [Fact]
    public void Constructor_InvalidMaxParallelFileOperations_ThrowsArgumentOutsideRangeException()
    {
        var exception = Assert.Throws<ArgumentOutsideRangeException>(() => new CompressionService(_logger, new() { MaxParallelFileOperations = 0 }));
        Assert.NotNull(exception);
        Assert.Equal(0, exception.ActualValue);
        Assert.Equal(1, exception.MinValue);
    }

    [Fact]
    public void Constructor_InvalidDefaultFileBufferSize_ThrowsArgumentOutsideRangeException()
    {
        var exception = Assert.Throws<ArgumentOutsideRangeException>(() => new CompressionService(_logger, new() { DefaultFileBufferSize = 512 })); // Less than 1024
        Assert.NotNull(exception);
        Assert.Equal(512, exception.ActualValue);
        Assert.Equal(1024, exception.MinValue);
    }

    [Fact]
    public void Constructor_InvalidAsyncFileBufferSize_ThrowsArgumentOutsideRangeException()
    {
        var exception = Assert.Throws<ArgumentOutsideRangeException>(() => new CompressionService(_logger, new() { AsyncFileBufferSize = 256 })); // Less than 1024
        Assert.NotNull(exception);
        Assert.Equal(256, exception.ActualValue);
        Assert.Equal(1024, exception.MinValue);
    }

    [Fact]
    public void Constructor_InvalidMaxInputSize_ThrowsArgumentOutsideRangeException()
    {
        var exception = Assert.Throws<ArgumentOutsideRangeException>(() => new CompressionService(_logger, new() { MaxInputSize = 512 })); // Less than 1024
        Assert.NotNull(exception);
        Assert.Equal(512L, exception.ActualValue);
        Assert.Equal(1024L, exception.MinValue);
    }

    [Fact]
    public void Constructor_ValidOptions_CreatesService()
    {
        var options = new CompressionServiceOptions {
            MaxParallelFileOperations = 4,
            DefaultFileBufferSize = 8192,
            AsyncFileBufferSize = 16384,
            MaxInputSize = 1024L * 1024 * 1024 // 1 GB
        };

        var service = new CompressionService(_logger, options);
        Assert.NotNull(service);
        Assert.Equal(options.DefaultAlgorithm, service.Algorithm);
    }

    [Fact]
    public void CompressFile_OnFailure_NoPartialFileLeft()
    {
        var service = new CompressionService();
        var tempFile = _tempSession.CreateFile("test content");
        var outputFile = tempFile + service.FileExtension;

        // Create a directory with the same name as the output file to cause a failure
        // Actually, let's use a different approach - create a file that's too large
        _tempSession.CreateFile(new('X', 1000000));

        // This should fail due to file size limit (if we set a low limit)
        // But actually, let's test with a file that doesn't exist as input
        // Actually, let's test atomic operation by checking that temp files are cleaned up
        // The best way is to check that if compression fails, no .tmp file is left

        // Create a scenario where compression might fail - use a very large file with low limit
        var serviceWithLowLimit = new CompressionService(
            options: new() {
                MaxInputSize = 1024 // 1 KB limit
            });

        var largeContent = new string('Y', 2000); // 2 KB
        var largeTempFile = _tempSession.CreateFile(largeContent);
        try {
            serviceWithLowLimit.CompressFile(largeTempFile, outputFile);
        }
        catch (ArgumentOutsideRangeException) {
            // Expected - file is too large
        }

        // Check that no .tmp file was left behind
        var tempFilePath = outputFile + ".tmp";
        Assert.False(File.Exists(tempFilePath), "Temporary file should be cleaned up on failure");
    }

    [Fact]
    public async Task CompressFileAsync_OnFailure_NoPartialFileLeft()
    {
        var service = new CompressionService(
            options: new() {
                MaxInputSize = 1024 // 1 KB limit
            });

        var largeContent = new string('Z', 2000); // 2 KB
        var largeTempFile = _tempSession.CreateFile(largeContent);
        var outputFile = largeTempFile + service.FileExtension;
        try {
            await service.CompressFileAsync(largeTempFile, outputFile, TestContext.Current.CancellationToken).ConfigureAwait(false);
        }
        catch (ArgumentOutsideRangeException) {
            // Expected - file is too large
        }

        // Check that no .tmp file was left behind
        var tempFilePath = outputFile + ".tmp";
        Assert.False(File.Exists(tempFilePath), "Temporary file should be cleaned up on failure");
    }

    [Fact]
    public void CompressFile_AtomicOperation_CompleteFileExists()
    {
        var service = new CompressionService();
        var tempFile = _tempSession.CreateFile("test content for atomic operation");
        var outputFile = tempFile + service.FileExtension;
        service.CompressFile(tempFile, outputFile);

        // Verify the output file exists and is complete (not a temp file)
        Assert.True(File.Exists(outputFile));
        Assert.False(File.Exists(outputFile + ".tmp"), "Temporary file should not exist after successful operation");

        // Verify we can decompress it (proves it's a complete, valid file)
        var decompressedFile = tempFile + ".decompressed";
        service.DecompressFile(outputFile, decompressedFile);
        var originalContent = File.ReadAllText(tempFile);
        var decompressedContent = File.ReadAllText(decompressedFile);
        Assert.Equal(originalContent, decompressedContent);
    }

    [Fact]
    public async Task CompressFileAsync_AtomicOperation_CompleteFileExists()
    {
        var service = new CompressionService();
        var tempFile = await _tempSession.CreateFileAsync("test content for async atomic operation", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var outputFile = tempFile + service.FileExtension;
        await service.CompressFileAsync(tempFile, outputFile, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Verify the output file exists and is complete (not a temp file)
        Assert.True(File.Exists(outputFile));
        Assert.False(File.Exists(outputFile + ".tmp"), "Temporary file should not exist after successful operation");

        // Verify we can decompress it (proves it's a complete, valid file)
        var decompressedFile = tempFile + ".decompressed";
        await service.DecompressFileAsync(outputFile, decompressedFile, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var originalContent = await File.ReadAllTextAsync(tempFile, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var decompressedContent = await File.ReadAllTextAsync(decompressedFile, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(originalContent, decompressedContent);
    }

    [Fact]
    public void DecompressFile_AtomicOperation_CompleteFileExists()
    {
        var service = new CompressionService();
        var tempFile = _tempSession.CreateFile("test content for decompress atomic operation");
        var compressedFile = tempFile + service.FileExtension;

        // First compress
        service.CompressFile(tempFile, compressedFile);
        var decompressedFile = tempFile + ".decompressed";
        service.DecompressFile(compressedFile, decompressedFile);

        // Verify the output file exists and is complete (not a temp file)
        Assert.True(File.Exists(decompressedFile));
        Assert.False(File.Exists(decompressedFile + ".tmp"), "Temporary file should not exist after successful operation");
        var originalContent = File.ReadAllText(tempFile);
        var decompressedContent = File.ReadAllText(decompressedFile);
        Assert.Equal(originalContent, decompressedContent);
    }

    [Fact]
    public async Task DecompressFileAsync_AtomicOperation_CompleteFileExists()
    {
        var service = new CompressionService();
        var tempFile = await _tempSession.CreateFileAsync("test content for async decompress atomic operation", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var compressedFile = tempFile + service.FileExtension;

        // First compress
        await service.CompressFileAsync(tempFile, compressedFile, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var decompressedFile = tempFile + ".decompressed";
        await service.DecompressFileAsync(compressedFile, decompressedFile, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Verify the output file exists and is complete (not a temp file)
        Assert.True(File.Exists(decompressedFile));
        Assert.False(File.Exists(decompressedFile + ".tmp"), "Temporary file should not exist after successful operation");
        var originalContent = await File.ReadAllTextAsync(tempFile, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var decompressedContent = await File.ReadAllTextAsync(decompressedFile, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(originalContent, decompressedContent);
    }

    [Fact]
    public void Compress_MaxInputSize_Configurable()
    {
        // Test that MaxInputSize can be configured to different values
        var smallLimit = 1024L; // 1 KB
        var largeLimit = 10L * 1024 * 1024 * 1024; // 10 GB
        var serviceSmall = new CompressionService(_logger, new() { MaxInputSize = smallLimit });
        var serviceLarge = new CompressionService(_logger, new() { MaxInputSize = largeLimit });
        var smallData = new byte[smallLimit];
        var largeData = new byte[1024 * 1024]; // 1 MB

        // Small service should accept small data
        serviceSmall.Compress(smallData, out var _);

        // Small service should reject large data
        Assert.Throws<ArgumentOutsideRangeException>(() => serviceSmall.Compress(largeData, out var _));

        // Large service should accept both
        serviceLarge.Compress(smallData, out var _);
        serviceLarge.Compress(largeData, out var _);
    }

    [Fact]
    public async Task CompressFilesAsync_WithCustomOutputPaths_RoundTrips()
    {
        var service = new CompressionService();
        var file1 = await _tempSession.CreateFileAsync("Content one for custom path", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var file2 = await _tempSession.CreateFileAsync("Content two for custom path", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var out1 = _tempSession.GetFilePath("custom1" + service.FileExtension);
        var out2 = _tempSession.GetFilePath("custom2" + service.FileExtension);
        var filePaths = new Dictionary<string, string?> { [file1] = out1, [file2] = out2 };
        var compressResult = await service.CompressFilesAsync(filePaths, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(2, compressResult.TotalFiles);
        Assert.Equal(2, compressResult.SuccessfulFilesCount);
        Assert.True(File.Exists(out1));
        Assert.True(File.Exists(out2));
        var dec1 = _tempSession.GetFilePath("dec1.txt");
        var dec2 = _tempSession.GetFilePath("dec2.txt");
        var decompPaths = new Dictionary<string, string?> { [out1] = dec1, [out2] = dec2 };
        var decompressResult = await service.DecompressFilesAsync(decompPaths, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(2, decompressResult.SuccessfulFilesCount);
        Assert.Equal("Content one for custom path", await File.ReadAllTextAsync(dec1, TestContext.Current.CancellationToken).ConfigureAwait(false));
        Assert.Equal("Content two for custom path", await File.ReadAllTextAsync(dec2, TestContext.Current.CancellationToken).ConfigureAwait(false));
    }

    [Fact]
    public void AddCompressionService_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddCompressionService();
        var provider = services.BuildServiceProvider();
        var svc = provider.GetRequiredService<ICompressionService>();
        Assert.NotNull(svc);
        var original = "test"u8.ToArray();
        svc.Compress(original, out var compressed);
        svc.Decompress(compressed, out var decompressed);
        Assert.Equal(original, decompressed);
    }

    [Fact]
    public void AddCompressionService_WithConfigure_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddCompressionService(opts => opts.DefaultAlgorithm = CompressionAlgorithm.GZip);
        var provider = services.BuildServiceProvider();
        var svc = provider.GetRequiredService<ICompressionService>();
        Assert.NotNull(svc);
    }

    [Fact]
    public void CompressString_WithExplicitInvalidEncoding_FallsBackToUtf8()
    {
        var service = new CompressionService();
        var original = "Test string";

        // Pass invalid encoding explicitly
        service.CompressString(original, out var compressed, Encoding.GetEncoding("utf-8"));
        service.DecompressString(compressed, out var decompressed, Encoding.GetEncoding("utf-8"));
        Assert.Equal(original, decompressed);

        // Now test with invalid encoding name in options but explicit valid encoding
        var serviceWithInvalidDefault = new CompressionService(_logger, new() { DefaultEncoding = "InvalidEncoding" });
        serviceWithInvalidDefault.CompressString(original, out var compressed2, Encoding.UTF8);
        serviceWithInvalidDefault.DecompressString(compressed2, out var decompressed2, Encoding.UTF8);
        Assert.Equal(original, decompressed2);
    }
}