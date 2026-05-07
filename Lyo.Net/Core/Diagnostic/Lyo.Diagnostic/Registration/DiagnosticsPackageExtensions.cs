using Lyo.Diagnostic.Classification;
using Lyo.Diagnostic.Context;
using Lyo.Diagnostic.Logging;
using Lyo.Diagnostic.Sanitisation;
using Lyo.Diagnostic.StackTrace;
using Lyo.PackageMetadata;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Diagnostic.Registration;

public static class DiagnosticsPackageExtensions
{
    /// <summary>
    /// Registers the full diagnostics package as singletons:
    /// <list type="bullet">
    /// <item><see cref="IStackTraceDecoder" /> / <see cref="StackTraceDecoder" /></item> <item><see cref="IExceptionClassifier" /> / <see cref="ExceptionClassifier" /></item>
    /// <item>
    /// <see cref="IDiagnosticContextBuilder" /> / <see cref="DiagnosticContextBuilder" /> (use
    /// <see cref="IDiagnosticContextBuilder.BuildAsync(System.Exception, System.Threading.CancellationToken)" /> when <see cref="StackTraceDecoderOptions.PackageMetadataStore" /> is set)
    /// </item>
    /// <item><see cref="IStructuredLogEnricher" /> / <see cref="StructuredLogEnricher" /></item> <item><see cref="ITraceSanitiser" /> / <see cref="TraceSanitiser" /></item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddDiagnosticsPackage(
        this IServiceCollection services,
        StackTraceDecoderOptions? decoderOptions = null,
        ExceptionClassifierOptions? classifierOptions = null,
        DiagnosticContextOptions? contextOptions = null,
        LogEnricherOptions? enricherOptions = null,
        TraceSanitiserOptions? sanitiserOptions = null,
        IPackageMetadataStore? packageMetadataStore = null)
    {
        var mergedDecoderOptions = MergePackageMetadataStore(decoderOptions, packageMetadataStore);
        services.AddSingleton<IStackTraceDecoder>(new StackTraceDecoder(mergedDecoderOptions));
        services.AddSingleton<IExceptionClassifier>(new ExceptionClassifier(classifierOptions));
        services.AddSingleton<IStructuredLogEnricher>(new StructuredLogEnricher(enricherOptions));
        services.AddSingleton<ITraceSanitiser>(new TraceSanitiser(sanitiserOptions));

        // DiagnosticContextBuilder depends on the above — resolve from container
        services.AddSingleton<IDiagnosticContextBuilder>(sp => new DiagnosticContextBuilder(
            sp.GetRequiredService<IStackTraceDecoder>(), sp.GetRequiredService<IExceptionClassifier>(), contextOptions));

        return services;
    }

    private static StackTraceDecoderOptions? MergePackageMetadataStore(StackTraceDecoderOptions? options, IPackageMetadataStore? packageMetadataStore)
    {
        if (packageMetadataStore is null)
            return options;

        return (options ?? StackTraceDecoderOptions.Default) with { PackageMetadataStore = packageMetadataStore };
    }
}