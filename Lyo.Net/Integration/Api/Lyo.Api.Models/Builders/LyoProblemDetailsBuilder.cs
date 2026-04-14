using System.Collections.Generic;
using System.Diagnostics;
using Lyo.Api.Models.Error;
using Lyo.Exceptions;

namespace Lyo.Api.Models.Builders;

/// <summary>Fluent builder for <see cref="LyoProblemDetails" /> with optional tracing and structured <c>errors</c> entries.</summary>
public sealed class LyoProblemDetailsBuilder
{
    /// <summary>Default root <see cref="LyoProblemDetails.Detail" /> when the problem is driven only by <see cref="LyoProblemDetails.Errors" />.</summary>
    public const string DefaultValidationDetailSummary = "One or more validation errors occurred.";

    private string _errorCode = Constants.ApiErrorCodes.Unknown;
    private readonly List<ApiError> _errors = [];
    private string _detail = string.Empty;
    private string? _spanId;
    private DateTime? _timestamp;
    private string? _traceId;
    private string? _instance;
    private int? _status;
    private string _title = "Request Failed";
    private string _type = "about:blank";
    private Dictionary<string, object?> _extensions = [];

    public static LyoProblemDetailsBuilder Create() => new();

    public static LyoProblemDetailsBuilder CreateWithActivity()
    {
#if Net7_0_OR_GREATER
        var activity = Activity.Current;
        return new LyoProblemDetailsBuilder()
            .WithTrace(activity?.TraceId.ToString(), activity?.SpanId.ToString());
#else
        return new();
#endif
    }

    public static LyoProblemDetailsBuilder FromException(Exception ex, string? errorCode = null)
    {
        var code = errorCode
            ?? (ex is LFException lf ? lf.ErrorCode : null)
            ?? Constants.ApiErrorCodes.Unknown;
        var b = CreateWithActivity().WithErrorCode(code);
        for (Exception? e = ex; e != null; e = e.InnerException)
            b.AddApiError(code, e.Message, e.StackTrace);

        return b.WithDetail(ex.Message);
    }

    public static LyoProblemDetailsBuilder CreateWithTrace(string? traceId, string? spanId = null) => new LyoProblemDetailsBuilder().WithTrace(traceId, spanId);

    /// <summary>Sets distributed trace and span identifiers (e.g. from <see cref="Activity" /> or headers).</summary>
    public LyoProblemDetailsBuilder WithTrace(string? traceId, string? spanId = null)
    {
        _traceId = traceId;
        _spanId = spanId;
        return this;
    }

    /// <summary>Sets RFC 7807 <c>instance</c> (often the request path or URI).</summary>
    public LyoProblemDetailsBuilder WithRoute(string? instance) => WithInstance(instance);

    public LyoProblemDetailsBuilder WithInstance(string? instance)
    {
        _instance = instance;
        return this;
    }

    public LyoProblemDetailsBuilder WithTraceId(string? traceId)
    {
        _traceId = traceId;
        return this;
    }

    public LyoProblemDetailsBuilder WithSpanId(string? spanId)
    {
        _spanId = spanId;
        return this;
    }

    public LyoProblemDetailsBuilder WithErrorCode(string errorCode)
    {
        _errorCode = errorCode;
        return this;
    }

    /// <summary>Root problem summary (RFC <c>detail</c>).</summary>
    public LyoProblemDetailsBuilder WithDetail(string detail)
    {
        _detail = ArgumentHelpers.ThrowIfNullReturn(detail, nameof(detail));
        return this;
    }

    /// <summary>Same as <see cref="WithDetail" /> — root summary text.</summary>
    public LyoProblemDetailsBuilder WithMessage(string message) => WithDetail(message);

    public LyoProblemDetailsBuilder WithTitle(string title)
    {
        _title = ArgumentHelpers.ThrowIfNullReturn(title, nameof(title));
        return this;
    }

    public LyoProblemDetailsBuilder WithStatus(int status)
    {
        _status = status;
        return this;
    }

    public LyoProblemDetailsBuilder WithType(string type)
    {
        _type = ArgumentHelpers.ThrowIfNullReturn(type, nameof(type));
        return this;
    }

    public LyoProblemDetailsBuilder WithTimestamp(DateTime timestamp)
    {
        _timestamp = timestamp;
        return this;
    }

    public LyoProblemDetailsBuilder WithExtensions(Dictionary<string, object?> extensions)
    {
        _extensions = extensions;
        return this;
    }

