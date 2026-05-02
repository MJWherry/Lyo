using Lyo.Diagnostic.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Api;

/// <summary>Registers Lyo diagnostic web services (breadcrumbs, in-memory inbox, structured logging) for API hosts.</summary>
public static class LyoApiDiagnosticExtensions
{
    /// <summary>Equivalent to <see cref="DiagnosticWebServiceCollectionExtensions.AddLyoDiagnosticsWeb" />.</summary>
    public static IServiceCollection AddLyoApiDiagnosticRecording(this IServiceCollection services, Action<DiagnosticWebOptions>? configure = null)
        => services.AddLyoDiagnosticsWeb(configure);
}
