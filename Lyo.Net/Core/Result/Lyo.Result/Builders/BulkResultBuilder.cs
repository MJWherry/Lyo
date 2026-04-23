namespace Lyo.Common.Builders;

/// <summary>Fluent builder for constructing a <see cref="BulkResult{T}"/> incrementally.</summary>
/// <typeparam name="T">The data type for each individual result.</typeparam>
public sealed class BulkResultBuilder<T>
{
    private readonly List<Result<T>> _results = [];

    /// <summary>Creates a new builder instance.</summary>
    public static BulkResultBuilder<T> Create() => new();

    /// <summary>Adds a successful result with the given data.</summary>
    public BulkResultBuilder<T> AddSuccess(T data)
    {
        _results.Add(Result<T>.Success(data));
        return this;
    }

    /// <summary>Adds a failed result with the given error.</summary>
    public BulkResultBuilder<T> AddFailure(Error error)
    {
        _results.Add(Result<T>.Failure(error));
        return this;
    }

    /// <summary>Adds a failed result with error details.</summary>
    public BulkResultBuilder<T> AddFailure(string message, string code,
        string? stackTrace = null, Error? innerError = null,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        _results.Add(Result<T>.Failure(message, code, stackTrace, innerError, metadata));
        return this;
    }

    /// <summary>Adds a failed result from an exception.</summary>
    public BulkResultBuilder<T> AddFailure(Exception exception, string? code = null,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        _results.Add(Result<T>.Failure(exception, code, metadata));
        return this;
    }

    /// <summary>Adds a pre-built result (success or failure).</summary>
    public BulkResultBuilder<T> Add(Result<T> result)
    {
        _results.Add(result);
        return this;
    }

    /// <summary>Adds multiple pre-built results.</summary>
    public BulkResultBuilder<T> AddRange(IEnumerable<Result<T>> results)
    {
        _results.AddRange(results);
        return this;
    }

    /// <summary>Adds results for each item in the sequence using the provided factory.</summary>
    public BulkResultBuilder<T> AddEach<TSource>(IEnumerable<TSource> source, Func<TSource, Result<T>> factory)
    {
        foreach (var item in source)
            _results.Add(factory(item));

        return this;
    }

    /// <summary>Gets the number of results added so far.</summary>
    public int Count => _results.Count;

    /// <summary>Builds and returns the <see cref="BulkResult{T}"/>.</summary>
    public BulkResult<T> Build() => BulkResult<T>.FromResults(_results);

    /// <summary>Implicit conversion to <see cref="BulkResult{T}"/>.</summary>
    public static implicit operator BulkResult<T>(BulkResultBuilder<T> builder) => builder.Build();
}

/// <summary>Fluent builder for constructing a <see cref="BulkResult{TRequest, TResult}"/> incrementally.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResult">The data type for each individual result.</typeparam>
public sealed class BulkResultBuilder<TRequest, TResult>
{
    private readonly List<Result<TRequest, TResult>> _results = [];

    /// <summary>Creates a new builder instance.</summary>
    public static BulkResultBuilder<TRequest, TResult> Create() => new();

    /// <summary>Adds a successful paired result.</summary>
    public BulkResultBuilder<TRequest, TResult> AddSuccess(TRequest request, TResult data)
    {
        _results.Add(Result<TRequest, TResult>.Success(request, data));
        return this;
    }

    /// <summary>Adds a failed paired result with the given error.</summary>
    public BulkResultBuilder<TRequest, TResult> AddFailure(TRequest request, Error error)
    {
        _results.Add(Result<TRequest, TResult>.Failure(request, error));
        return this;
    }

    /// <summary>Adds a failed paired result from an exception.</summary>
    public BulkResultBuilder<TRequest, TResult> AddFailure(TRequest request, Exception exception, string? code = null)
    {
        _results.Add(Result<TRequest, TResult>.Failure(request, exception, code));
        return this;
    }

    /// <summary>Adds a pre-built paired result.</summary>
    public BulkResultBuilder<TRequest, TResult> Add(Result<TRequest, TResult> result)
    {
        _results.Add(result);
        return this;
    }

    /// <summary>Adds results for each item in the sequence using the provided factory.</summary>
    public BulkResultBuilder<TRequest, TResult> AddEach<TSource>(IEnumerable<TSource> source, Func<TSource, Result<TRequest, TResult>> factory)
    {
        foreach (var item in source)
            _results.Add(factory(item));

        return this;
    }

    /// <summary>Gets the number of results added so far.</summary>
    public int Count => _results.Count;

    /// <summary>Builds and returns the <see cref="BulkResult{TRequest, TResult}"/>.</summary>
    public BulkResult<TRequest, TResult> Build() => BulkResult<TRequest, TResult>.FromResults(_results);

    /// <summary>Implicit conversion to <see cref="BulkResult{TRequest, TResult}"/>.</summary>
    public static implicit operator BulkResult<TRequest, TResult>(BulkResultBuilder<TRequest, TResult> builder) => builder.Build();
}
