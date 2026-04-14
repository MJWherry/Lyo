using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Pdf.Web.Components.PdfAnnotator;

/// <summary>Extension methods for registering the PDF annotator service.</summary>
public static class Extensions
{
    /// <summary>Adds the PDF annotator service to the service collection.</summary>
    public static IServiceCollection AddPdfAnnotatorService(this IServiceCollection services)
    {
        services.AddSingleton<IPdfAnnotatorService>(sp => new BrowserPdfAnnotator(sp.GetService<ILogger<BrowserPdfAnnotator>>()));
        return services;
    }

    /// <summary>Registers the controller used by Blazor PDF annotation components.</summary>
    public static IServiceCollection AddPdfAnnotatorInterop(this IServiceCollection services)
    {
        services.AddScoped<LyoPdfAnnotatorController>();
        return services;
    }
}