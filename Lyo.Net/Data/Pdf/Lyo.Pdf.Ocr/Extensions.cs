using Lyo.Exceptions;
using Lyo.Images.Ocr;
using Lyo.Pdf.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Pdf.Ocr;

/// <summary>Registers <see cref="PdfOcrService"/> (and <see cref="IPdfPageRasterizer"/> when missing).</summary>
public static class PdfOcrServiceCollectionExtensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="PdfOcrService"/>; registers <see cref="IPdfPageRasterizer"/> when not already present.</summary>
        /// <remarks><see cref="IOcrEngine"/> must be registered separately (e.g. Tesseract).</remarks>
        public IServiceCollection AddPdfOcr()
        {
            ArgumentHelpers.ThrowIfNull(services);
            if (!services.Any(static d => d.ServiceType == typeof(IPdfPageRasterizer)))
                services.AddPdfPageRasterizer();

            services.AddSingleton<PdfOcrService>();
            return services;
        }
    }
}
