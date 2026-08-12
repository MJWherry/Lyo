namespace Lyo.Api.Models.Error;

/// <summary>
/// Thrown for intentional/caught API failures carrying structured <see cref="LyoProblemDetails" />.
/// API <c>LoggingMiddleware</c> catches this, logs at Warn, and writes <c>application/problem+json</c>.
/// </summary>
public sealed class ApiErrorException : Exception
{
    /// <summary>Problem details written to the HTTP response by middleware.</summary>
    public LyoProblemDetails ProblemDetails { get; }

    /// <summary>HTTP status from <see cref="ProblemDetails" />.</summary>
    public int Status => ProblemDetails.Status;

    /// <summary>Creates an exception whose message is <see cref="LyoProblemDetails.GetFullMessage" /> so logs include field-level context.</summary>
    public ApiErrorException(LyoProblemDetails problemDetails)
        : base(problemDetails.GetFullMessage())
        => ProblemDetails = problemDetails;

    /// <summary>Creates an <see cref="ApiErrorException" /> from an existing problem.</summary>
    public static ApiErrorException From(LyoProblemDetails problem) => new(problem);
}
