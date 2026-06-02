using System.Diagnostics;

namespace Lyo.Geolocation.Models;

[DebuggerDisplay("{ToString(),nq}")]
public class PlaceOpeningHours
{
    public bool IsOpen { get; set; }

    public IEnumerable<string>? WeekdayText { get; set; }

    public IEnumerable<PlaceHoursPeriod>? Periods { get; set; }

    public override string ToString() => $"PlaceOpeningHours: open={IsOpen}, periods={Periods?.Count() ?? 0}";
}