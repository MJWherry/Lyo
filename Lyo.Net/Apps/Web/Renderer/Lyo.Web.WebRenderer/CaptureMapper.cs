using Lyo.Exceptions;
using Lyo.Web.WebRenderer.Models;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace Lyo.Web.WebRenderer;

/// <summary>Maps a <see cref="WebRenderReq" /> onto Puppeteer options without launching a browser.</summary>
internal static class CaptureMapper
{
    internal static PdfOptions BuildPdfOptions(WebRenderReq request)
    {
        ArgumentHelpers.ThrowIfNull(request);
        var pdf = new PdfOptions {
            Format = ResolvePaper(request.Paper) ?? PaperFormat.A4,
            Landscape = request.Landscape,
            PreferCSSPageSize = request.PreferCssPageSize,
            PrintBackground = true
        };

        if (request.Scale is double scale)
            pdf.Scale = (decimal)scale;

        if (!string.IsNullOrWhiteSpace(request.Margin))
            pdf.MarginOptions = new() {
                Top = request.Margin,
                Right = request.Margin,
                Bottom = request.Margin,
                Left = request.Margin
            };

        return pdf;
    }

    internal static ScreenshotOptions BuildScreenshotOptions(WebRenderReq request)
    {
        ArgumentHelpers.ThrowIfNull(request);
        return new() {
            Type = request.Format switch {
                WebRenderFormat.Jpeg => ScreenshotType.Jpeg,
                WebRenderFormat.WebP => ScreenshotType.Webp,
                var _ => ScreenshotType.Png
            },
            Quality = request.Quality,
            FullPage = request.FullPage,
            OmitBackground = request.OmitBackground
        };
    }

    internal static LaunchOptions BuildLaunchOptions(WebRenderOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        var args = new List<string> { "--disable-dev-shm-usage" };
        if (options.NoSandbox)
            args.Add("--no-sandbox");

        return new() { Headless = true, ExecutablePath = options.BrowserExePath, Args = [.. args] };
    }

    private static PaperFormat? ResolvePaper(string? paper)
    {
        if (string.IsNullOrWhiteSpace(paper) || paper.Equals("CSS", StringComparison.OrdinalIgnoreCase) || paper.Equals("Auto", StringComparison.OrdinalIgnoreCase))
            return null;

        return paper.ToLowerInvariant() switch {
            "letter" => PaperFormat.Letter,
            "legal" => PaperFormat.Legal,
            "tabloid" => PaperFormat.Tabloid,
            "a3" => PaperFormat.A3,
            "a4" => PaperFormat.A4,
            "a5" => PaperFormat.A5,
            var _ => throw new ArgumentException($"Paper '{paper}' is not supported. Use Letter, Legal, Tabloid, A3, A4, A5, or CSS.", nameof(paper))
        };
    }
}