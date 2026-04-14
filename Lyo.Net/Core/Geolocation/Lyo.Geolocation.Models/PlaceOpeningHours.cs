namespace Lyo.Geolocation.Models;

public class PlaceOpeningHours
{
    public bool IsOpen { get; set; }

    public IEnumerable<string> WeekdayText { get; set; }

    public IEnumerable<PlaceHoursPeriod> Periods { get; set; }
}