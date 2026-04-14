using Lyo.Common.Enums;
using Lyo.Exceptions;
using static Lyo.Exceptions.OperationHelpers;

namespace Lyo.Common.Builders;

/// <summary>Fluent builder for creating Error instances.</summary>
public class ErrorBuilder
{
    private string _code = string.Empty;
    private Exception? _exception;
    private Error? _innerError;
    private string? _message;
    private Dictionary<string, object>? _metadata;
    private ErrorSeverity _severity = ErrorSeverity.Error;
    private string? _stackTrace;
    private DateTime? _timestamp;

    /// <summary>Creates a new builder instance.</summary>
    public static ErrorBuilder Create() => new();

    /// <summary>Creates a builder from an existing exception.</summary>
    public static ErrorBuilder FromException(Exception exception, string? code = null)
    {
        var builder = new ErrorBuilder().WithMessage(exception.Message).WithCode(code ?? exception.GetType().Name).WithStackTrace(exception.StackTrace);
        if (exception.InnerException != null)
            builder.WithInnerException(exception.InnerException, code);

        return builder;
    }

    /// <summary>Sets the error message.</summary>
    public ErrorBuilder WithMessage(string? message)
    {
        _message = message;
        return this;
    }

    /// <summary>Sets the error code.</summary>
    public ErrorBuilder WithCode(string code)
    {
        _code = ArgumentHelpers.ThrowIfNullReturn(code, nameof(code));
        return this;
    }

    /// <summary>Sets the stack trace.</summary>
    public ErrorBuilder WithStackTrace(string? stackTrace)
    {
        _stackTrace = stackTrace;
        return this;
    }

    /// <summary>Sets the inner error.</summary>
    public ErrorBuilder WithInnerError(Error? innerError)
    {
        _innerError = innerError;
        return this;
    }

    /// <summary>Sets the inner error from an exception.</summary>
    public ErrorBuilder WithInnerException(Exception? innerException, string? code = null)
    {
        if (innerException != null)
            _innerError = Error.FromException(innerException, code);

        return this;
    }

    /// <summary>Adds a metadata key-value pair.</summary>
    public ErrorBuilder WithMetadata(string key, object value)
    {
        _metadata ??= new();
        _metadata[key] = value;
        return this;
    }

    /// <summary>Sets the metadata dictionary.</summary>
    public ErrorBuilder WithMetadata(IReadOnlyDictionary<string, object>? metadata)
    {
        if (metadata != null) {
            _metadata = new();
            foreach (var kvp in metadata)
                _metadata[kvp.Key] = kvp.Value;
        }
        else
            _metadata = null;

        return this;
    }

    /// <summary>Adds multiple metadata entries.</summary>
    public ErrorBuilder WithMetadata(params (string key, object value)[] entries)
    {
        _metadata ??= new();
        foreach (var (key, value) in entries)
            _metadata[key] = value;

        return this;
    }

    /// <summary>Sets the timestamp.</summary>
    public ErrorBuilder WithTimestamp(DateTime timestamp)
    {
        _timestamp = timestamp;
        return this;
    }

    /// <summary>Sets the exception. If message or stacktrace aren't set, they will be populated from the exception.</summary>
    public ErrorBuilder WithException(Exception? exception)
    {
        _exception = exception;
        return this;
    }

    /// <summary>Sets the error severity.</summary>
    public ErrorBuilder WithSeverity(ErrorSeverity severity)
    {
        _severity = severity;
        return this;
    }

    /// <summary>Builds the Error instance.</summary>
    public Error Build()
    {
        ThrowIfNullOrEmpty(_code, "Code is required to build an Error");

        // If exception is provided and message isn't set, use exception message
        if (string.IsNullOrEmpty(_message) && _exception != null)
            _message = _exception.Message;

        // If exception is provided and stacktrace isn't set, use exception stack trace
        if (string.IsNullOrEmpty(_stackTrace) && _exception != null)
            _stackTrace = _exception.StackTrace;

        // If InnerError isn't provided but exception has InnerException, it will be handled in Error constructor
        var error = new Error(_message, _code, _stackTrace, _innerError, _metadata, _exception, _severity) { Timestamp = _timestamp ?? DateTime.UtcNow };
        return error;
    }

    /// <summary>Implicit conversion to Error.</summary>
    public static implicit operator Error(ErrorBuilder builder) => builder.Build();
}