using Lyo.Exceptions.Models;
using Lyo.Metrics;
using Lyo.QRCode.Models;
using Lyo.QRCode.QRCoder;
using Lyo.Testing;
using Microsoft.Extensions.Logging;

namespace Lyo.QRCode.Tests;

public class QRCodeServiceTests
{
    private readonly ILogger<QRCoderQRCodeService> _logger;

    public QRCodeServiceTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _logger = loggerFactory.CreateLogger<QRCoderQRCodeService>();
    }

    private QRCoderQRCodeService CreateService(QRCodeServiceOptions? options = null, IMetrics? metrics = null)
    {
        options ??= new() {
            DefaultFormat = QRCodeFormat.Png,
            DefaultSize = 256,
            DefaultErrorCorrectionLevel = QRCodeErrorCorrectionLevel.Medium,
            EnableMetrics = false
        };

        return new(options, _logger, metrics);
    }

    [Fact]
    public async Task GenerateAsync_WithValidData_ReturnsSuccess()
    {
        var service = CreateService();
        var result = await service.GenerateAsync("https://example.com", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        if (result is QRCodeResult qrResult) {
            Assert.NotNull(qrResult.ImageBytes);
            Assert.True(qrResult.ImageBytes!.Length > 0);
            Assert.Equal(QRCodeFormat.Png, qrResult.Format);
        }
        else
            Assert.Fail("Result should be QRCodeResult");
    }

    [Fact]
    public async Task GenerateAsync_WithCustomOptions_ReturnsSuccess()
    {
        var service = CreateService();
        var options = new QRCodeOptions {
            Format = QRCodeFormat.Png,
            Size = 512,
            ErrorCorrectionLevel = QRCodeErrorCorrectionLevel.High,
            DarkColor = "#000000",
            LightColor = "#FFFFFF"
        };

        var result = await service.GenerateAsync("https://example.com", options, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        if (result is QRCodeResult qrResult) {
            Assert.NotNull(qrResult.ImageBytes);
            Assert.Equal(512, qrResult.Size);
            Assert.Equal(QRCodeErrorCorrectionLevel.High, options.ErrorCorrectionLevel);
        }
        else
            Assert.Fail("Result should be QRCodeResult");
    }

    [Fact]
    public async Task GenerateAsync_WithBuilder_ReturnsSuccess()
    {
        var service = CreateService();
        var builder = QRCodeBuilder.New().WithData("https://example.com").WithFormat(QRCodeFormat.Png).WithSize(256).WithErrorCorrectionLevel(QRCodeErrorCorrectionLevel.Medium);
        var result = await service.GenerateAsync(builder, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        if (result is QRCodeResult qrResult)
            Assert.NotNull(qrResult.ImageBytes);
        else
            Assert.Fail("Result should be QRCodeResult");
    }

    [Fact]
    public async Task GenerateAsync_WithEmptyData_ThrowsException()
    {
        var service = CreateService();
        await Assert.ThrowsAsync<ArgumentException>(async () => {
            await service.GenerateAsync("", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        });
    }

    [Fact]
    public async Task GenerateAsync_WithNullData_ThrowsException()
    {
        var service = CreateService();
        await Assert.ThrowsAsync<ArgumentNullException>(async () => {
            await service.GenerateAsync(null!, TestContext.Current.CancellationToken).ConfigureAwait(false);
        });
    }

    [Fact]
    public async Task GenerateAsync_WithInvalidSize_ReturnsFailure()
    {
        var service = CreateService();
        var options = new QRCodeOptions {
            Format = QRCodeFormat.Png, Size = 10 // Too small
        };

        var result = await service.GenerateAsync("https://example.com", options, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Errors);
        Assert.True(result.Errors!.Count > 0);
        Assert.NotNull(result.Errors[0].Exception);
        Assert.IsType<ArgumentOutsideRangeException>(result.Errors[0].Exception);
    }

    [Fact]
    public async Task GenerateToStreamAsync_WithValidData_WritesToStream()
    {
        var service = CreateService();
        using var stream = new MemoryStream();
        var result = await service.GenerateToStreamAsync("https://example.com", stream, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        Assert.True(stream.Length > 0);
        stream.Position = 0;
        var bytes = new byte[stream.Length];
        await stream.ReadExactlyAsync(bytes, 0, (int)stream.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public async Task GenerateToFileAsync_WithValidData_CreatesFile()
    {
        var service = CreateService();
        var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
        try {
            var result = await service.GenerateToFileAsync("https://example.com", filePath, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
            Assert.True(result.IsSuccess);
            Assert.True(result.Data);
            Assert.True(File.Exists(filePath));
            var fileInfo = new FileInfo(filePath);
            Assert.True(fileInfo.Length > 0);
        }
        finally {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Fact]
    public async Task GenerateAsync_WithDifferentFormats_ReturnsSuccess()
    {
        var service = CreateService();

        // Test PNG format
        var pngOptions = new QRCodeOptions { Format = QRCodeFormat.Png, Size = 256 };
        var pngResult = await service.GenerateAsync("https://example.com", pngOptions, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(pngResult.IsSuccess);
        if (pngResult is QRCodeResult pngQrResult)
            Assert.Equal(QRCodeFormat.Png, pngQrResult.Format);
        else
            Assert.Fail("Result should be QRCodeResult");

        // Test SVG format (works on all platforms)
        var svgOptions = new QRCodeOptions { Format = QRCodeFormat.Svg, Size = 256 };
        var svgResult = await service.GenerateAsync("https://example.com", svgOptions, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(svgResult.IsSuccess);
        if (svgResult is QRCodeResult svgQrResult)
            Assert.Equal(QRCodeFormat.Svg, svgQrResult.Format);
        else
            Assert.Fail("Result should be QRCodeResult");
    }

    [Fact]
    public async Task GenerateAsync_WithDifferentErrorCorrectionLevels_ReturnsSuccess()
    {
        var service = CreateService();
        var levels = new[] { QRCodeErrorCorrectionLevel.Low, QRCodeErrorCorrectionLevel.Medium, QRCodeErrorCorrectionLevel.Quartile, QRCodeErrorCorrectionLevel.High };
        foreach (var level in levels) {
            var options = new QRCodeOptions { Format = QRCodeFormat.Png, Size = 256, ErrorCorrectionLevel = level };
            var result = await service.GenerateAsync("https://example.com", options, TestContext.Current.CancellationToken).ConfigureAwait(false);
            Assert.True(result.IsSuccess);
            if (result is QRCodeResult qrResult)
                Assert.NotNull(qrResult.ImageBytes);
            else
                Assert.Fail("Result should be QRCodeResult");
        }
    }

    [Fact]
    public async Task GenerateAsync_WithLongData_ReturnsSuccess()
    {
        var service = CreateService();
        var longData = "https://example.com/" + new string('a', 1000);
        var result = await service.GenerateAsync(longData, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        if (result is QRCodeResult qrResult)
            Assert.NotNull(qrResult.ImageBytes);
        else
            Assert.Fail("Result should be QRCodeResult");
    }

    [Fact]
    public async Task GenerateAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        var service = CreateService();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => {
            await service.GenerateAsync("https://example.com", ct: cts.Token).ConfigureAwait(false);
        });
    }

    [Fact]
    public async Task GenerateBatchAsync_WithMultipleRequests_ReturnsSuccess()
    {
        var service = CreateService();
        var requests = new[] {
            new QRCodeRequest { Data = "https://example.com/1", Options = new() { Format = QRCodeFormat.Png, Size = 256 } },
            new QRCodeRequest { Data = "https://example.com/2", Options = new() { Format = QRCodeFormat.Png, Size = 256 } },
            new QRCodeRequest { Data = "https://example.com/3", Options = new() { Format = QRCodeFormat.Png, Size = 256 } }
        };

        var result = await service.GenerateBatchAsync(requests, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.NotNull(result.Results);
        Assert.Equal(3, result.Results.Count);
        Assert.All(result.Results, r => Assert.True(r.IsSuccess));
    }

    [Fact]
    public async Task GenerateAsync_WithMetricsEnabled_RecordsGenericMetrics()
    {
        var metrics = new MetricsService();
        var service = CreateService(
            new() {
                DefaultFormat = QRCodeFormat.Png,
                DefaultSize = 256,
                DefaultErrorCorrectionLevel = QRCodeErrorCorrectionLevel.Medium,
                EnableMetrics = true
            }, metrics);

        var result = await service.GenerateAsync("https://example.com", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(metrics.GetHistogram(Constants.Metrics.GenerateDuration));
        Assert.Equal(1, metrics.GetCounterValue(Constants.Metrics.GenerateSuccess));
        Assert.Null(metrics.GetHistogram(QRCoder.Constants.Metrics.GenerateDuration));
    }

    [Fact]
    public void DefaultFormat_ReturnsConfiguredFormat()
    {
        var options = new QRCodeServiceOptions { DefaultFormat = QRCodeFormat.Svg };
        var service = CreateService(options);
        Assert.Equal(QRCodeFormat.Svg, service.DefaultFormat);
    }

    [Fact]
    public async Task GenerateAsync_WithCustomColors_ReturnsSuccess()
    {
        var service = CreateService();
        var options = new QRCodeOptions {
            Format = QRCodeFormat.Png,
            Size = 256,
            DarkColor = "#FF0000",
            LightColor = "#00FF00"
        };

        var result = await service.GenerateAsync("https://example.com", options, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        if (result is QRCodeResult qrResult)
            Assert.NotNull(qrResult.ImageBytes);
        else
            Assert.Fail("Result should be QRCodeResult");
    }

    [Fact]
    public async Task GenerateAsync_WithQuietZones_ReturnsSuccess()
    {
        var service = CreateService();
        var options = new QRCodeOptions { Format = QRCodeFormat.Png, Size = 256, DrawQuietZones = true };
        var result = await service.GenerateAsync("https://example.com", options, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        if (result is QRCodeResult qrResult)
            Assert.NotNull(qrResult.ImageBytes);
        else
            Assert.Fail("Result should be QRCodeResult");
    }
}