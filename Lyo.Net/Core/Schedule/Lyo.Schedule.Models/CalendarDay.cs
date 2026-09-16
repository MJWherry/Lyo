using Lyo.DateAndTime;

namespace Lyo.Schedule.Models;

/// <summary>One civil day in a <see cref="Calendar" /> and the entries that intersect it.</summary>
/// <param name="Date">Civil date in the calendar's time zone.</param>
/// <param name="Entries">Entries that intersect this day, in start order. Empty when the day is clear.</param>
public sealed record CalendarDay(DateOnlyModel Date, IReadOnlyList<CalendarEntry> Entries);
