using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Google.Geolocation.Client;

internal static class GoogleMapsApiStatus
{
    public static void EnsureOk(string? status, string? errorMessage)
    {
        switch (status) {
            case "OK":
            case "ZERO_RESULTS":
                return;
            case null:
                throw new InvalidFormatException("Google Maps API response missing status");
            case "OVER_QUERY_LIMIT":
                throw new ServiceUnavailableException($"Google Maps API quota exceeded: {errorMessage}");
            case "REQUEST_DENIED":
            case "INVALID_REQUEST":
                throw new InvalidFormatException($"Google Maps API error ({status}): {errorMessage}");
            default:
                throw new ServiceUnavailableException($"Google Maps API returned status {status}: {errorMessage}");
        }
    }
}
