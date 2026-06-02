using System.Diagnostics;

namespace Lyo.Geolocation.Models;

[DebuggerDisplay("{ToString(),nq}")]
public class PlaceHoursPeriod
{
    public DayOfWeek Day { get; set; }

    public TimeSpan OpenTime { get; set; }

    public TimeSpan CloseTime { get; set; }

    public override string ToString()
        => $"PlaceHoursPeriod: {Day} {OpenTime:hh\\:mm}-{CloseTime:hh\\:mm}";
}
