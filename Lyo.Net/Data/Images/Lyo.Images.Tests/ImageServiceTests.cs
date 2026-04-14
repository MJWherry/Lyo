using Lyo.Common.Enums;
using Lyo.Exceptions.Models;
using Lyo.Images.Models;
using Lyo.Images.Skia;
using Lyo.Testing;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using ResizeMode = Lyo.Images.Models.ResizeMode;

namespace Lyo.Images.Tests;

public class ImageServiceTests
{
    private readonly ILogger<SkiaImageService> _logger;

    public ImageServiceTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _logger = loggerFactory.CreateLogger<SkiaImageService>();
    }

    private SkiaImageService CreateService(ImageServiceOptions? options = null)
    {
        options ??= new() {
            DefaultQuality = 90,
            MaxWidth = 10000,
            MaxHeight = 10000,
            MaxFileSizeBytes = 100 * 1024 * 1024,
            EnableMetrics = false
        };

        return new(options, _logger);
    }

    private static byte[] CreateTestImage(int width = 100, int height = 100)
    {
        using var image = new Image<Rgba32>(width, height, Color.Red);
        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        return ms.ToArray();
    }

    /// <summary>Creates an image with multiple distinct colors (e.g., stripes or grid).</summary>
    private static byte[] CreateMultiColorTestImage(int width = 100, int height = 100, int colorBlockSize = 25)
    {
        var colors = new[] { Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Magenta, Color.Cyan, Color.Orange, Color.Purple, Color.White, Color.Black };
        using var image = new Image<Rgba32>(width, height);
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++) {
            var colorIndex = (x / colorBlockSize + y / colorBlockSize) % colors.Length;
            image[x, y] = colors[colorIndex];
        }

        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        return ms.ToArray();
    }

    private static byte[] CreateWeightedColorImage()
    {
        using var image = new Image<Rgba32>(100, 100);
        for (var y = 0; y < 100; y++)
        for (var x = 0; x < 100; x++) {
            if (x < 60)
                image[x, y] = Color.Red;
            else if (x < 85)
                image[x, y] = Color.Blue;
            else
                image[x, y] = Color.Green;
        }

        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        return ms.ToArray();
    }

    private static byte[] CreateMostlyTransparentImage()
    {
        using var image = new Image<Rgba32>(100, 100, new(0, 0, 0, 0));
        for (var y = 0; y < 20; y++)
        for (var x = 0; x < 20; x++)
            image[x, y] = Color.Red;

        for (var y = 0; y < 10; y++)
        for (var x = 20; x < 30; x++)
            image[x, y] = Color.Blue;

        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        return ms.ToArray();
    }

    [Fact]
    public async Task ResizeAsync_WithValidImage_ReturnsResizedImage()
    {
        var service = CreateService();
        var inputBytes = CreateTestImage(200, 200);
        using var inputStream = new MemoryStream(inputBytes);
        using var outputStream = new MemoryStream();
        var result = await service.ResizeAsync(inputStream, outputStream, 100, 100, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        Assert.True(outputStream.Length > 0);
        outputStream.Position = 0;
        using var resultImage = await Image.LoadAsync(outputStream, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(resultImage.Width <= 100);
        Assert.True(resultImage.Height <= 100);
    }

    [Fact]
    public async Task ResizeAsync_WithDifferentResizeModes_ReturnsResizedImage()
    {
        var service = CreateService();
        var modes = new[] { ResizeMode.Max, ResizeMode.Crop, ResizeMode.Pad, ResizeMode.BoxPad, ResizeMode.Stretch };
        foreach (var mode in modes) {
            var inputBytes = CreateTestImage(200, 200);
            using var inputStream = new MemoryStream(inputBytes);
            using var outputStream = new MemoryStream();
            var result = await service.ResizeAsync(inputStream, outputStream, 100, 100, mode, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
            Assert.True(result.IsSuccess);
            Assert.True(outputStream.Length > 0);
        }
    }

    [Fact]
    public async Task CropAsync_WithValidImage_ReturnsCroppedImage()
    {
        var service = CreateService();
        var inputBytes = CreateTestImage(200, 200);
        using var inputStream = new MemoryStream(inputBytes);
        using var outputStream = new MemoryStream();
        var result = await service.CropAsync(inputStream, outputStream, 50, 50, 100, 100, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        Assert.True(outputStream.Length > 0);
        outputStream.Position = 0;
        using var resultImage = await Image.LoadAsync(outputStream, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(100, resultImage.Width);
        Assert.Equal(100, resultImage.Height);
    }

    [Fact]
    public async Task RotateAsync_WithValidImage_ReturnsRotatedImage()
    {
        var service = CreateService();
        var inputBytes = CreateTestImage(100, 200);
        using var inputStream = new MemoryStream(inputBytes);
        using var outputStream = new MemoryStream();
        var result = await service.RotateAsync(inputStream, outputStream, 90, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        Assert.True(outputStream.Length > 0);
        outputStream.Position = 0;
        using var resultImage = await Image.LoadAsync(outputStream, TestContext.Current.CancellationToken).ConfigureAwait(false);
        // SkiaImageService rotates within the same canvas dimensions (no expansion), so dimensions stay 100x200
        Assert.Equal(100, resultImage.Width);
        Assert.Equal(200, resultImage.Height);
    }

    [Fact]
    public async Task WatermarkAsync_WithValidImage_ReturnsWatermarkedImage()
    {
        var service = CreateService();
        var inputBytes = CreateTestImage(200, 200);
        using var inputStream = new MemoryStream(inputBytes);
        using var outputStream = new MemoryStream();
        var watermarkOptions = new WatermarkOptions {
            FontSize = 24,
            FontFamily = "Arial",
            TextColor = "#FFFFFF",
            Position = WatermarkPosition.BottomRight,
            Opacity = 0.7f,
            Padding = 10
        };

        var result = await service.WatermarkAsync(inputStream, outputStream, "Test Watermark", watermarkOptions, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        Assert.True(outputStream.Length > 0);
    }

    [Fact]
    public async Task ConvertFormatAsync_WithValidImage_ReturnsConvertedImage()
    {
        var service = CreateService();
        var inputBytes = CreateTestImage();
        using var inputStream = new MemoryStream(inputBytes);
        using var outputStream = new MemoryStream();
        var result = await service.ConvertFormatAsync(inputStream, outputStream, ImageFormat.Jpeg, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        Assert.True(outputStream.Length > 0);
    }

    [Fact]
    public async Task GenerateThumbnailAsync_WithValidImage_ReturnsThumbnail()
    {
        var service = CreateService();
        var inputBytes = CreateTestImage(500, 500);
        using var inputStream = new MemoryStream(inputBytes);
        var result = await service.GenerateThumbnailAsync(inputStream, 200, 200, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(result.Data!.Length > 0);
        using var thumbnailStream = new MemoryStream(result.Data);
        using var thumbnailImage = await Image.LoadAsync(thumbnailStream, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(thumbnailImage.Width <= 200);
        Assert.True(thumbnailImage.Height <= 200);
    }

    [Fact]
    public async Task GetPaletteAsync_WithValidImage_ReturnsPalette()
    {
        var service = CreateService();
        var inputBytes = CreateTestImage(200, 200);
        using var inputStream = new MemoryStream(inputBytes);
        var result = await service.GetPaletteAsync(inputStream, 10, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data!.Colors);
        Assert.True(result.Data.Colors.Count <= 10);
        Assert.All(result.Data.Colors, c => Assert.Matches(@"^#[0-9A-Fa-f]{6}$", c));
    }

    [Fact]
    public async Task GetPaletteAsync_WithMultiColorImage_ReturnsMultipleColors()
    {
        var service = CreateService();
        var inputBytes = CreateMultiColorTestImage(200, 200);
        using var inputStream = new MemoryStream(inputBytes);
        var result = await service.GetPaletteAsync(inputStream, 10, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(result.Data!.Colors.Count >= 2, "Multi-color image should produce at least 2 palette colors");
        Assert.True(result.Data.Colors.Count <= 10);
        Assert.All(result.Data.Colors, c => Assert.Matches(@"^#[0-9A-Fa-f]{6}$", c));
        Assert.Equal(result.Data.Colors.Count, result.Data.Colors.Distinct().Count()); // No duplicates
    }

    [Fact]
    public async Task GetPaletteAsync_WithColorCountLimit_RespectsRequestedCount()
    {
        var service = CreateService();
        var inputBytes = CreateMultiColorTestImage(200, 200);
        using var inputStream = new MemoryStream(inputBytes);
        var result = await service.GetPaletteAsync(inputStream, 5, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(result.Data!.Colors.Count <= 5);
    }

    [Fact]
    public async Task GetPaletteAsync_WithLargeColorCount_ReturnsUpToRequestedColors()
    {
        var service = CreateService();
        var inputBytes = CreateMultiColorTestImage(200, 200, 20); // 10 color blocks
        using var inputStream = new MemoryStream(inputBytes);
        var result = await service.GetPaletteAsync(inputStream, 20, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(result.Data!.Colors.Count >= 2);
        Assert.True(result.Data.Colors.Count <= 20);
    }

    [Fact]
    public async Task GetPaletteAsync_ReturnsColorsOrderedByUsage()
    {
        var service = CreateService();
        var inputBytes = CreateWeightedColorImage();
        using var inputStream = new MemoryStream(inputBytes);
        var result = await service.GetPaletteAsync(inputStream, 3, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data!.Colors.Count);
        Assert.Equal("#FF0000", result.Data.Colors[0]);
        Assert.Equal("#0000FF", result.Data.Colors[1]);
        // 5-bit bucket for G=128 expands via Expand5BitChannelTo8(16) => 0x84, not 0x80 (see SkiaImageService.ExtractPalette)
        Assert.Equal("#008400", result.Data.Colors[2]);
    }

    [Fact]
    public async Task GetPaletteAsync_IgnoresTransparentPixels()
    {
        var service = CreateService();
        var inputBytes = CreateMostlyTransparentImage();
        using var inputStream = new MemoryStream(inputBytes);
        var result = await service.GetPaletteAsync(inputStream, 4, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data!.Colors);
        Assert.Equal("#FF0000", result.Data.Colors[0]);
        Assert.Equal("#0000FF", result.Data.Colors[1]);
        Assert.DoesNotContain("#000000", result.Data.Colors);
    }

    [Fact]
    public async Task GetPaletteAsync_WhenIgnoreTransparentDisabled_IncludesTransparentColorBucket()
    {
        var service = CreateService(
            new() {
                DefaultQuality = 90,
                MaxWidth = 10000,
                MaxHeight = 10000,
                MaxFileSizeBytes = 100 * 1024 * 1024,
                EnableMetrics = false,
                IgnoreTransparentPixelsInPalette = false,
                PaletteAlphaCutoff = 16
            });

        var inputBytes = CreateMostlyTransparentImage();
        using var inputStream = new MemoryStream(inputBytes);
        var result = await service.GetPaletteAsync(inputStream, 3, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("#000000", result.Data!.Colors[0]);
    }

    [Fact]
    public async Task GetMetadataAsync_WithValidImage_ReturnsMetadata()
    {
        var service = CreateService();
        var inputBytes = CreateTestImage(200, 150);
        using var inputStream = new MemoryStream(inputBytes);
        var result = await service.GetMetadataAsync(inputStream, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(200, result.Data!.Width);
        Assert.Equal(150, result.Data.Height);
        Assert.NotNull(result.Data.FileSizeBytes);
        Assert.True(result.Data.FileSizeBytes > 0);
    }

    [Fact]
    public async Task GetMetadataFromFileAsync_WithValidImage_ReturnsMetadata()
    {
        var service = CreateService();
        var inputBytes = CreateTestImage(200, 150);
        var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
        try {
            await File.WriteAllBytesAsync(filePath, inputBytes, TestContext.Current.CancellationToken).ConfigureAwait(false);
            var result = await service.GetMetadataFromFileAsync(filePath, TestContext.Current.CancellationToken).ConfigureAwait(false);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(200, result.Data!.Width);
            Assert.Equal(150, result.Data.Height);
            Assert.NotNull(result.Data.FileSizeBytes);
        }
        finally {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Fact]
    public async Task CompressAsync_WithValidImage_ReturnsCompressedImage()
    {
        var service = CreateService();
        var inputBytes = CreateTestImage(500, 500);
        using var inputStream = new MemoryStream(inputBytes);
        using var outputStream = new MemoryStream();
        var result = await service.CompressAsync(inputStream, outputStream, 80, ImageFormat.Jpeg, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        Assert.True(outputStream.Length > 0);
        // Compressed image should generally be smaller (though not guaranteed)
        Assert.True(outputStream.Length <= inputBytes.Length * 1.1); // Allow 10% tolerance
    }

    [Fact]
    public async Task ResizeFileAsync_WithValidImage_CreatesResizedFile()
    {
        var service = CreateService();
        var inputBytes = CreateTestImage(200, 200);
        var inputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
        var outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
        try {
            await File.WriteAllBytesAsync(inputPath, inputBytes, TestContext.Current.CancellationToken).ConfigureAwait(false);
            var result = await service.ResizeFileAsync(inputPath, outputPath, 100, 100, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
            Assert.True(result.IsSuccess);
            Assert.True(result.Data);
            Assert.True(File.Exists(outputPath));
            var fileInfo = new FileInfo(outputPath);
            Assert.True(fileInfo.Length > 0);
        }
        finally {
            if (File.Exists(inputPath))
                File.Delete(inputPath);

            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ProcessBatchAsync_WithMultipleRequests_ReturnsResults()
    {
        var service = CreateService();
        var inputBytes = CreateTestImage(200, 200);
        var requests = new[] {
            new ImageProcessRequest {
                InputStream = new MemoryStream(inputBytes),
                OutputStream = new MemoryStream(),
                Operation = new ResizeOperation { Width = 100, Height = 100, Mode = ResizeMode.Max },
                TargetFormat = ImageFormat.Png
            },
            new ImageProcessRequest {
                InputStream = new MemoryStream(inputBytes),
                OutputStream = new MemoryStream(),
                Operation = new CropOperation {
                    X = 50,
                    Y = 50,
                    Width = 100,
                    Height = 100
                },
                TargetFormat = ImageFormat.Png
            }
        };

        var result = await service.ProcessBatchAsync(requests, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.NotNull(result.Results);
        Assert.Equal(2, result.Results.Count);
        Assert.All(result.Results, r => Assert.True(r.IsSuccess));
    }

    [Fact]
    public async Task ResizeAsync_WithInvalidDimensions_ThrowsArgumentOutsideRangeException()
    {
        var service = CreateService();
        var inputBytes = CreateTestImage();
        using var inputStream = new MemoryStream(inputBytes);
        using var outputStream = new MemoryStream();
        await Assert.ThrowsAsync<ArgumentOutsideRangeException>(async () => {
            await service.ResizeAsync(inputStream, outputStream, 0, 100, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        });
    }

    [Fact]
    public async Task CropAsync_WithInvalidCoordinates_ThrowsArgumentOutsideRangeException()
    {
        var service = CreateService();
        var inputBytes = CreateTestImage();
        using var inputStream = new MemoryStream(inputBytes);
        using var outputStream = new MemoryStream();
        await Assert.ThrowsAsync<ArgumentOutsideRangeException>(async () => {
            await service.CropAsync(inputStream, outputStream, -1, 0, 100, 100, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        });
    }

    [Fact]
    public async Task ResizeAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        var service = CreateService();
        var inputBytes = CreateTestImage(200, 200);
        using var inputStream = new MemoryStream(inputBytes);
        using var outputStream = new MemoryStream();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => {
            await service.ResizeAsync(inputStream, outputStream, 100, 100, ct: cts.Token).ConfigureAwait(false);
        });
    }

    [Fact]
    public async Task ResizeAsync_WithQuality_AppliesQuality()
    {
        var service = CreateService();
        var inputBytes = CreateTestImage(200, 200);
        using var inputStream = new MemoryStream(inputBytes);
        using var outputStream = new MemoryStream();
        var result = await service.ResizeAsync(inputStream, outputStream, 100, 100, ResizeMode.Max, ImageFormat.Jpeg, 50, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        Assert.True(outputStream.Length > 0);
    }
}