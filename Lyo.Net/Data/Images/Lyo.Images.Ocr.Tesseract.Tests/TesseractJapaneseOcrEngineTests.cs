using Lyo.Images.Ocr;
using Lyo.Images.Ocr.Models;
using Lyo.Result;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Lyo.Images.Ocr.Tesseract.Tests;

/// <summary>Japanese OCR requires <c>jpn.traineddata</c> alongside <c>eng.traineddata</c> in tessdata and a Japanese-capable font for raster synthesis.</summary>
public sealed class TesseractJapaneseOcrEngineTests(TesseractOcrTestFixture fixture)
{
    private readonly TesseractOcrTestFixture _fixture = fixture;

    private static readonly string[] JapaneseFontCandidates = [
        "Noto Sans CJK JP",
        "Noto Serif CJK JP",
        "Noto Sans JP",
        "Noto Serif JP",
        "IPAPGothic",
        "IPAexGothic",
        "IPAexMincho",
        "Yu Gothic",
        "YuMincho",
        "Meiryo",
        "MS Gothic",
        "Hiragino Sans",
        "PingFang SC",
        "Microsoft YaHei"
    ];

    [Fact]
    public async Task ReadAsync_jpn_language_recognizes_rendered_japanese()
    {
        Assert.SkipUnless(_fixture.RunNativeIntegration, TesseractOcrTestFixture.NativeIntegrationDisabledSkipMessage);
        var tessDir = _fixture.ResolveTessdataDirectory();
        Assert.SkipWhen(string.IsNullOrEmpty(tessDir), TesseractOcrTestFixture.TessdataResolutionHint);
        Assert.SkipUnless(TesseractOcrTestFixture.HasLanguageModel(tessDir, "jpn"), TesseractOcrTestFixture.JapaneseModelMissingHint);

        var png = TryCreatePngWithJapaneseText("日本語");

        Assert.SkipWhen(png == null,
            "No Japanese-capable font found for ImageSharp rendering (install fonts-noto-cjk or fonts-ipafont).");

        var shared = _fixture.GetOcrEngineOptions();
        var tess = _fixture.GetTesseractOcrEngineOptions();
        if (string.IsNullOrWhiteSpace(tess.TessdataDirectory))
            tess.TessdataDirectory = tessDir;

        using var engine = new TesseractOcrEngine(shared, tess);
        await using var ms = new MemoryStream(png);
        var result = await engine.ReadAsync(ms, new OcrReadRequest { Languages = "jpn", PageSegmentationMode = OcrPageSegmentationMode.SingleBlock },
            TestContext.Current.CancellationToken);
        SkipIfNativeLibrariesUnavailable(result);
        Assert.True(result.IsSuccess, result.Errors is { } e ? string.Join("; ", e.Select(x => x.Message)) : "");
        Assert.NotNull(result.Data);

        var compact = WhitespaceRemoved(result.Data!.FullText);
        Assert.True(compact.Contains("日本語", StringComparison.Ordinal) || compact.Contains("日本", StringComparison.Ordinal),
            $"Expected Japanese glyphs in OCR text; got: {result.Data.FullText}");
    }

    [Fact]
    public async Task ReadAsync_eng_jpn_combined_models_recognize_japanese_png()
    {
        Assert.SkipUnless(_fixture.RunNativeIntegration, TesseractOcrTestFixture.NativeIntegrationDisabledSkipMessage);
        var tessDir = _fixture.ResolveTessdataDirectory();
        Assert.SkipWhen(string.IsNullOrEmpty(tessDir), TesseractOcrTestFixture.TessdataResolutionHint);
        Assert.SkipUnless(TesseractOcrTestFixture.HasLanguageModel(tessDir, "jpn"), TesseractOcrTestFixture.JapaneseModelMissingHint);

        var png = TryCreatePngWithJapaneseText("テスト");

        Assert.SkipWhen(png == null,
            "No Japanese-capable font found for ImageSharp rendering (install fonts-noto-cjk or fonts-ipafont).");

        var shared = _fixture.GetOcrEngineOptions();
        var tess = _fixture.GetTesseractOcrEngineOptions();
        if (string.IsNullOrWhiteSpace(tess.TessdataDirectory))
            tess.TessdataDirectory = tessDir;

        using var engine = new TesseractOcrEngine(shared, tess);
        await using var ms = new MemoryStream(png);
        var result = await engine.ReadAsync(ms, new OcrReadRequest { Languages = "eng+jpn", PageSegmentationMode = OcrPageSegmentationMode.SingleBlock },
            TestContext.Current.CancellationToken);
        SkipIfNativeLibrariesUnavailable(result);
        Assert.True(result.IsSuccess, result.Errors is { } e ? string.Join("; ", e.Select(x => x.Message)) : "");
        Assert.NotNull(result.Data);

        var compact = WhitespaceRemoved(result.Data!.FullText);
        Assert.Contains("テスト", compact, StringComparison.Ordinal);
    }

    private static string WhitespaceRemoved(string text) =>
        text.Replace(" ", "", StringComparison.Ordinal).Replace("\n", "", StringComparison.Ordinal).Replace("\r", "", StringComparison.Ordinal);

    private static byte[]? TryCreatePngWithJapaneseText(string text)
    {
        foreach (var family in JapaneseFontCandidates) {
            try {
                var font = SystemFonts.CreateFont(family, 56, FontStyle.Bold);
                using var image = new Image<Rgba32>(960, 320, Color.White);
                image.Mutate(ctx => ctx.DrawText(text, font, Color.Black, new(48f, 120f)));
                using var ms = new MemoryStream();
                image.SaveAsPng(ms);
                return ms.ToArray();
            }
            catch {
                // Family missing or glyph not covered — try next candidate.
            }
        }

        return null;
    }

    private static void SkipIfNativeLibrariesUnavailable(Result<OcrPageResult> result)
    {
        if (result.IsSuccess || result.Errors is not { } errs)
            return;

        var first = errs[0];
        if (first.Code == OcrErrorCodes.NativeLibraryNotFound)
            Assert.Skip(first.Message);
    }
}
