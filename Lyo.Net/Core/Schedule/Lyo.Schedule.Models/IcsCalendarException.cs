namespace Lyo.Schedule.Models;

/// <summary>An iCalendar document could not be read or its recurrence could not be expanded inside the cap.</summary>
public sealed class IcsCalendarException(string message) : Exception(message);
