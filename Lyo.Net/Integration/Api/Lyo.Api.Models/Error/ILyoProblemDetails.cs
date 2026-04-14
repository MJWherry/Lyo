namespace Lyo.Api.Models.Error;

public interface ILyoProblemDetails
{
    string Type { get; init; }

    string Title { get; init; }

    int Status { get; init; }

    string Detail { get; init; }

    string? Instance { get; init; }

    IReadOnlyList<ApiError> Errors { get; init; }

    string? TraceId { get; init; }

    string? Stacktrace { get; init; }

    string? SpanId { get; init; }

    DateTime Timestamp { get; init; }

    /// <summary>Optional RFC 7807 extension members (e.g. <c>keys</c>).</summary>
    Dictionary<string, object?>? Extensions { get; init; }
}
