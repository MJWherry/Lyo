using System.Diagnostics;

namespace Lyo.Common;

/// <summary>Represents the result of an operation that can either succeed with data or fail with errors.</summary>
/// <typeparam name="T">The type of the data returned on success.</typeparam>
[DebuggerDisplay("{ToString(),nq}")]
public record Result<T>(bool IsSuccess, T? Data, IReadOnlyList<Error>? Errors = null) : ResultBase
{
    public override IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>Creates a successful result with data.</summary>
    public static Result<T> Success(T data, DateTime? timestamp = null, IReadOnlyDictionary<string, object>? metadata = null)
        => new(true, data) { Timestamp = timestamp ?? DateTime.UtcNow, Metadata = metadata };

    /// <summary>Creates a failed result with errors.</summary>
    public static Result<T> Failure(IReadOnlyList<Error> errors, DateTime? timestamp = null, IReadOnlyDictionary<string, object>? metadata = null)
        => new(false, default, errors) { Timestamp = timestamp ?? DateTime.UtcNow, Metadata = metadata };

    /// <summary>Creates a failed result with a single error.</summary>
    public static Result<T> Failure(Error error, DateTime? timestamp = null, IReadOnlyDictionary<string, object>? metadata = null)
        => new(false, default, [error]) { Timestamp = timestamp ?? DateTime.UtcNow, Metadata = metadata };

    /// <summary>Creates a failed result with error details.</summary>
    public static Result<T> Failure(
        string message,
        string code,
        string? stackTrace = null,
        Error? innerError = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        DateTime? timestamp = null)
        => new(false, default, [new(message, code, stackTrace, innerError, metadata) { Timestamp = timestamp ?? DateTime.UtcNow }]) {
            Timestamp = timestamp ?? DateTime.UtcNow, Metadata = metadata
        };

    /// <summary>Creates a failed result from an exception.</summary>
    public static Result<T> Failure(Exception exception, string? code = null, IReadOnlyDictionary<string, object>? metadata = null, DateTime? timestamp = null)
        => new(false, default, [Error.FromException(exception, code, metadata, timestamp)]) { Timestamp = timestamp ?? DateTime.UtcNow, Metadata = metadata };

