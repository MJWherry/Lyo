using System.Diagnostics;

namespace Lyo.Common;

/// <summary>Represents the result of a bulk operation containing multiple individual results.</summary>
/// <typeparam name="T">The type of the data returned on success for each individual result.</typeparam>
[DebuggerDisplay("{ToString(),nq}")]
public record BulkResult<T> : ResultBase
{
    // Lazy-cached computed values to avoid repeated O(n) enumeration.
    private readonly Lazy<IReadOnlyList<Result<T>>> _successfulResults;
    private readonly Lazy<IReadOnlyList<Result<T>>> _failedResults;
    private readonly Lazy<IReadOnlyList<Error>> _allErrors;
    private readonly Lazy<IReadOnlyList<string>> _errorCodes;
    private readonly Lazy<IReadOnlyList<string>> _errorMessages;

    /// <summary>Gets the collection of individual results.</summary>
    public IReadOnlyList<Result<T>> Results { get; }

    public BulkResult(IReadOnlyList<Result<T>> results)
    {
        Results = results;
        _successfulResults = new(() => Results.Where(r => r.IsSuccess).ToList(), isThreadSafe: false);
        _failedResults = new(() => Results.Where(r => !r.IsSuccess).ToList(), isThreadSafe: false);
        _allErrors = new(() => _failedResults.Value.SelectMany(r => r.Errors ?? []).ToList(), isThreadSafe: false);
        _errorCodes = new(() => _allErrors.Value.SelectMany(GetAllErrorCodes).Distinct().ToList(), isThreadSafe: false);
        _errorMessages = new(() => _allErrors.Value.SelectMany(GetAllErrorMessages).Distinct().ToList(), isThreadSafe: false);
    }

    /// <summary>Gets a value indicating whether any results succeeded.</summary>
    public override bool IsSuccess {
        get => SuccessCount > 0;
        init { }
    }

    /// <summary>Gets all errors from all failed results, or null if there are none.</summary>
    public override IReadOnlyList<Error>? Errors => _allErrors.Value.Count > 0 ? _allErrors.Value : null;

    /// <summary>Optional metadata (e.g. queue worker directives such as "requeue").</summary>
    public override IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>Gets the total number of results.</summary>
    public int TotalCount => Results.Count;

    /// <summary>Gets the number of successful results.</summary>
    public int SuccessCount => _successfulResults.Value.Count;

    /// <summary>Gets the number of failed results.</summary>
    public int FailureCount => _failedResults.Value.Count;

    /// <summary>Gets a value indicating whether all results were successful.</summary>
    public bool IsCompleteSuccess => SuccessCount == TotalCount && TotalCount > 0;

    /// <summary>Gets a value indicating whether all results failed.</summary>
    public bool IsCompleteFailure => FailureCount == TotalCount && TotalCount > 0;

    /// <summary>Gets a value indicating whether there are any errors.</summary>
    public bool HasErrors => FailureCount > 0;

    /// <summary>Gets a value indicating whether there is partial success (some succeeded, some failed).</summary>
    public bool HasPartialSuccess => SuccessCount > 0 && FailureCount > 0;

    /// <summary>Gets all successful results.</summary>
    public IReadOnlyList<Result<T>> SuccessfulResults => _successfulResults.Value;

    /// <summary>Gets all failed results.</summary>
    public IReadOnlyList<Result<T>> FailedResults => _failedResults.Value;

    /// <summary>Gets the first error from all failed results, if any.</summary>
    public Error? FirstError => _failedResults.Value.FirstOrDefault()?.Errors?.FirstOrDefault();

    /// <summary>Gets all distinct error codes from all failed results (including inner errors).</summary>
    public IReadOnlyList<string> ErrorCodes => _errorCodes.Value;

    /// <summary>Gets all distinct error messages from all failed results (including inner errors).</summary>
    public IReadOnlyList<string> ErrorMessages => _errorMessages.Value;

    /// <summary>Gets all successful data values.</summary>
    public IReadOnlyList<T> SuccessfulData => SuccessfulResults.Where(r => r.Data != null).Select(r => r.Data!).ToList();

    /// <summary>Gets any data values present on failed results.</summary>
    public IReadOnlyList<T> FailedData => FailedResults.Where(r => r.Data != null).Select(r => r.Data!).ToList();

    /// <summary>Creates a BulkResult from a collection of individual results.</summary>
    public static BulkResult<T> FromResults(IEnumerable<Result<T>> results) => new(results.ToList());

    /// <summary>Creates a BulkResult where every item is a success.</summary>
    public static BulkResult<T> FromData(IEnumerable<T> data) => new(data.Select(d => Result<T>.Success(d)).ToList());

    /// <summary>Creates a BulkResult where every item is a failure with the given error.</summary>
    public static BulkResult<T> FromErrors(IEnumerable<Error> errors) => new(errors.Select(e => Result<T>.Failure(e)).ToList());

