using Lyo.Api.Models.Error;
using Lyo.Exceptions.Models;

namespace Lyo.Api.Client;

/// <summary>
/// Thrown when the API returns a non-success status code. Contains RFC 7807 problem details when the response body is parseable. Derives from <see cref="HttpException" /> so
/// callers can handle client errors with the shared hierarchy (status code, error code, transience).
/// </summary>
public sealed class ApiException : HttpException
{
    /// <summary>Gets the problem details when the response body was successfully parsed; otherwise null.</summary>
    public LyoProblemDetails? ProblemDetails { get; }

    /// <summary>Gets the full problem message (root detail plus structured <see cref="LyoProblemDetails.Errors" /> when present), or the exception message if ProblemDetails is null.</summary>
    public string Detail => ProblemDetails?.GetFullMessage() ?? Message;

    /// <summary>Gets whether the status code indicates a transient failure worth retrying (408, 429, 502, 503, 504).</summary>
    public override bool IsTransient => StatusCode is 408 or 429 or 502 or 503 or 504;

    /// <summary>Initializes a new instance of the <see cref="ApiException" /> class.</summary>
    public ApiException(int statusCode, string message, LyoProblemDetails? problemDetails = null, Exception? innerException = null)
        : base(statusCode, message, innerException)
        => ProblemDetails = problemDetails;
}