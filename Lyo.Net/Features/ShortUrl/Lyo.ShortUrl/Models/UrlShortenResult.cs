using Lyo.Common;

namespace Lyo.ShortUrl.Models;

/// <summary>Result of a URL shortening operation with URL shortening-specific properties.</summary>
public sealed record UrlShortenResult : Result<string>
{
    /// <summary>The shortened URL.</summary>
    public string? ShortUrl { get; init; }

    /// <summary>The original/long URL.</summary>
    public string? LongUrl { get; init; }

    /// <summary>The custom alias used (if any).</summary>
    public string? Alias { get; init; }

    /// <summary>The creation date of the short URL.</summary>
    public DateTime? CreatedDate { get; init; }

    /// <summary>The expiration date of the short URL (if any).</summary>
    public DateTime? ExpirationDate { get; init; }

    /// <summary>A human-readable message describing the result.</summary>
    public string? Message { get; init; }

    private UrlShortenResult(bool isSuccess, string? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful UrlShortenResult with short URL.</summary>
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

    /// <summary>Creates a failed UrlShortenResult from an exception.</summary>
    public static UrlShortenResult FromException(Exception exception, string? longUrl = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, null, [error]) { LongUrl = longUrl };
    }

    /// <summary>Creates a failed UrlShortenResult with a custom error message.</summary>
    public static UrlShortenResult FromError(string errorMessage, string errorCode, string? longUrl = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, null, [error]) { LongUrl = longUrl };
    }
}