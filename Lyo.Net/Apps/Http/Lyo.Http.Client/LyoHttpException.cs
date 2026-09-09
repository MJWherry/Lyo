using Lyo.Exceptions.Models;

namespace Lyo.Http.Client;

/// <summary>Thrown on a non-success HTTP status for generic (non-Lyo-API) clients. Holds a truncated body; no problem-details parsing.</summary>
public sealed class LyoHttpException : HttpException
{
    /// <summary>Maximum characters kept from the response body.</summary>
    public const int BodyTruncateChars = 4096;

    /// <summary>Truncated response body, when read.</summary>
    public string? ResponseBody { get; }

    /// <inheritdoc />
    public override bool IsTransient => StatusCode is 408 or 429 or 502 or 503 or 504;

    /// <summary>Creates a <see cref="LyoHttpException" />.</summary>
    public LyoHttpException(int statusCode, string message, string? responseBody = null, Exception? innerException = null)
        : base(statusCode, message, innerException)
        => ResponseBody = Truncate(responseBody);

    /// <summary>Truncates <paramref name="body" /> to <see cref="BodyTruncateChars" />.</summary>
    public static string? Truncate(string? body)
    {
        if (body is not { Length: > BodyTruncateChars })
            return body;

        return body.Substring(0, BodyTruncateChars);
    }
}
