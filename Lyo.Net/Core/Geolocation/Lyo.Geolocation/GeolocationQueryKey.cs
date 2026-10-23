using System.Security.Cryptography;
using Lyo.Common.Core.Conversion;
using Lyo.Common.Core.Enums;
using Lyo.Exceptions;
using Lyo.Geolocation.Models.Addresses;

namespace Lyo.Geolocation;

/// <summary>Turns geocode inputs into stable cache keys.</summary>
public static class GeolocationQueryKey
{
    /// <summary>Builds a cache key from a free-form address string.</summary>
    public static string FromAddressString(string address)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(address);
        return Normalize(address);
    }

    /// <summary>Builds a cache key from a structured address.</summary>
    public static string FromAddress(Address address)
    {
        ArgumentHelpers.ThrowIfNull(address);
        return Normalize(address.GetCanonicalForm());
    }

    /// <summary>Builds a reverse-geocode cache key (coordinate rounded to about 1 m).</summary>
    public static string FromCoordinate(double latitude, double longitude)
    {
        var lat = Math.Round(latitude, 5);
        var lng = Math.Round(longitude, 5);
        return Normalize($"{lat},{lng}");
    }

    private static string Normalize(string value)
    {
        var trimmed = value.Trim().ToUpperInvariant();
        var bytes = ByteConvert.GetBytes(trimmed);
#if NET5_0_OR_GREATER
        var hash = SHA256.HashData(bytes);
#else
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
#endif
        return ByteConvert.ToHex(hash, TextLetterCase.Lower);
    }
}