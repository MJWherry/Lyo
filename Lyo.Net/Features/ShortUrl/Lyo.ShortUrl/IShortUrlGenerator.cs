namespace Lyo.ShortUrl;

/// <summary>Service interface for generating short URL identifiers.</summary>
public interface IShortUrlGenerator
{
    /// <summary>Generates a unique short URL identifier.</summary>
    /// <param name="length">The desired length of the generated ID (optional, uses default if not specified).</param>
    /// <returns>A generated short URL identifier.</returns>
    string Generate(int? length = null);
}