    private static IEnumerable<string> GetAllErrorCodes(Error error)
    {
        yield return error.Code;

        var current = error.InnerError;
        while (current != null) {
            yield return current.Code;
            current = current.InnerError;
        }
    }

    private static IEnumerable<string> GetAllErrorMessages(Error error)
    {
        yield return error.Message;

        var current = error.InnerError;
        while (current != null) {
            yield return current.Message;
            current = current.InnerError;
        }
    }

    public override string ToString() => $"BulkResult: {SuccessCount}/{TotalCount} successful, Timestamp={Timestamp:O}";
}

/// <summary>Represents the result of a bulk operation with paired request/response results.</summary>
/// <typeparam name="TRequest">The type of the request object.</typeparam>
/// <typeparam name="TResult">The type of the data returned on success for each individual result.</typeparam>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record BulkResult<TRequest, TResult> : BulkResult<TResult>
{
    private readonly Lazy<IReadOnlyList<Result<TRequest, TResult>>> _successfulPaired;
    private readonly Lazy<IReadOnlyList<Result<TRequest, TResult>>> _failedPaired;

    /// <summary>Gets the collection of paired request/response results.</summary>
    public new IReadOnlyList<Result<TRequest, TResult>> Results { get; }

    public BulkResult(IReadOnlyList<Result<TRequest, TResult>> results)
        : base(results.Select(r => (Result<TResult>)r).ToList())
    {
        Results = results;
        _successfulPaired = new(() => Results.Where(r => r.IsSuccess).ToList(), isThreadSafe: false);
        _failedPaired = new(() => Results.Where(r => !r.IsSuccess).ToList(), isThreadSafe: false);
    }

    /// <summary>Gets all successful paired results.</summary>
    public new IReadOnlyList<Result<TRequest, TResult>> SuccessfulResults => _successfulPaired.Value;

    /// <summary>Gets all failed paired results.</summary>
    public new IReadOnlyList<Result<TRequest, TResult>> FailedResults => _failedPaired.Value;

    /// <summary>Gets all successful request objects.</summary>
    public IReadOnlyList<TRequest> SuccessfulRequests => SuccessfulResults.Where(r => r.Request != null).Select(r => r.Request!).ToList();

    /// <summary>Gets all failed request objects.</summary>
    public IReadOnlyList<TRequest> FailedRequests => FailedResults.Where(r => r.Request != null).Select(r => r.Request!).ToList();

    /// <summary>Creates a BulkResult from a collection of paired results.</summary>
    public static BulkResult<TRequest, TResult> FromResults(IEnumerable<Result<TRequest, TResult>> results) => new(results.ToList());

    /// <summary>Creates a BulkResult where every item is a success.</summary>
    public static BulkResult<TRequest, TResult> FromData(IEnumerable<(TRequest Request, TResult Data)> data)
        => new(data.Select(d => Result<TRequest, TResult>.Success(d.Request, d.Data)).ToList());

    /// <summary>Creates a BulkResult where every item is a failure.</summary>
    public static BulkResult<TRequest, TResult> FromErrors(IEnumerable<(TRequest Request, Error Error)> errors)
        => new(errors.Select(e => Result<TRequest, TResult>.Failure(e.Request, e.Error)).ToList());
}

/// <summary>Represents a bulk result produced from a single request — one request produces many individual results.</summary>
/// <typeparam name="TRequest">The type of the single request.</typeparam>
/// <typeparam name="TResult">The type of each individual result.</typeparam>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record BulkResultFromRequest<TRequest, TResult> : BulkResult<TResult>
{
    /// <summary>Gets the single request that produced these results.</summary>
    public TRequest Request { get; }

    public BulkResultFromRequest(TRequest request, IReadOnlyList<Result<TResult>> results)
        : base(results)
        => Request = request;

    /// <summary>Creates from a single request and successful data items.</summary>
    public static BulkResultFromRequest<TRequest, TResult> FromData(TRequest request, IEnumerable<TResult> data)
        => new(request, data.Select(d => Result<TResult>.Success(d)).ToList());

    /// <summary>Creates from a single request and a collection of results.</summary>
    public static BulkResultFromRequest<TRequest, TResult> FromResults(TRequest request, IEnumerable<Result<TResult>> results)
        => new(request, results.ToList());

    /// <summary>Creates a failed bulk result from the request and a collection of errors.</summary>
    public static BulkResultFromRequest<TRequest, TResult> FromErrors(TRequest request, IEnumerable<Error> errors)
        => new(request, errors.Select(e => Result<TResult>.Failure(e)).ToList());

    /// <summary>Creates a failed bulk result from an exception.</summary>
    public static BulkResultFromRequest<TRequest, TResult> FromException(TRequest request, Exception exception, string? code = null)
        => new(request, [Result<TResult>.Failure(exception, code)]);

    public override string ToString()
        => $"BulkResultFromRequest: Request={Request}, {SuccessCount}/{TotalCount} successful, Timestamp={Timestamp:O}";
}
