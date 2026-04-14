using System.Diagnostics;

namespace Lyo.Common;

/// <summary>Represents the result of a bulk operation containing multiple individual results.</summary>
/// <typeparam name="T">The type of the data returned on success for each individual result.</typeparam>
[DebuggerDisplay("{ToString(),nq}")]
public record BulkResult<T>(IReadOnlyList<Result<T>> Results) : ResultBase
{
    /// <summary>Gets a value indicating whether any results succeeded. Used for queue requeue decisions.</summary>
    public override bool IsSuccess {
        get => SuccessCount > 0;
        init { }
    }

    /// <summary>Optional metadata for queue workers (e.g. "requeue" to override default behavior).</summary>
    public override IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>Gets the total number of results.</summary>
    public int TotalCount => Results.Count;

    /// <summary>Gets the number of successful results.</summary>
    public int SuccessCount => Results.Count(r => r.IsSuccess);

    /// <summary>Gets the number of failed results.</summary>
    public int FailureCount => Results.Count(r => !r.IsSuccess);

    /// <summary>Gets a value indicating whether all results were successful.</summary>
    public bool IsCompleteSuccess => SuccessCount == TotalCount && TotalCount > 0;

    /// <summary>Gets a value indicating whether all results failed.</summary>
    public bool IsCompleteFailure => FailureCount == TotalCount && TotalCount > 0;

    /// <summary>Gets all successful results.</summary>
    public IReadOnlyList<Result<T>> SuccessfulResults => Results.Where(r => r.IsSuccess).ToList();

    /// <summary>Gets all failed results.</summary>
    public IReadOnlyList<Result<T>> FailedResults => Results.Where(r => !r.IsSuccess).ToList();

    /// <summary>Gets a value indicating whether there are any errors.</summary>
    public bool HasErrors => FailureCount > 0;

    /// <summary>Gets a value indicating whether there is partial success (some succeeded, some failed).</summary>
    public bool HasPartialSuccess => SuccessCount > 0 && FailureCount > 0;

    /// <summary>Gets the first error from all failed results, if any.</summary>
    public Error? FirstError => FailedResults.FirstOrDefault()?.Errors?.FirstOrDefault();

    /// <summary>Gets all error codes from all failed results.</summary>
    public IReadOnlyList<string> ErrorCodes => FailedResults.SelectMany(r => r.Errors ?? []).SelectMany(GetAllErrorCodes).Distinct().ToList();

    /// <summary>Gets all error messages from all failed results.</summary>
    public IReadOnlyList<string> ErrorMessages => FailedResults.SelectMany(r => r.Errors ?? []).SelectMany(GetAllErrorMessages).Distinct().ToList();

    /// <summary>Gets all successful data values.</summary>
    public IReadOnlyList<T> SuccessfulData => SuccessfulResults.Where(r => r.Data != null).Select(r => r.Data!).ToList();

    /// <summary>Gets all failed data values (if any were provided before failure).</summary>
    public IReadOnlyList<T> FailedData => FailedResults.Where(r => r.Data != null).Select(r => r.Data!).ToList();

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

    /// <summary>Creates a BulkResult from a collection of individual results.</summary>
    public static BulkResult<T> FromResults(IEnumerable<Result<T>> results) => new(results.ToList());

    /// <summary>Creates a BulkResult from a collection of data values (all successful).</summary>
    public static BulkResult<T> FromData(IEnumerable<T> data) => new(data.Select(d => Result<T>.Success(d)).ToList());

    /// <summary>Creates a BulkResult from a collection of errors (all failed).</summary>
    public static BulkResult<T> FromErrors(IEnumerable<Error> errors) => new(errors.Select(e => Result<T>.Failure(e)).ToList());

    public override string ToString() => $"BulkResult: {SuccessCount}/{TotalCount} successful, Timestamp={Timestamp:O}";
}

