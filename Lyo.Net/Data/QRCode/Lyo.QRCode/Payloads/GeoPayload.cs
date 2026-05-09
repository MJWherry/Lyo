using System.Diagnostics;
using System.Globalization;
using Lyo.Exceptions;

namespace Lyo.QRCode.Payloads;

/// <summary><c>geo:</c> latitude/longitude URI.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class GeoPayload : IQrPayload
{
    /// <summary>Creates a geo payload from decimal degrees.</summary>
    /// <param name="latitude">Latitude in [-90, 90].</param>
    /// <param name="longitude">Longitude in [-180, 180].</param>
    /// <param name="queryLabel">Optional human label (maps to <c>?q=</c> for some clients).</param>
    public GeoPayload(double latitude, double longitude, string? queryLabel = null)
    {
        Latitude = latitude;
        Longitude = longitude;
        QueryLabel = queryLabel?.Trim();
    }

    /// <summary>Latitude in decimal degrees.</summary>
    public double Latitude { get; }

    /// <summary>Longitude in decimal degrees.</summary>
    public double Longitude { get; }

    /// <summary>Optional query label.</summary>
    public string? QueryLabel { get; }

    /// <inheritdoc />
    public override string ToString()
        => QueryLabel is null
            ? $"GeoPayload ({Latitude}, {Longitude})"
            : $"GeoPayload ({Latitude}, {Longitude}), label={QueryLabel}";

    /// <inheritdoc />
    public string ToQrString()
    {
        ArgumentHelpers.ThrowIfNotInRange(Latitude, -90, 90, nameof(Latitude), "Latitude must be between -90 and 90.");
        ArgumentHelpers.ThrowIfNotInRange(Longitude, -180, 180, nameof(Longitude), "Longitude must be between -180 and 180.");

        var lat = Latitude.ToString(CultureInfo.InvariantCulture);
        var lon = Longitude.ToString(CultureInfo.InvariantCulture);
        var core = "geo:" + lat + "," + lon;

        if (string.IsNullOrEmpty(QueryLabel))
            return core;

        return core + "?q=" + Uri.EscapeDataString(QueryLabel);
    }
}