    /// <summary>Attempts to get the value from a successful result.</summary>
    /// <param name="value">The value if successful, otherwise default.</param>
    /// <returns>True if the result is successful and contains a value, otherwise false.</returns>
    public bool TryGetValue(out T value)
    {
        if (IsSuccess && Data != null) {
            value = Data;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>Gets the value from a successful result, or throws an exception if failed.</summary>
    /// <returns>The data value if successful.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the result is not successful.</exception>
    public T ValueOrThrow()
    {
        if (IsSuccess && Data != null)
            return Data;

        var errorMessages = Errors != null && Errors.Count > 0 ? string.Join("; ", Errors.Select(e => $"{e.Code}: {e.Message}")) : "Operation failed";
        throw new InvalidOperationException(errorMessages);
    }

    /// <summary>Gets the value from a successful result, or returns the default value if failed.</summary>
    public T ValueOrDefault(T defaultValue) => IsSuccess && Data != null ? Data : defaultValue;

    /// <summary>Gets the value from a successful result, or returns the value from the factory if failed.</summary>
    public T ValueOrDefault(Func<T> defaultValueFactory) => IsSuccess && Data != null ? Data : defaultValueFactory();

    /// <summary>Pattern matching - returns a value based on whether the result is successful or failed.</summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<IReadOnlyList<Error>, TResult> onFailure) => IsSuccess ? onSuccess(Data!) : onFailure(Errors ?? []);

    /// <summary>Executes actions based on whether the result is successful or failed.</summary>
    public Result<T> Switch(Action<T> onSuccess, Action<IReadOnlyList<Error>> onFailure)
    {
        if (IsSuccess && Data != null)
            onSuccess(Data);
        else
            onFailure(Errors ?? []);

        return this;
    }

    /// <summary>Provides a fallback value if the result failed.</summary>
    public T Recover(T fallbackValue) => IsSuccess && Data != null ? Data : fallbackValue;

    /// <summary>Provides a fallback value from a function if the result failed.</summary>
    public T Recover(Func<IReadOnlyList<Error>, T> fallback) => IsSuccess && Data != null ? Data : fallback(Errors ?? []);

    /// <summary>Attempts to recover from failure by executing a recovery operation.</summary>
    public Result<T> RecoverWith(Func<IReadOnlyList<Error>, Result<T>> recovery) => IsSuccess ? this : recovery(Errors ?? []);

    /// <summary>Filters the result - only succeeds if the predicate is true.</summary>
    public Result<T> Where(Func<T, bool> predicate, string errorCode, string errorMessage)
    {
        if (!IsSuccess)
            return this;

        if (Data != null && predicate(Data))
            return this;

        return Failure(errorMessage, errorCode, null, null, Metadata, Timestamp);
    }

    /// <summary>Gets all errors including inner errors as a flat list.</summary>
    public IReadOnlyList<Error> GetAllErrors()
    {
        var allErrors = new List<Error>();
        if (Errors != null) {
            foreach (var error in Errors) {
                allErrors.Add(error);
                FlattenInnerErrors(error, allErrors);
            }
        }

        return allErrors;
    }

    private static void FlattenInnerErrors(Error error, List<Error> accumulator)
    {
        var current = error.InnerError;
        while (current != null) {
            accumulator.Add(current);
            current = current.InnerError;
        }
    }

    /// <summary>Deconstructs the result into its components.</summary>
    public void Deconstruct(out bool isSuccess, out T? data, out IReadOnlyList<Error>? errors)
    {
        isSuccess = IsSuccess;
        data = Data;
        errors = Errors;
    }

    /// <summary>Implicit conversion from T to Result&lt;T&gt;.</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>Implicit conversion from Error to Result&lt;T&gt;.</summary>
    public static implicit operator Result<T>(Error error) => Failure(error);

    public override string ToString()
        => IsSuccess
            ? $"Success: {Data}, Timestamp={Timestamp:O}, Metadata Count={Metadata?.Count ?? 0}"
            : $"Failure: {string.Join("; ", Errors ?? [])}, Timestamp={Timestamp:O}, Metadata Count={Metadata?.Count ?? 0}";
}

/// <summary>Represents the result of an operation that includes both the request and result data.</summary>
/// <typeparam name="TRequest">The type of the request object.</typeparam>
/// <typeparam name="TResult">The type of the data returned on success.</typeparam>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Result<TRequest, TResult>(bool IsSuccess, TRequest? Request, TResult? Data, IReadOnlyList<Error>? Errors = null)
    : Result<TResult>(IsSuccess, Data, Errors)
{
    /// <summary>Creates a successful result with request and data.</summary>
    public static Result<TRequest, TResult> Success(TRequest request, TResult data, DateTime? timestamp = null, IReadOnlyDictionary<string, object>? metadata = null)
        => new(true, request, data) { Timestamp = timestamp ?? DateTime.UtcNow, Metadata = metadata };

    /// <summary>Creates a failed result with request and errors.</summary>
    public static Result<TRequest, TResult> Failure(TRequest request, IReadOnlyList<Error> errors, DateTime? timestamp = null, IReadOnlyDictionary<string, object>? metadata = null)
        => new(false, request, default, errors) { Timestamp = timestamp ?? DateTime.UtcNow, Metadata = metadata };

    /// <summary>Creates a failed result with request and a single error.</summary>
    public static Result<TRequest, TResult> Failure(TRequest request, Error error, DateTime? timestamp = null, IReadOnlyDictionary<string, object>? metadata = null)
        => new(false, request, default, [error]) { Timestamp = timestamp ?? DateTime.UtcNow, Metadata = metadata };

    /// <summary>Creates a failed result with request and error details.</summary>
    public static Result<TRequest, TResult> Failure(
        TRequest request,
        string message,
        string code,
        string? stackTrace = null,
        Error? innerError = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        DateTime? timestamp = null)
        => new(false, request, default, [new(message, code, stackTrace, innerError, metadata) { Timestamp = timestamp ?? DateTime.UtcNow }]) {
            Timestamp = timestamp ?? DateTime.UtcNow, Metadata = metadata
        };

    /// <summary>Creates a failed result with request from an exception.</summary>
    public static Result<TRequest, TResult> Failure(
        TRequest request,
        Exception exception,
        string? code = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        DateTime? timestamp = null)
        => new(false, request, default, [Error.FromException(exception, code, metadata, timestamp)]) { Timestamp = timestamp ?? DateTime.UtcNow, Metadata = metadata };

    /// <summary>Attempts to get the request object.</summary>
    /// <param name="request">The request if present, otherwise default.</param>
    /// <returns>True if the request is present, otherwise false.</returns>
    public bool TryGetRequest(out TRequest request)
    {
        if (Request != null) {
            request = Request;
            return true;
        }

        request = default!;
        return false;
    }

    /// <summary>Deconstructs the result into its components.</summary>
    public void Deconstruct(out bool isSuccess, out TRequest? request, out TResult? data, out IReadOnlyList<Error>? errors)
    {
        isSuccess = IsSuccess;
        request = Request;
        data = Data;
        errors = Errors;
    }

    public override string ToString()
        => IsSuccess
            ? $"Success: Request={Request}, Data={Data}, Timestamp={Timestamp:O}, Metadata Count={Metadata?.Count ?? 0}"
            : $"Failure: Request={Request}, Errors={string.Join("; ", Errors ?? [])}, Timestamp={Timestamp:O}, Metadata Count={Metadata?.Count ?? 0}";
}