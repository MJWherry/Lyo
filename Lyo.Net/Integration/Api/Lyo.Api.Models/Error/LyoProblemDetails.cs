using System.Diagnostics;
using System.Net;

namespace Lyo.Api.Models.Error;

/// <summary>RFC 9457 problem details with Lyo-specific <see cref="Errors" /> and tracing. Serialized with the default JSON contract (no custom converter).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record LyoProblemDetails(
    string Detail,
    int Status,
    DateTime Timestamp,
    IReadOnlyList<ApiError> Errors,
    string Title = "Request Failed",
    string Type = "about:blank",
    string? Instance = null,
    string? TraceId = null,
    string? SpanId = null,
    string? Stacktrace = null,
    Dictionary<string, object?>? Extensions = null) : ILyoProblemDetails
{
    /// <summary>
    /// Root <see cref="Detail"/> plus structured <see cref="Errors"/> descriptions when present.
    /// Prefer this for exception/UI surfaces so callers see validation entries, not only the summary.
    /// </summary>
    public string GetFullMessage()
    {
        if (Errors.Count == 0)
            return Detail;

        var errorText = string.Join("; ", Errors.Select(e => e.Description).Where(d => !string.IsNullOrWhiteSpace(d)));
        if (string.IsNullOrWhiteSpace(errorText))
            return Detail;

        // FromCode / single-error cases often duplicate Detail onto Errors[0].
        if (string.IsNullOrWhiteSpace(Detail)
            || Errors.Any(e => string.Equals(e.Description, Detail, StringComparison.Ordinal)))
            return errorText;

        return $"{Detail} {errorText}";
    }

    public int GetErrorDepth() => Math.Max(1, Errors.Count);

    public override string ToString() => $"{TraceId} - {Detail}, Stacktrace Available={!string.IsNullOrEmpty(Stacktrace)} {Timestamp:G}";

    /// <summary>Maps stable <see cref="Constants.ApiErrorCodes" /> values to HTTP status codes for problem responses.</summary>
    public static int MapErrorCodeToHttpStatus(string code)
        => code switch {
            Constants.ApiErrorCodes.NotFound => 404,
            Constants.ApiErrorCodes.Forbidden => 403,
            Constants.ApiErrorCodes.Unauthorized => 401,
            Constants.ApiErrorCodes.Conflict => 409,
            Constants.ApiErrorCodes.Gone => 410,
            Constants.ApiErrorCodes.UnprocessableEntity => 422,
            Constants.ApiErrorCodes.TooManyRequests => 429,
            Constants.ApiErrorCodes.Cancelled => 499,
            Constants.ApiErrorCodes.SqlException => 500,
            Constants.ApiErrorCodes.MessageQueueConnectionIssue => 503,
            Constants.ApiErrorCodes.ServiceUnavailable => 503,
            Constants.ApiErrorCodes.GatewayTimeout => 504,
            var _ => 400
        };

    /// <summary>Default <see cref="Constants.ApiErrorCodes" /> value for an HTTP status code, used when an exception carries no explicit error code.</summary>
    public static string MapHttpStatusToErrorCode(int statusCode)
        => statusCode switch {
            400 => Constants.ApiErrorCodes.InvalidRequest,
            401 => Constants.ApiErrorCodes.Unauthorized,
            403 => Constants.ApiErrorCodes.Forbidden,
            404 => Constants.ApiErrorCodes.NotFound,
            409 => Constants.ApiErrorCodes.Conflict,
            410 => Constants.ApiErrorCodes.Gone,
            422 => Constants.ApiErrorCodes.UnprocessableEntity,
            429 => Constants.ApiErrorCodes.TooManyRequests,
            499 => Constants.ApiErrorCodes.Cancelled,
            503 => Constants.ApiErrorCodes.ServiceUnavailable,
            504 => Constants.ApiErrorCodes.GatewayTimeout,
            var _ => Constants.ApiErrorCodes.Unknown
        };

    /// <summary>Single-code problem with optional trace, instance, and extensions (replaces target-typed <c>new(..., DateTime.UtcNow)</c>).</summary>
    public static LyoProblemDetails FromCode(
        string errorCode,
        string detail,
        DateTime? timestamp = null,
        string? traceId = null,
        string? instance = null,
        Dictionary<string, object?>? extensions = null)
    {
        var status = MapErrorCodeToHttpStatus(errorCode);
        return new(detail, status, timestamp ?? DateTime.UtcNow, [new(errorCode, detail)], Instance: instance, TraceId: traceId, Extensions: extensions);
    }

    /// <summary>HTTP status title for <see cref="MapErrorCodeToHttpStatus" /> (e.g. export wrapping).</summary>
    public static string HttpStatusTitle(int statusCode)
        => Enum.IsDefined(typeof(HttpStatusCode), statusCode) ? ((HttpStatusCode)statusCode).ToString().Replace('_', ' ') : "Request Failed";
}