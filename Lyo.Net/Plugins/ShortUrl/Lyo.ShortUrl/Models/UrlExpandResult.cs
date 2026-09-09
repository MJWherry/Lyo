using Lyo.Result;

namespace Lyo.ShortUrl.Models;

/// <summary>Outcome of a URL expansion operation with URL expansion-specific properties.</summary>
public sealed record UrlExpandResult : Result<string>
{
    /// <summary>Original destination URL.</summary>
    public string? LongUrl { get; init; }

    /// <summary>Expanded short URL.</summary>
    public string? ShortUrl { get; init; }

    /// <summary>Indicates whether the URL has expired.</summary>
    public bool? IsExpired { get; init; }

    /// <summary>Readable message that explains the outcome.</summary>
    public string? Message { get; init; }

    private UrlExpandResult(bool isSuccess, string? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Produces a successful UrlExpandResult with long URL.</summary>
    public static UrlExpandResult FromSuccess(string longUrl, string? shortUrl = null, bool? isExpired = null, string? message = null)
        => new(true, longUrl) {
            LongUrl = longUrl,
            ShortUrl = shortUrl,
            IsExpired = isExpired,
            Message = message
        };

    /// <summary>Builds a failed UrlExpandResult from an exception.</summary>
    public static UrlExpandResult FromException(Exception exception, string? shortUrl = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, null, [error]) { ShortUrl = shortUrl };
    }

    /// <summary>Builds a failed UrlExpandResult with a custom error message.</summary>
    public static UrlExpandResult FromError(string errorMessage, string errorCode, string? shortUrl = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, null, [error]) { ShortUrl = shortUrl };
    }
}