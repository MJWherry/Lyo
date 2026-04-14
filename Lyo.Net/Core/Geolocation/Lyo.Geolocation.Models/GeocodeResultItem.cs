namespace Lyo.Geolocation.Models;

public class GeocodeResultItem
{
    public int Index { get; set; }

    public string OriginalQuery { get; set; }

    public bool IsSuccess { get; set; }

    public GeocodeResult Result { get; set; }

    public string ErrorMessage { get; set; }
}