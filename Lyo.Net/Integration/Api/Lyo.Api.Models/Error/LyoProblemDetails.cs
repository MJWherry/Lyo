using System.Diagnostics;
using System.Linq;
using System.Net;
using Lyo.Api.Models;

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
    public string GetFullMessage()
        => Errors.Count > 0
            ? string.Join(" -> ", Errors.Select(e => e.Description))
            : Detail;

    public int GetErrorDepth() => Math.Max(1, Errors.Count);

    public override string ToString() => $"{TraceId} - {Detail}, Stacktrace Available={!string.IsNullOrEmpty(Stacktrace)} {Timestamp:G}";

    /// <summary>Maps stable <see cref="Constants.ApiErrorCodes" /> values to HTTP status codes for problem responses.</summary>
    public static int MapErrorCodeToHttpStatus(string code) => code switch
    {
        Constants.ApiErrorCodes.NotFound => 404,
        Constants.ApiErrorCodes.Forbidden => 403,
        Constants.ApiErrorCodes.Cancelled => 499,
        Constants.ApiErrorCodes.SqlException => 500,
        Constants.ApiErrorCodes.MessageQueueConnectionIssue => 503,
        _ => 400
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
        return new(
            detail,
            status,
            timestamp ?? DateTime.UtcNow,
            [new ApiError(errorCode, detail, null)],
            Instance: instance,
            TraceId: traceId,
            Extensions: extensions);
    }

    /// <summary>HTTP status title for <see cref="MapErrorCodeToHttpStatus" /> (e.g. export wrapping).</summary>
    public static string HttpStatusTitle(int statusCode)
        => Enum.IsDefined(typeof(HttpStatusCode), statusCode)
            ? ((HttpStatusCode)statusCode).ToString().Replace('_', ' ')
            : "Request Failed";
}
