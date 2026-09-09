using Lyo.Exceptions;
using Lyo.Images.Ocr;
using Lyo.Pdf.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Pdf.Ocr;

/// <summary>DI helpers that register <see cref="PdfOcrService" /> (and <see cref="IPdfPageRasterizer" /> when missing).</summary>
public static class PdfOcrServiceCollectionExtensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="PdfOcrService" />; adds <see cref="IPdfPageRasterizer" /> when it is not already present.</summary>
        /// <remarks><see cref="IOcrEngine" /> must be registered separately (for example Tesseract).</remarks>
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