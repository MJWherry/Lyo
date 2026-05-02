using Lyo.Diagnostic.Context;
using Microsoft.Extensions.Logging;

namespace Lyo.Diagnostic.Logging;

public interface IStructuredLogEnricher
{
    /// <summary>Writes structured diagnostic properties to an <see cref="ILogger" /> at the appropriate level for the exception severity.</summary>
    void Log(ILogger logger, DiagnosticContext context, string? additionalMessage = null);

    /// <summary>
    /// Returns a flat <see cref="IReadOnlyDictionary{TKey,TValue}" /> of the structured properties that would be written to the log. Use this when integrating with Serilog,
    /// NLog, or other structured loggers directly, or when you want to attach the properties to an existing log scope.
    /// </summary>
    IReadOnlyDictionary<string, object?> BuildLogProperties(DiagnosticContext context);
}