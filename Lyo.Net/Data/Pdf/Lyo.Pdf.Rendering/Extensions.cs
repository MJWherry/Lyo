using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
namespace Lyo.Pdf.Rendering;

/// <summary>Registers PDF rasterization services.</summary>
public static class PdfRenderingServiceCollectionExtensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="PdfToImagePageRasterizer"/> as <see cref="IPdfPageRasterizer"/>.</summary>
        public IServiceCollection AddPdfPageRasterizer()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<IPdfPageRasterizer, PdfToImagePageRasterizer>();
            return services;
        }
    }
}
