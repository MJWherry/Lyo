namespace Lyo.Common;

/// <summary>Static factory methods for Result&lt;Unit&gt; (void/unit results).</summary>
public static class ResultVoid
{
    /// <summary>Creates a successful result with no data.</summary>
    public static Result<Unit> Success(DateTime? timestamp = null, IReadOnlyDictionary<string, object>? metadata = null) => Result<Unit>.Success(Unit.Value, timestamp, metadata);

    /// <summary>Creates a failed result with errors.</summary>
    public static Result<Unit> Failure(IReadOnlyList<Error> errors, DateTime? timestamp = null, IReadOnlyDictionary<string, object>? metadata = null)
        => Result<Unit>.Failure(errors, timestamp, metadata);

    /// <summary>Creates a failed result with a single error.</summary>
    public static Result<Unit> Failure(Error error, DateTime? timestamp = null, IReadOnlyDictionary<string, object>? metadata = null)
        => Result<Unit>.Failure(error, timestamp, metadata);

    /// <summary>Creates a failed result with error details.</summary>
    public static Result<Unit> Failure(
        string message,
        string code,
        string? stackTrace = null,
        Error? innerError = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        DateTime? timestamp = null)
        => Result<Unit>.Failure(message, code, stackTrace, innerError, metadata, timestamp);

    /// <summary>Creates a failed result from an exception.</summary>
    public static Result<Unit> Failure(Exception exception, string? code = null, IReadOnlyDictionary<string, object>? metadata = null, DateTime? timestamp = null)
        => Result<Unit>.Failure(exception, code, metadata, timestamp);
}