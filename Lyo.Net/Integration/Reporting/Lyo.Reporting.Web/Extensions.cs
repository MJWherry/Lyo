using Lyo.Exceptions;
using Lyo.Reporting.Models.Rendering;
using Lyo.Reporting.Web.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Reporting.Web;

/// <summary>DI extensions for HTML/PDF report rendering.</summary>
public static class Extensions
{
    /// <summary>
    /// Registers <see cref="HtmlPdfReportRenderer"/> as an <see cref="IReportRenderer"/>.
    /// Requires <see cref="Lyo.Web.WebRenderer.IWebRendererService"/> to be registered separately.
    /// </summary>
    public static IServiceCollection AddReportingWebRenderer(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReportRenderer, HtmlPdfReportRenderer>());
        return services;
    }
}
