namespace Lyo.Api.Models.Error;

/// <summary>Thrown when an operation fails with structured <see cref="LyoProblemDetails" /> (e.g. export after a failed projected query).</summary>
public sealed class ApiErrorException : Exception
{
    public LyoProblemDetails ProblemDetails { get; }

    public ApiErrorException(LyoProblemDetails problemDetails)
        : base(problemDetails.Detail) => ProblemDetails = problemDetails;
}
