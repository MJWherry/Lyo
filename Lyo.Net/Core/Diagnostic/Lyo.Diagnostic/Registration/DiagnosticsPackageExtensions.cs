using Lyo.Diagnostic.Classification;
using Lyo.Diagnostic.Context;
using Lyo.Diagnostic.Logging;
using Lyo.Diagnostic.Sanitisation;
using Lyo.Diagnostic.StackTrace;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Diagnostic;

public static class DiagnosticsPackageExtensions
{
    /// <summary>
    /// Registers the full diagnostics package as singletons:
    /// <list type="bullet">
    /// <item><see cref="IStackTraceDecoder" /> / <see cref="StackTraceDecoder" /></item> <item><see cref="IExceptionClassifier" /> / <see cref="ExceptionClassifier" /></item>
    /// <item><see cref="IDiagnosticContextBuilder" /> / <see cref="DiagnosticContextBuilder" /></item>
    /// <item><see cref="IStructuredLogEnricher" /> / <see cref="StructuredLogEnricher" /></item> <item><see cref="ITraceSanitiser" /> / <see cref="TraceSanitiser" /></item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddDiagnosticsPackage(
        this IServiceCollection services,
        StackTraceDecoderOptions? decoderOptions = null,
        ExceptionClassifierOptions? classifierOptions = null,
        DiagnosticContextOptions? contextOptions = null,
        LogEnricherOptions? enricherOptions = null,
        TraceSanitiserOptions? sanitiserOptions = null)
    {
        services.AddSingleton<IStackTraceDecoder>(new StackTraceDecoder(decoderOptions));
        services.AddSingleton<IExceptionClassifier>(new ExceptionClassifier(classifierOptions));
        services.AddSingleton<IStructuredLogEnricher>(new StructuredLogEnricher(enricherOptions));
        services.AddSingleton<ITraceSanitiser>(new TraceSanitiser(sanitiserOptions));

        // DiagnosticContextBuilder depends on the above — resolve from container
        services.AddSingleton<IDiagnosticContextBuilder>(sp => new DiagnosticContextBuilder(
            sp.GetRequiredService<IStackTraceDecoder>(), sp.GetRequiredService<IExceptionClassifier>(), contextOptions));

        return services;
    }
}