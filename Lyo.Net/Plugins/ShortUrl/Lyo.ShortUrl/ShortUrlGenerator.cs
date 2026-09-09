using Lyo.Common.Core.Security;

namespace Lyo.ShortUrl;

/// <summary>Stock implementation of short URL generator service.</summary>
public sealed class ShortUrlGenerator : IShortUrlGenerator
{
    private const string DefaultChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int DefaultLength = 8;

    /// <summary>Mints a unique short-URL id.</summary>
    /// <param name="length">Optional generated-id length; the default is used when omitted.</param>
    /// <returns>Newly minted short-URL id.</returns>
    public string Generate(int? length = null) => CryptographicRandom.GetString(length ?? DefaultLength, DefaultChars);
}
