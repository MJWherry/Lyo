using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions.Models;
using Lyo.Web.WebRenderer.Models;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace Lyo.Web.WebRenderer.Tests;

public sealed class WebRendererCaptureTests
{
    [Fact]
    public void BuildCaptureOptions_LetterLandscapePdf_SetsFormatAndCssPreference()
    {
        var pdf = CaptureMapper.BuildPdfOptions(new WebRenderReq { Paper = "Letter", Landscape = true, PreferCssPageSize = true, Scale = 0.8, Margin = "0.5in" });
        Assert.Equal(PaperFormat.Letter, pdf.Format);
        Assert.True(pdf.Landscape);
        Assert.True(pdf.PreferCSSPageSize);
        Assert.True(pdf.PrintBackground);
        Assert.Equal(0.8m, pdf.Scale);
        Assert.Equal("0.5in", pdf.MarginOptions.Top);
    }

    [Fact]
    public void BuildCaptureOptions_Jpeg_SetsQualityAndFullPage()
    {
        var shot = CaptureMapper.BuildScreenshotOptions(new WebRenderReq { Format = WebRenderFormat.Jpeg, Quality = 70, FullPage = true, OmitBackground = true });
        Assert.Equal(ScreenshotType.Jpeg, shot.Type);
        Assert.Equal(70, shot.Quality);
        Assert.True(shot.FullPage);
        Assert.True(shot.OmitBackground);
    }

    [Fact]
    public void BuildLaunchOptions_NoSandbox_IncludesArg()
    {
        var plain = CaptureMapper.BuildLaunchOptions(new WebRenderOptions { BrowserExePath = "/usr/bin/chromium", NoSandbox = false });
        Assert.Contains("--disable-dev-shm-usage", plain.Args);
        Assert.DoesNotContain("--no-sandbox", plain.Args);

        var sandbox = CaptureMapper.BuildLaunchOptions(new WebRenderOptions { BrowserExePath = "/usr/bin/chromium", NoSandbox = true });
        Assert.Contains("--disable-dev-shm-usage", sandbox.Args);
        Assert.Contains("--no-sandbox", sandbox.Args);
    }

    [Fact]
    public async Task ConvertHtmlToPdfAsync_MissingBrowser_ThrowsConfigurationException()
    {
        var renderer = WebRendererTestHost.CreateService(new WebRenderOptions { BrowserExePath = null });
        var ex = await Assert.ThrowsAsync<ConfigurationException>(() => renderer.ConvertHtmlToPdfAsync("<p>Hi</p>", TestCt));
        Assert.Contains("BrowserExePath", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CopyCaptureToAsync_LetterPdf_MediaBoxIsLetter()
    {
        var browser = Utilities.DetectBrowserPath(SupportedBrowser.Chrome);
        Assert.SkipUnless(browser is not null, "Chrome was not found.");
        await using var renderer = WebRendererTestHost.CreateService(new WebRenderOptions { BrowserExePath = browser });
        await using var stream = new MemoryStream();
        var html = "<!DOCTYPE html><html><head><style>@page { size: letter portrait; }</style></head><body><p>Letter</p></body></html>";
        await renderer.CopyCaptureToAsync(html, stream, new WebRenderReq { Format = WebRenderFormat.Pdf, Paper = "CSS", PreferCssPageSize = true }, TestCt);
        var text = System.Text.Encoding.ASCII.GetString(stream.ToArray());
        var box = MediaBox(text);
        Assert.InRange(box.Width, 600, 624);
        Assert.InRange(box.Height, 780, 804);
    }

    [Fact]
    public async Task CopyCaptureToAsync_PngAndJpeg_MatchMagic()
    {
        var browser = Utilities.DetectBrowserPath(SupportedBrowser.Chrome);
        Assert.SkipUnless(browser is not null, "Chrome was not found.");
        await using var renderer = WebRendererTestHost.CreateService(new WebRenderOptions { BrowserExePath = browser });
        var html = "<!DOCTYPE html><html><body><h1>Shot</h1></body></html>";

        await using var png = new MemoryStream();
        await renderer.CopyCaptureToAsync(html, png, new WebRenderReq { Format = WebRenderFormat.Png }, TestCt);
        var pngBytes = png.ToArray();
        Assert.Equal([0x89, 0x50, 0x4E, 0x47], pngBytes[..4]);

        await using var jpeg = new MemoryStream();
        await renderer.CopyCaptureToAsync(html, jpeg, new WebRenderReq { Format = WebRenderFormat.Jpeg, Quality = 80 }, TestCt);
        var jpegBytes = jpeg.ToArray();
        Assert.Equal([0xFF, 0xD8, 0xFF], jpegBytes[..3]);
    }

    [Fact]
    public async Task GenerateAsync_Pdf_WritesFileAndRes()
    {
        var browser = Utilities.DetectBrowserPath(SupportedBrowser.Chrome);
        Assert.SkipUnless(browser is not null, "Chrome was not found.");
        await using var renderer = WebRendererTestHost.CreateService(new WebRenderOptions { BrowserExePath = browser });
        var path = Path.Combine(Path.GetTempPath(), LyoGuid.CreateV7().ToString("N") + ".pdf");
        try {
            var result = await renderer.GenerateAsync(new WebRenderReq { Html = "<p>File</p>", Format = WebRenderFormat.Pdf, OutputFilePath = path }, TestCt);
            Assert.True(result.Success);
            Assert.Equal(path, result.OutputPath);
            Assert.Equal(new FileInfo(path).Length, result.ByteLength);
            Assert.True(result.ByteLength > 0);
        }
        finally {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static (double Width, double Height) MediaBox(string pdf)
    {
        const string marker = "/MediaBox [";
        var start = pdf.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, "PDF did not contain a MediaBox.");
        var end = pdf.IndexOf(']', start);
        var parts = pdf[(start + marker.Length)..end].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return (double.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture), double.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture));
    }
}
