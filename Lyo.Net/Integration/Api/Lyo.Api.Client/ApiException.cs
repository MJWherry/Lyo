using Lyo.Api.Models.Error;

namespace Lyo.Api.Client;

/// <summary>Thrown when the API returns a non-success status code. Contains RFC 7807 problem details when the response body is parseable.</summary>
public sealed class ApiException : Exception
{
    /// <summary>Gets the HTTP status code from the response.</summary>
    public int StatusCode { get; }

    /// <summary>Gets the problem details when the response body was successfully parsed; otherwise null.</summary>
    public LyoProblemDetails? ProblemDetails { get; }

    /// <summary>Gets the detail message from ProblemDetails, or the exception message if ProblemDetails is null.</summary>
    public string Detail => ProblemDetails?.Detail ?? Message;

    /// <summary>Initializes a new instance of the <see cref="ApiException" /> class.</summary>
    public ApiException(int statusCode, string message, LyoProblemDetails? problemDetails = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ProblemDetails = problemDetails;
    }
}
