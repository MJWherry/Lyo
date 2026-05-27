using Lyo.Diagnostic;
using Lyo.Diagnostic.AspNetCore.Correlation;
using Lyo.Diagnostic.Breadcrumbs;
using Lyo.Diagnostic.Correlation;
using Lyo.Diagnostic.Inbox;
using Lyo.Diagnostic.Registration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Diagnostic.AspNetCore;

/// <summary>Registers Lyo diagnostics, in-memory inbox, scoped breadcrumbs, and ASP.NET Core middleware dependencies.</summary>
public static class DiagnosticWebServiceCollectionExtensions
{
    /// <summary>
    /// Adds core diagnostics (<see cref="DiagnosticsPackageExtensions.AddDiagnosticsPackage" />), in-memory inbox, scoped <see cref="IBreadcrumbTrail" />, the HTTP-aware
    /// <see cref="ICorrelationIdResolver"/>, the outbound <see cref="LyoCorrelationDelegatingHandler"/>, and <see cref="DiagnosticWebOptions" />.
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

        services.AddLyoCorrelation();
        return services;
    }

    /// <summary>
    /// Registers <see cref="HttpContextCorrelationIdResolver"/> as the ambient <see cref="ICorrelationIdResolver"/> (only if no resolver has been registered yet) and the
    /// <see cref="LyoCorrelationDelegatingHandler"/> + <see cref="CorrelationHandlerOptions"/> needed by <see cref="AddLyoCorrelationHandler"/>. Called automatically by
    /// <see cref="AddLyoDiagnosticsWeb"/>; expose separately for hosts that don't want the full diagnostics surface but still want correlation propagation.
    /// </summary>
    public static IServiceCollection AddLyoCorrelation(this IServiceCollection services)
    {
        services.AddOptions<DiagnosticWebOptions>();
        services.AddOptions<CorrelationHandlerOptions>();
        services.AddHttpContextAccessor();
        services.TryAddSingleton<ICorrelationIdResolver, HttpContextCorrelationIdResolver>();
        services.TryAddTransient<LyoCorrelationDelegatingHandler>(sp => new(
            sp.GetRequiredService<ICorrelationIdResolver>(),
            sp.GetService<IOptions<CorrelationHandlerOptions>>()?.Value));
        return services;
    }

    /// <summary>
    /// Chains <see cref="LyoCorrelationDelegatingHandler"/> onto a typed-client pipeline. Register the handler dependencies first via <see cref="AddLyoCorrelation"/> (or
    /// <see cref="AddLyoDiagnosticsWeb"/>). For pipelines that also include an auth handler, call this <strong>before</strong> the auth handler so the correlation header is
    /// stamped on the outermost request and propagates through any nested refresh roundtrip.
    /// </summary>
    public static IHttpClientBuilder AddLyoCorrelationHandler(this IHttpClientBuilder builder)
        => builder.AddHttpMessageHandler<LyoCorrelationDelegatingHandler>();
}
