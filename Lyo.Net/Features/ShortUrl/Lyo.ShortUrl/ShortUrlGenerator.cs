using System.Security.Cryptography;

namespace Lyo.ShortUrl;

/// <summary>Default implementation of short URL generator service.</summary>
public sealed class ShortUrlGenerator : IShortUrlGenerator
{
    private const string DefaultChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int DefaultLength = 8;
    private readonly RandomNumberGenerator _rng;

    /// <summary>Initializes a new instance of the <see cref="ShortUrlGenerator" /> class.</summary>
    public ShortUrlGenerator() => _rng = RandomNumberGenerator.Create();

    /// <summary>Generates a unique short URL identifier.</summary>
    /// <param name="length">The desired length of the generated ID (optional, uses default if not specified).</param>
    /// <returns>A generated short URL identifier.</returns>
    public string Generate(int? length = null)
    {
        var idLength = length ?? DefaultLength;
        var bytes = new byte[idLength];
        _rng.GetBytes(bytes);
        return new(bytes.Select(b => DefaultChars[b % DefaultChars.Length]).ToArray());
    }
}