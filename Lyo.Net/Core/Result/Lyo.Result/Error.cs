using System.Diagnostics;
using Lyo.Common.Enums;
using Lyo.Exceptions;

namespace Lyo.Common;

/// <summary>Represents an error with a message, code, type, optional stack trace, inner error, metadata, timestamp, and optional exception.</summary>
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

    public ErrorType Type { get; init; } = ErrorType.Generic;

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public Error(
        string? message,
        string code,
        string? stackTrace = null,
        Error? innerError = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        Exception? exception = null,
        ErrorSeverity severity = ErrorSeverity.Error,
        ErrorType type = ErrorType.Generic)
    {
        Code = ArgumentHelpers.ThrowIfNullReturn(code, nameof(code));
        Exception = exception;

        var resolvedMessage = !string.IsNullOrEmpty(message) ? message : exception?.Message;
        ArgumentHelpers.ThrowIf(string.IsNullOrEmpty(resolvedMessage), "Either message or exception must be provided", nameof(message));
        Message = resolvedMessage!;

        StackTrace = !string.IsNullOrEmpty(stackTrace) ? stackTrace : exception?.StackTrace;

        if (innerError == null && exception?.InnerException != null)
            InnerError = FromException(exception.InnerException, code);
        else
            InnerError = innerError;

        Metadata = metadata;
        Severity = severity;
        Type = type;
    }

    public override string ToString()
        => $"Code={Code}, Type={Type}, Message={Message}, Severity={Severity}, Timestamp={Timestamp:O}, StackTrace Available={!string.IsNullOrEmpty(StackTrace)}, InnerError={InnerError != null}, Exception={Exception != null}, Metadata Count={Metadata?.Count ?? 0}";

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

    /// <summary>Creates a validation error.</summary>
    public static Error Validation(
        string message,
        string code = ValidationErrorCodes.ValidationFailed,
        IReadOnlyDictionary<string, object>? metadata = null)
        => new(message, code, null, null, metadata, null, ErrorSeverity.Warning, ErrorType.Validation);

    /// <summary>Creates a not-found error.</summary>
    public static Error NotFound(
        string message,
        string code = ValidationErrorCodes.NotFound,
        IReadOnlyDictionary<string, object>? metadata = null)
        => new(message, code, null, null, metadata, null, ErrorSeverity.Error, ErrorType.NotFound);

    /// <summary>Creates a conflict error.</summary>
    public static Error Conflict(
        string message,
        string code = ValidationErrorCodes.Conflict,
        IReadOnlyDictionary<string, object>? metadata = null)
        => new(message, code, null, null, metadata, null, ErrorSeverity.Error, ErrorType.Conflict);

    /// <summary>Creates an unauthorized error.</summary>
    public static Error Unauthorized(
        string message = "Authentication is required",
        string code = ValidationErrorCodes.Unauthorized,
        IReadOnlyDictionary<string, object>? metadata = null)
        => new(message, code, null, null, metadata, null, ErrorSeverity.Error, ErrorType.Unauthorized);

    /// <summary>Creates a forbidden error.</summary>
    public static Error Forbidden(
        string message = "You do not have permission to perform this action",
        string code = ValidationErrorCodes.Forbidden,
        IReadOnlyDictionary<string, object>? metadata = null)
        => new(message, code, null, null, metadata, null, ErrorSeverity.Error, ErrorType.Forbidden);

    /// <summary>Creates an internal error, optionally wrapping an exception.</summary>
    public static Error Internal(
        string message = "An internal error occurred",
        string code = ValidationErrorCodes.InternalError,
        Exception? exception = null,
        IReadOnlyDictionary<string, object>? metadata = null)
        => new(message, code, exception?.StackTrace, exception != null ? FromException(exception, code) : null, metadata, exception, ErrorSeverity.Critical, ErrorType.InternalError);

    /// <summary>Creates a timeout error.</summary>
    public static Error Timeout(
        string message = "The operation timed out",
        string code = ValidationErrorCodes.Timeout,
        IReadOnlyDictionary<string, object>? metadata = null)
        => new(message, code, null, null, metadata, null, ErrorSeverity.Error, ErrorType.Timeout);

    /// <summary>Creates a service-unavailable error.</summary>
    public static Error ServiceUnavailable(
        string message = "A required service is unavailable",
        string code = ValidationErrorCodes.ServiceUnavailable,
        IReadOnlyDictionary<string, object>? metadata = null)
        => new(message, code, null, null, metadata, null, ErrorSeverity.Critical, ErrorType.ServiceUnavailable);
}
