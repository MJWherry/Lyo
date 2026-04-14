using Lyo.Common;

namespace Lyo.ShortUrl.Models;

/// <summary>Result of a URL statistics retrieval operation with statistics-specific properties.</summary>
public sealed record UrlStatisticsResult : Result<UrlStatistics>
{
    /// <summary>The short URL.</summary>
    public string? ShortUrl { get; init; }

    /// <summary>The long URL.</summary>
    public string? LongUrl { get; init; }

    /// <summary>A human-readable message describing the result.</summary>
    public string? Message { get; init; }

    private UrlStatisticsResult(bool isSuccess, UrlStatistics? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful UrlStatisticsResult with statistics.</summary>
    public static UrlStatisticsResult FromSuccess(UrlStatistics statistics, string? shortUrl = null, string? longUrl = null, string? message = null)
        => new(true, statistics) { ShortUrl = shortUrl ?? statistics.ShortUrl, LongUrl = longUrl ?? statistics.LongUrl, Message = message };

    /// <summary>Creates a failed UrlStatisticsResult from an exception.</summary>
    public static UrlStatisticsResult FromException(Exception exception, string? shortUrl = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, null, [error]) { ShortUrl = shortUrl };
    }

    /// <summary>Creates a failed UrlStatisticsResult with a custom error message.</summary>
    public static UrlStatisticsResult FromError(string errorMessage, string errorCode, string? shortUrl = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, null, [error]) { ShortUrl = shortUrl };
    }
}