namespace Lyo.ShortUrl;

/// <summary>Error codes used by URL shortener services.</summary>
public static class ShortUrlErrorCodes
{
    /// <summary>Failed to shorten URL.</summary>
    public const string ShortenFailed = "SHORTURL_SHORTEN_FAILED";

    /// <summary>Failed to expand URL.</summary>
    public const string ExpandFailed = "SHORTURL_EXPAND_FAILED";

    /// <summary>Failed to get URL statistics.</summary>
    public const string GetStatisticsFailed = "SHORTURL_GET_STATISTICS_FAILED";

    /// <summary>Failed to delete URL.</summary>
    public const string DeleteFailed = "SHORTURL_DELETE_FAILED";

    /// <summary>Failed to update URL.</summary>
    public const string UpdateFailed = "SHORTURL_UPDATE_FAILED";

    /// <summary>Operation was cancelled.</summary>
    public const string OperationCancelled = "SHORTURL_OPERATION_CANCELLED";

    /// <summary>URL not found.</summary>
    public const string UrlNotFound = "SHORTURL_URL_NOT_FOUND";

    /// <summary>URL has expired.</summary>
    public const string UrlExpired = "SHORTURL_URL_EXPIRED";

    /// <summary>Invalid URL format.</summary>
    public const string InvalidUrl = "SHORTURL_INVALID_URL";

    /// <summary>Custom alias already exists.</summary>
    public const string AliasAlreadyExists = "SHORTURL_ALIAS_ALREADY_EXISTS";

    /// <summary>Custom alias not allowed.</summary>
    public const string CustomAliasNotAllowed = "SHORTURL_CUSTOM_ALIAS_NOT_ALLOWED";

    /// <summary>Invalid alias length.</summary>
    public const string InvalidAliasLength = "SHORTURL_INVALID_ALIAS_LENGTH";
}