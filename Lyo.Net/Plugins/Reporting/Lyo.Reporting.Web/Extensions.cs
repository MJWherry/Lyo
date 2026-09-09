using Lyo.Exceptions;
using Lyo.Reporting.Models.Rendering;
using Lyo.Reporting.Web.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Reporting.Web;

/// <summary>DI helpers for HTML/PDF report rendering.</summary>
public static class Extensions
{
    /// <summary>
    /// Adds <see cref="HtmlPdfReportRenderer" /> as a scoped <see cref="IReportRenderer" />. Lifetime matches
    /// <see cref="Lyo.Web.WebRenderer.IWebRendererService" /> (and Blazor <c>HtmlRenderer</c>), which must be registered separately.
    /// </summary>
    public static IServiceCollection AddReportingWebRenderer(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IReportRenderer, HtmlPdfReportRenderer>());
        return services;
    }
}