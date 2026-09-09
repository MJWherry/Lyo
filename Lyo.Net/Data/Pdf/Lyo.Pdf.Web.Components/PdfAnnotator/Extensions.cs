using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Pdf.Web.Components.PdfAnnotator;

/// <summary>DI helpers that register the PDF annotator service.</summary>
public static class Extensions
{
    /// <summary>Registers the PDF annotator service on the collection.</summary>
    public static IServiceCollection AddPdfAnnotatorService(this IServiceCollection services)
    {
        services.AddSingleton<IPdfAnnotatorService>(sp => new BrowserPdfAnnotator(sp.GetService<ILogger<BrowserPdfAnnotator>>()));
        return services;
    }

    /// <summary>Registers the interop controller used by Blazor PDF annotation components.</summary>
    public static IServiceCollection AddPdfAnnotatorInterop(this IServiceCollection services)
    {
        services.AddScoped<LyoPdfAnnotatorController>();
        return services;
    }
}