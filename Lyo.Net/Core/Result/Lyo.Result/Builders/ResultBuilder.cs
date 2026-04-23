namespace Lyo.Common.Builders;

/// <summary>Fluent builder for creating Result instances.</summary>
/// <typeparam name="T">The type of the data returned on success.</typeparam>
public class ResultBuilder<T>
{
    private T? _data;
    private List<Error>? _errors;
    private bool _isSuccess;
    private Dictionary<string, object>? _metadata;
    private DateTime? _timestamp;

    /// <summary>Creates a new builder instance.</summary>
    public static ResultBuilder<T> Create() => new();

    /// <summary>Sets the result as successful with data.</summary>
    public ResultBuilder<T> WithSuccess(T data)
    {
        _isSuccess = true;
        _data = data;
        _errors = null;
        return this;
    }

    /// <summary>Sets the result as failed with errors.</summary>
    public ResultBuilder<T> WithFailure(IReadOnlyList<Error> errors)
    {
        _isSuccess = false;
        _data = default;
        _errors = new(errors);
        return this;
    }

    /// <summary>Sets the result as failed with a single error.</summary>
    public ResultBuilder<T> WithFailure(Error error)
    {
        _isSuccess = false;
        _data = default;
        _errors = new() { error };
        return this;
    }

    /// <summary>Sets the result as failed with error details.</summary>
    public ResultBuilder<T> WithFailure(string message, string code, string? stackTrace = null, Error? innerError = null, IReadOnlyDictionary<string, object>? metadata = null)
    {
        _isSuccess = false;
        _data = default;
        _errors = new() { new(message, code, stackTrace, innerError, metadata) };
        return this;
    }

    /// <summary>Sets the result as failed from an exception.</summary>
    public ResultBuilder<T> WithFailure(Exception exception, string? code = null, IReadOnlyDictionary<string, object>? metadata = null)
    {
        _isSuccess = false;
        _data = default;
        _errors = new() { Error.FromException(exception, code, metadata) };
        return this;
    }

    /// <summary>Adds an error to the failure list.</summary>
    public ResultBuilder<T> AddError(Error error)
    {
        _isSuccess = false;
        _errors ??= new();
        _errors.Add(error);
        return this;
    }

    /// <summary>Adds an error to the failure list with details.</summary>
    public ResultBuilder<T> AddError(string message, string code, string? stackTrace = null, Error? innerError = null, IReadOnlyDictionary<string, object>? metadata = null)
    {
        _isSuccess = false;
        _errors ??= new();
        _errors.Add(new(message, code, stackTrace, innerError, metadata));
        return this;
    }

    /// <summary>Sets the timestamp.</summary>
    public ResultBuilder<T> WithTimestamp(DateTime timestamp)
    {
        _timestamp = timestamp;
        return this;
    }

    /// <summary>Adds a metadata key-value pair.</summary>
    public ResultBuilder<T> WithMetadata(string key, object value)
    {
        _metadata ??= new();
        _metadata[key] = value;
        return this;
    }

    /// <summary>Sets the metadata dictionary.</summary>
    public ResultBuilder<T> WithMetadata(IReadOnlyDictionary<string, object>? metadata)
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
    public ResultBuilder<T> WithMetadata(params (string key, object value)[] entries)
    {
        _metadata ??= new();
        foreach (var (key, value) in entries)
            _metadata[key] = value;

        return this;
    }

    /// <summary>Builds the Result instance.</summary>
    public Result<T> Build() => new(_isSuccess, _data, _errors) { Timestamp = _timestamp ?? DateTime.UtcNow, Metadata = _metadata };

    /// <summary>Implicit conversion to Result.</summary>
    public static implicit operator Result<T>(ResultBuilder<T> builder) => builder.Build();
}

/// <summary>Fluent builder for creating Result instances with request and result data.</summary>
/// <typeparam name="TRequest">The type of the request object.</typeparam>
/// <typeparam name="TResult">The type of the data returned on success.</typeparam>
public class ResultBuilder<TRequest, TResult>
{
    private TResult? _data;
    private List<Error>? _errors;
    private bool _isSuccess;
    private Dictionary<string, object>? _metadata;
    private TRequest? _request;
    private DateTime? _timestamp;

