using Lyo.Images.Ocr.Models;
using Lyo.Result;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Lyo.Images.Ocr.Tesseract.Tests;

public sealed class TesseractOcrEngineTests(TesseractOcrTestFixture fixture)
{
    [Fact]
    public async Task ReadAsync_Empty_Stream_Returns_ImageEmpty_Before_Engine_Load()
    {
        var shared = new OcrEngineOptions();
        var tess = new TesseractOcrEngineOptions { TessdataDirectory = "/nonexistent/numeric-placeholder-only" };
        using var engine = new TesseractOcrEngine(shared, tess);
        await using var empty = new MemoryStream();
        var result = await engine.ReadAsync(empty, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(result.IsSuccess);
        Assert.Equal(OcrErrorCodes.ImageEmpty, result.Errors![0].Code);
    }

    [Fact]
    public async Task ReadAsync_Missing_Tessdata_Returns_Failure()
    {
        var shared = new OcrEngineOptions();
        var tess = new TesseractOcrEngineOptions { TessdataDirectory = Path.Combine(Path.GetTempPath(), "missing-tessdata-" + Guid.NewGuid()) };
        using var engine = new TesseractOcrEngine(shared, tess);
        await using var png = new MemoryStream(CreatePngWithText("X"));
        var result = await engine.ReadAsync(png, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(result.IsSuccess);
        Assert.Equal(OcrErrorCodes.EngineNotConfigured, result.Errors![0].Code);
    }

    [Fact]
    public async Task ReadAsync_Png_With_Clear_Text_Recognizes_Content()
    {
        Assert.SkipUnless(fixture.RunNativeIntegration, TesseractOcrTestFixture.NativeIntegrationDisabledSkipMessage);
        var tessDir = fixture.ResolveTessdataDirectory();
        Assert.SkipWhen(string.IsNullOrEmpty(tessDir), TesseractOcrTestFixture.TessdataResolutionHint);
        var shared = fixture.GetOcrEngineOptions();
        var tess = fixture.GetTesseractOcrEngineOptions();
        if (string.IsNullOrWhiteSpace(tess.TessdataDirectory))
            tess.TessdataDirectory = tessDir;

        using var engine = new TesseractOcrEngine(shared, tess);
        await using var png = new MemoryStream(CreatePngWithText("HELLO"));
        var result = await engine.ReadAsync(png, cancellationToken: TestContext.Current.CancellationToken);
        SkipIfNativeLibrariesUnavailable(result);
        Assert.True(result.IsSuccess, result.Errors is { } e ? string.Join("; ", e.Select(x => x.Message)) : "");
        Assert.NotNull(result.Data);
        Assert.Contains("HELLO", result.Data!.FullText.Replace(" ", "", StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(result.Data.Words);
    }

    [Fact]
    public async Task ReadAsync_Produces_Y_Up_Boxes()
    {
        Assert.SkipUnless(fixture.RunNativeIntegration, TesseractOcrTestFixture.NativeIntegrationDisabledSkipMessage);
        var tessDir = fixture.ResolveTessdataDirectory();
        Assert.SkipWhen(string.IsNullOrEmpty(tessDir), TesseractOcrTestFixture.TessdataResolutionHint);
        var shared = fixture.GetOcrEngineOptions();
        var tessOpts = fixture.GetTesseractOcrEngineOptions();
        tessOpts.TessdataDirectory = string.IsNullOrWhiteSpace(tessOpts.TessdataDirectory) ? tessDir : tessOpts.TessdataDirectory;
        using var engine = new TesseractOcrEngine(shared, tessOpts);
        await using var png = new MemoryStream(CreatePngWithText("AB"));
        var result = await engine.ReadAsync(png, new() { PageSegmentationMode = OcrPageSegmentationMode.SingleBlock }, TestContext.Current.CancellationToken);
        SkipIfNativeLibrariesUnavailable(result);
        Assert.True(result.IsSuccess);
        foreach (var w in result.Data!.Words) {
            Assert.True(w.BoundingBoxPixels.Top >= w.BoundingBoxPixels.Bottom);
            Assert.True(w.BoundingBoxPixels.Right >= w.BoundingBoxPixels.Left);
        }
    }

    private static void SkipIfNativeLibrariesUnavailable(Result<OcrPageResult> result)
    {
        if (result.IsSuccess || result.Errors is not { } errs)
            return;

        var first = errs[0];
        if (first.Code == OcrErrorCodes.NativeLibraryNotFound)
            Assert.Skip(first.Message);
    }

    private static byte[] CreatePngWithText(string text)
    {
        using var image = new Image<Rgba32>(640, 240, Color.White);
        var font = SystemFonts.CreateFont(SystemFonts.Families.First().Name, 72, FontStyle.Bold);
        image.Mutate(ctx => ctx.DrawText(text, font, Color.Black, new(40, 80)));
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }
}