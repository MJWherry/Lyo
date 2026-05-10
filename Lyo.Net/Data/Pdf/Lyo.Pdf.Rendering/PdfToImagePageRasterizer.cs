using System.Diagnostics;
using Lyo.Result;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PDFtoImage;
using SixLabors.ImageSharp;

namespace Lyo.Pdf.Rendering;

/// <summary><see cref="IPdfPageRasterizer"/> backed by PDFtoImage (PDFium + Skia).</summary>
public sealed class PdfToImagePageRasterizer(ILogger<PdfToImagePageRasterizer>? logger = null) : IPdfPageRasterizer
{
    private readonly ILogger _logger = logger ?? NullLogger<PdfToImagePageRasterizer>.Instance;

    /// <inheritdoc />
    public Task<Result<PdfRasterPage>> RenderPageToPngAsync(
        ReadOnlyMemory<byte> pdfBytes,
        int pageNumber1Based,
        int dpi,
        string? password = null,
        CancellationToken cancellationToken = default)
        => Task.Run(
            () => {
                var sw = Stopwatch.StartNew();
                try {
                    ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber1Based, 1);
                    ArgumentOutOfRangeException.ThrowIfLessThan(dpi, 1);
                    var buffer = pdfBytes.ToArray();
                    var pageCount = Conversion.GetPageCount(buffer, password ?? "");
                    if (pageNumber1Based > pageCount)
                        return Result<PdfRasterPage>.Failure(new Error($"Page {pageNumber1Based} is out of range (document has {pageCount} pages).", PdfRenderErrorCodes.PageOutOfRange));

                    var index = Index.FromStart(pageNumber1Based - 1);
                    using var pngStream = new MemoryStream();
                    Conversion.SavePng(pngStream, buffer, index, password ?? "", new RenderOptions(Dpi: dpi));
                    var pngBytes = pngStream.ToArray();
                    using var probe = new MemoryStream(pngBytes, writable: false);
                    var info = Image.Identify(probe);
                    if (info is null)
                        throw new InvalidOperationException("Rendered output is not a valid image.");

                    sw.Stop();
                    _logger.LogTrace("Rendered PDF page {Page} at {Dpi} dpi in {Ms} ms.", pageNumber1Based, dpi, sw.Elapsed.TotalMilliseconds);
                    return Result<PdfRasterPage>.Success(new PdfRasterPage(pngBytes, info.Width, info.Height));
                }
                catch (Exception ex) {
                    sw.Stop();
                    _logger.LogDebug(ex, "PDF rasterization failed for page {Page}.", pageNumber1Based);
                    return Result<PdfRasterPage>.Failure(ex, PdfRenderErrorCodes.RenderFailed);
                }
            },
            cancellationToken);
}