    /// <summary>Creates a new builder instance.</summary>
    public static ResultBuilder<TRequest, TResult> Create() => new();

    /// <summary>Sets the request object.</summary>
    public ResultBuilder<TRequest, TResult> WithRequest(TRequest request)
    {
        _request = request;
        return this;
    }

    /// <summary>Sets the result as successful with request and data.</summary>
    public ResultBuilder<TRequest, TResult> WithSuccess(TRequest request, TResult data)
    {
        _isSuccess = true;
        _request = request;
        _data = data;
        _errors = null;
        return this;
    }

    /// <summary>Sets the result as failed with request and errors.</summary>
    public ResultBuilder<TRequest, TResult> WithFailure(TRequest request, IReadOnlyList<Error> errors)
    {
        _isSuccess = false;
        _request = request;
        _data = default;
        _errors = new(errors);
        return this;
    }

    /// <summary>Sets the result as failed with request and a single error.</summary>
    public ResultBuilder<TRequest, TResult> WithFailure(TRequest request, Error error)
    {
        _isSuccess = false;
        _request = request;
        _data = default;
        _errors = new() { error };
        return this;
    }

    /// <summary>Sets the result as failed with request and error details.</summary>
    public ResultBuilder<TRequest, TResult> WithFailure(
        TRequest request,
        string message,
        string code,
        string? stackTrace = null,
        Error? innerError = null,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        _isSuccess = false;
        _request = request;
        _data = default;
        _errors = new() { new(message, code, stackTrace, innerError, metadata) };
        return this;
    }

    /// <summary>Sets the result as failed with request from an exception.</summary>
    public ResultBuilder<TRequest, TResult> WithFailure(TRequest request, Exception exception, string? code = null, IReadOnlyDictionary<string, object>? metadata = null)
    {
        _isSuccess = false;
        _request = request;
        _data = default;
        _errors = new() { Error.FromException(exception, code, metadata) };
        return this;
    }

    /// <summary>Adds an error to the failure list.</summary>
    public ResultBuilder<TRequest, TResult> AddError(Error error)
    {
        _isSuccess = false;
        _errors ??= new();
        _errors.Add(error);
        return this;
    }

    /// <summary>Adds an error to the failure list with details.</summary>
    public ResultBuilder<TRequest, TResult> AddError(
        string message,
        string code,
        string? stackTrace = null,
        Error? innerError = null,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        _isSuccess = false;
        _errors ??= new();
        _errors.Add(new(message, code, stackTrace, innerError, metadata));
        return this;
    }

    /// <summary>Sets the timestamp.</summary>
    public ResultBuilder<TRequest, TResult> WithTimestamp(DateTime timestamp)
    {
        _timestamp = timestamp;
        return this;
    }

    /// <summary>Adds a metadata key-value pair.</summary>
    public ResultBuilder<TRequest, TResult> WithMetadata(string key, object value)
    {
        _metadata ??= new();
        _metadata[key] = value;
        return this;
    }

    /// <summary>Sets the metadata dictionary.</summary>
    public ResultBuilder<TRequest, TResult> WithMetadata(IReadOnlyDictionary<string, object>? metadata)
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
    public ResultBuilder<TRequest, TResult> WithMetadata(params (string key, object value)[] entries)
    {
        _metadata ??= new();
        foreach (var (key, value) in entries)
            _metadata[key] = value;

        return this;
    }

    /// <summary>Builds the Result instance.</summary>
    public Result<TRequest, TResult> Build() => new(_isSuccess, _request, _data, _errors) { Timestamp = _timestamp ?? DateTime.UtcNow, Metadata = _metadata };

    /// <summary>Implicit conversion to Result.</summary>
    public static implicit operator Result<TRequest, TResult>(ResultBuilder<TRequest, TResult> builder) => builder.Build();
}