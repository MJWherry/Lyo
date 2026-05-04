using Lyo.Diagnostic;
using Lyo.Diagnostic.Breadcrumbs;
using Lyo.Diagnostic.Inbox;
using Lyo.Diagnostic.Registration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Diagnostic.AspNetCore;

/// <summary>Registers Lyo diagnostics, in-memory inbox, scoped breadcrumbs, and ASP.NET Core middleware dependencies.</summary>
public static class DiagnosticWebServiceCollectionExtensions
{
    /// <summary>
    /// Adds core diagnostics (<see cref="DiagnosticsPackageExtensions.AddDiagnosticsPackage" />), in-memory inbox, scoped <see cref="IBreadcrumbTrail" />, and
    /// <see cref="DiagnosticWebOptions" />.
    /// </summary>
    public static IServiceCollection AddLyoDiagnosticsWeb(this IServiceCollection services, Action<DiagnosticWebOptions>? configure = null)
    {
        services.AddOptions<DiagnosticWebOptions>();
        if (configure is not null)
            services.Configure(configure);

        services.AddDiagnosticsPackage();

        services.AddSingleton<InMemoryErrorInbox>(sp => {
            var web = sp.GetRequiredService<IOptions<DiagnosticWebOptions>>().Value;
            return new InMemoryErrorInbox(new() { MaxOccurrences = web.InMemoryInboxMaxOccurrences });
        });
        services.AddSingleton<IErrorOccurrenceSink>(sp => sp.GetRequiredService<InMemoryErrorInbox>());
        services.AddSingleton<IErrorInboxReader>(sp => sp.GetRequiredService<InMemoryErrorInbox>());

        services.AddSingleton<IBreadcrumbRedactor>(_ => PassThroughBreadcrumbRedactor.Instance);
        services.AddScoped<IBreadcrumbTrail>(sp => {
            var web = sp.GetRequiredService<IOptions<DiagnosticWebOptions>>().Value;
            var redactor = sp.GetRequiredService<IBreadcrumbRedactor>();
            return new RingBufferBreadcrumbTrail(web.BreadcrumbCapacity, redactor);
        });

        return services;
    }
}
