using System.Diagnostics;
using Lyo.Common.Enums;
using Lyo.Exceptions;

namespace Lyo.Common;

/// <summary>Represents an error with a message, code, optional stack trace, inner error, metadata, timestamp, and optional exception.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Error
{
    public string Message { get; init; }

    public string Code { get; init; }

    public string? StackTrace { get; init; }

    public Error? InnerError { get; init; }

    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    public Exception? Exception { get; init; }

    public ErrorSeverity Severity { get; init; } = ErrorSeverity.Error;

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public Error(
        string? message,
        string code,
        string? stackTrace = null,
        Error? innerError = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        Exception? exception = null,
        ErrorSeverity severity = ErrorSeverity.Error)
    {
        Code = ArgumentHelpers.ThrowIfNullReturn(code, nameof(code));
        Exception = exception;

        // If exception is provided and message isn't, use exception message
        // Ensure Message is never null
        var resolvedMessage = !string.IsNullOrEmpty(message) ? message : exception?.Message;
        ArgumentHelpers.ThrowIf(string.IsNullOrEmpty(resolvedMessage), "Either message or exception must be provided", nameof(message));
        Message = resolvedMessage!; // Safe because we've validated it's not null above

        // If exception is provided and stackTrace isn't, use exception stack trace
        StackTrace = !string.IsNullOrEmpty(stackTrace) ? stackTrace : exception?.StackTrace;

        // If InnerError isn't provided but exception has InnerException, drill down
        if (innerError == null && exception?.InnerException != null)
            InnerError = FromException(exception.InnerException, code);
        else
            InnerError = innerError;

        Metadata = metadata;
        Severity = severity;
    }

    public override string ToString()
        => $"Code={Code}, Message={Message}, Severity={Severity}, Timestamp={Timestamp:O}, StackTrace Available={!string.IsNullOrEmpty(StackTrace)}, InnerError={InnerError != null}, Exception={Exception != null}, Metadata Count={Metadata?.Count ?? 0}";

    /// <summary>Creates an Error from an exception.</summary>
    public static Error FromException(
        Exception exception,
        string? code = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        DateTime? timestamp = null,
        ErrorSeverity severity = ErrorSeverity.Error)
    {
        Error? innerError = null;
        if (exception.InnerException != null)
            innerError = FromException(exception.InnerException, code, null, timestamp, severity);

        return new(exception.Message, code ?? exception.GetType().Name, exception.StackTrace, innerError, metadata, exception, severity) {
            Timestamp = timestamp ?? DateTime.UtcNow
        };
    }
}