namespace Lyo.ShortUrl;

/// <summary>Contract for generating short URL identifiers.</summary>
public interface IShortUrlGenerator
{
    /// <summary>Mints a unique short-URL id.</summary>
    /// <param name="length">Optional generated-id length; the default is used when omitted.</param>
    /// <returns>Newly minted short-URL id.</returns>
    string Generate(int? length = null);
}