    public LyoProblemDetailsBuilder AddApiError(string code, string description, string? stacktrace = null)
    {
        ArgumentHelpers.ThrowIfNullReturn(description, nameof(description));
        _errors.Add(new ApiError(code, description, stacktrace));
        return this;
    }

    public LyoProblemDetailsBuilder WithInvalidField(string fieldName)
    {
        ArgumentHelpers.ThrowIfNullReturn(fieldName, nameof(fieldName));
        return AddApiError(Constants.ApiErrorCodes.InvalidField, $"Field '{fieldName}' is not valid.");
    }

    public LyoProblemDetailsBuilder WithInvalidSelectField(string fieldName)
    {
        ArgumentHelpers.ThrowIfNullReturn(fieldName, nameof(fieldName));
        return AddApiError(Constants.ApiErrorCodes.InvalidSelectField, $"Field '{fieldName}' is not valid.");
    }

    public LyoProblemDetailsBuilder AddErrors(IEnumerable<ApiError> errors)
    {
        ArgumentHelpers.ThrowIfNullReturn(errors, nameof(errors));
        foreach (var i in errors)
            _errors.Add(i);

        return this;
    }

    /// <summary>Field validation helper using <see cref="Constants.ApiErrorCodes.InvalidField" />.</summary>
    public LyoProblemDetailsBuilder AddValidation(string fieldName, string error)
    {
        ArgumentHelpers.ThrowIfNullReturn(fieldName, nameof(fieldName));
        ArgumentHelpers.ThrowIfNullReturn(error, nameof(error));
        return AddApiError(Constants.ApiErrorCodes.InvalidField, $"{fieldName}: {error}");
    }

    public LyoProblemDetails Build()
    {
        var detail = _detail;
        if (_errors.Count > 0) {
            if (string.IsNullOrEmpty(detail))
                detail = DefaultValidationDetailSummary;
        }
        else if (string.IsNullOrEmpty(detail)) {
            OperationHelpers.ThrowIfNullOrEmpty(_detail, "Detail (or WithMessage) is required to build LyoProblemDetails when there are no errors.");
        }

        IReadOnlyList<ApiError> errors = _errors.Count > 0 ? _errors : [new ApiError(_errorCode, detail, null)];

        var status = _status ?? LyoProblemDetails.MapErrorCodeToHttpStatus(_errorCode);

        return new LyoProblemDetails(
            detail,
            status,
            _timestamp ?? DateTime.UtcNow,
            errors,
            _title,
            _type,
            _instance,
            _traceId,
            _spanId,
            null,
            _extensions.Count > 0 ? new Dictionary<string, object?>(_extensions) : null);
    }

    public static implicit operator LyoProblemDetails(LyoProblemDetailsBuilder builder) => builder.Build();
}

/// <summary>Extension methods for common error scenarios.</summary>
public static class LyoProblemDetailsBuilderExtensions
{
    public static LyoProblemDetailsBuilder NotFound(this LyoProblemDetailsBuilder builder, string resourceType, string resourceId)
        => builder.WithErrorCode(Constants.ApiErrorCodes.NotFound).WithMessage($"{resourceType} with ID '{resourceId}' was not found");

    public static LyoProblemDetailsBuilder InvalidOperation(this LyoProblemDetailsBuilder builder, string operation, string reason)
        => builder.WithErrorCode(Constants.ApiErrorCodes.InvalidOperation).WithMessage($"Invalid operation '{operation}': {reason}");

    public static LyoProblemDetailsBuilder SqlError(this LyoProblemDetailsBuilder builder, string operation)
        => builder.WithErrorCode(Constants.ApiErrorCodes.SqlException).WithMessage($"Database error occurred during {operation}");

    public static LyoProblemDetailsBuilder ExceedsBulkLimit(this LyoProblemDetailsBuilder builder, int actual, int maximum)
        => builder.WithErrorCode(Constants.ApiErrorCodes.ExceedMaxBulkSize).WithMessage($"Bulk operation size {actual} exceeds maximum allowed size of {maximum}");

    public static LyoProblemDetailsBuilder MessageQueueError(this LyoProblemDetailsBuilder builder, string queueName, string? details = null)
    {
        var message = $"Failed to connect to message queue '{queueName}'";
        if (!string.IsNullOrEmpty(details))
            message += $": {details}";

        return builder.WithErrorCode(Constants.ApiErrorCodes.MessageQueueConnectionIssue).WithMessage(message);
    }
}
