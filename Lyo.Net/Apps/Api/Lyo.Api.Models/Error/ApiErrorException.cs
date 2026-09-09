namespace Lyo.Api.Models.Error;

/// <summary>
/// Thrown for intentional, caught API failures that carry structured <see cref="LyoProblemDetails" />.
/// API <c>LoggingMiddleware</c> catches this, logs at Warn, and writes <c>application/problem+json</c>.
/// </summary>
public sealed class ApiErrorException : Exception
{
    /// <summary>Problem details the middleware writes to the HTTP response.</summary>
    public LyoProblemDetails ProblemDetails { get; }

    /// <summary>HTTP status from <see cref="ProblemDetails" />.</summary>
    public int Status => ProblemDetails.Status;

    /// <summary>Builds an exception whose message is <see cref="LyoProblemDetails.GetFullMessage" /> so logs include field-level context.</summary>
    public ApiErrorException(LyoProblemDetails problemDetails)
        : base(problemDetails.GetFullMessage())
        => ProblemDetails = problemDetails;

    /// <summary>Builds an <see cref="ApiErrorException" /> from an existing problem.</summary>
    public static ApiErrorException From(LyoProblemDetails problem) => new(problem);
}