/// <summary>Represents the result of a bulk operation containing multiple individual results with request/response pairs.</summary>
/// <typeparam name="TRequest">The type of the request object.</typeparam>
/// <typeparam name="TResult">The type of the data returned on success for each individual result.</typeparam>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record BulkResult<TRequest, TResult> : BulkResult<TResult>
{
    /// <summary>Gets the collection of individual results with request/response pairs.</summary>
    public new IReadOnlyList<Result<TRequest, TResult>> Results { get; }

    /// <summary>Gets all successful results.</summary>
    public new IReadOnlyList<Result<TRequest, TResult>> SuccessfulResults => Results.Where(r => r.IsSuccess).ToList();

    /// <summary>Gets all failed results.</summary>
    public new IReadOnlyList<Result<TRequest, TResult>> FailedResults => Results.Where(r => !r.IsSuccess).ToList();

    /// <summary>Gets all successful request objects.</summary>
    public IReadOnlyList<TRequest> SuccessfulRequests => SuccessfulResults.Where(r => r.Request != null).Select(r => r.Request!).ToList();

    /// <summary>Gets all failed request objects.</summary>
    public IReadOnlyList<TRequest> FailedRequests => FailedResults.Where(r => r.Request != null).Select(r => r.Request!).ToList();

    /// <summary>Initializes a new instance of the bulk result.</summary>
    /// <param name="results">The collection of request/response results.</param>
    public BulkResult(IReadOnlyList<Result<TRequest, TResult>> results)
        : base(results.Select(r => (Result<TResult>)r).ToList())
        => Results = results;

    /// <summary>Creates a BulkResult from a collection of individual results.</summary>
    public static BulkResult<TRequest, TResult> FromResults(IEnumerable<Result<TRequest, TResult>> results) => new(results.ToList());

    /// <summary>Creates a BulkResult from a collection of request/response pairs (all successful).</summary>
    public static BulkResult<TRequest, TResult> FromData(IEnumerable<(TRequest Request, TResult Data)> data)
        => new(data.Select(d => Result<TRequest, TResult>.Success(d.Request, d.Data)).ToList());

    /// <summary>Creates a BulkResult from a collection of errors with requests (all failed).</summary>
    public static BulkResult<TRequest, TResult> FromErrors(IEnumerable<(TRequest Request, Error Error)> errors)
        => new(errors.Select(e => Result<TRequest, TResult>.Failure(e.Request, e.Error)).ToList());
}

/// <summary>Represents a bulk result from a single request - one request produces many individual results.</summary>
/// <typeparam name="TRequest">The type of the single request.</typeparam>
/// <typeparam name="TResult">The type of each individual result.</typeparam>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record BulkResultFromRequest<TRequest, TResult> : BulkResult<TResult>
{
    /// <summary>Gets the single request that produced these results.</summary>
    public TRequest Request { get; }

    /// <summary>Initializes a new instance with one request and many results.</summary>
    /// <param name="request">The single request that produced the results.</param>
    /// <param name="results">The collection of individual results.</param>
    public BulkResultFromRequest(TRequest request, IReadOnlyList<Result<TResult>> results)
        : base(results)
        => Request = request;

    /// <summary>Creates from a single request and successful data items.</summary>
    public static BulkResultFromRequest<TRequest, TResult> FromData(TRequest request, IEnumerable<TResult> data)
        => new(request, data.Select(d => Result<TResult>.Success(d)).ToList());

    /// <summary>Creates from a single request and a collection of results.</summary>
    public static BulkResultFromRequest<TRequest, TResult> FromResults(TRequest request, IEnumerable<Result<TResult>> results) => new(request, results.ToList());

    /// <summary>Creates a failed bulk result with the request and errors.</summary>
    public static BulkResultFromRequest<TRequest, TResult> FromErrors(TRequest request, IEnumerable<Error> errors)
        => new(request, errors.Select(e => Result<TResult>.Failure(e)).ToList());

    /// <summary>Creates a failed bulk result from an exception.</summary>
    public static BulkResultFromRequest<TRequest, TResult> FromException(TRequest request, Exception exception, string? code = null)
        => new(request, [Result<TResult>.Failure(exception, code)]);

    public override string ToString() => $"BulkResultFromRequest: Request={Request}, {SuccessCount}/{TotalCount} successful, Timestamp={Timestamp:O}";
}