using Lyo.Result;

namespace Lyo.ShortUrl.Models;

/// <summary>Outcome of a URL shortening operation with URL shortening-specific properties.</summary>
public sealed record UrlShortenResult : Result<string>
{
    /// <summary>URL after shortening.</summary>
    public string? ShortUrl { get; init; }

    /// <summary>Original destination URL.</summary>
    public string? LongUrl { get; init; }

    /// <summary>Custom alias, when one was used.</summary>
    public string? Alias { get; init; }

    /// <summary>When the short URL was created.</summary>
    public DateTime? CreatedDate { get; init; }

    /// <summary>Short-URL expiry, if set.</summary>
    public DateTime? ExpirationDate { get; init; }

    /// <summary>Readable message that explains the outcome.</summary>
    public string? Message { get; init; }

    private UrlShortenResult(bool isSuccess, string? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Builds a successful UrlShortenResult with short URL.</summary>
    public static UrlShortenResult FromSuccess(
        string shortUrl,
        string longUrl,
        string? alias = null,
        DateTime? createdDate = null,
        DateTime? expirationDate = null,
        string? message = null)
        => new(true, shortUrl) {
            ShortUrl = shortUrl,
            LongUrl = longUrl,
            Alias = alias,
            CreatedDate = createdDate ?? DateTime.UtcNow,
            ExpirationDate = expirationDate,
            Message = message
        };

    /// <summary>Builds a failed UrlShortenResult from an exception.</summary>
    public static UrlShortenResult FromException(Exception exception, string? longUrl = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, null, [error]) { LongUrl = longUrl };
    }

    /// <summary>Produces a failed UrlShortenResult with a custom error message.</summary>
    public static UrlShortenResult FromError(string errorMessage, string errorCode, string? longUrl = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, null, [error]) { LongUrl = longUrl };
    }
}