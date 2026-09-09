using Lyo.Api.Models.Error;
using Lyo.Exceptions.Models;

namespace Lyo.Api.Client;

/// <summary>
/// Thrown on a non-success API status. Holds RFC 7807 problem details when the body parses. Extends <see cref="HttpException" /> so
/// callers share the same hierarchy (status code, error code, transience).
/// </summary>
public sealed class ApiException : HttpException
{
    /// <summary>Parsed problem details, or null if the body could not be parsed.</summary>
    public LyoProblemDetails? ProblemDetails { get; }

    /// <summary>
    /// Full problem text (root detail plus structured <see cref="LyoProblemDetails.Errors" /> when present), or the exception message when ProblemDetails is null.
    /// </summary>
    public string Detail => ProblemDetails?.GetFullMessage() ?? Message;

    /// <summary>True when the status looks transient and worth retrying (408, 429, 502, 503, 504).</summary>
    public override bool IsTransient => StatusCode is 408 or 429 or 502 or 503 or 504;

    /// <summary>Creates an <see cref="ApiException" />.</summary>
    public ApiException(int statusCode, string message, LyoProblemDetails? problemDetails = null, Exception? innerException = null)
        : base(statusCode, message, innerException)
        => ProblemDetails = problemDetails;
}