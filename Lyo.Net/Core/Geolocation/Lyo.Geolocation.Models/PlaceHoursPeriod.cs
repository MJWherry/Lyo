namespace Lyo.Geolocation.Models;

public class PlaceHoursPeriod
{
    public DayOfWeek Day { get; set; }

    public TimeSpan OpenTime { get; set; }

    public TimeSpan CloseTime { get; set; }
}