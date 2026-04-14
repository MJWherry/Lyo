namespace Lyo.Geolocation.Models;

public class ProximitySearchResult<T>
    where T : class
{
    public T Item { get; set; }

    public double DistanceMeters { get; set; }

    public double DistanceKilometers => DistanceMeters / 1000;

    public double DistanceMiles => DistanceMeters * 0.000621371;
}