# Lyo.DateAndTime

A production-ready date and time utility library for .NET with timezone conversion, scheduling, and US state timezone
mapping support.

## DateOnly and TimeOnly on .NET Standard 2.0

This library targets **`netstandard2.0`** (and **`net10.0`**). The BCL types **`System.DateOnly`** and **`System.TimeOnly`** are not available on .NET Standard 2.0; they were introduced in .NET 6.

For that surface area, the library provides **`DateOnlyModel`** and **`TimeOnlyModel`**: implementations aligned with the `System.DateOnly` / `System.TimeOnly` APIs so consumers on `netstandard2.0` get the same date-only and time-of-day behavior. Scheduling helpers on `DateAndTime` use `System.TimeOnly` when building for .NET 6 or later, and `TimeOnlyModel` when building for .NET Standard 2.0.

## Features

- ✅ **Timezone Conversion** - Convert between UTC and US state local times
- ✅ **US State Mapping** - Automatic timezone mapping for all US states
- ✅ **Scheduling** - Find next scheduled times and check if schedules are past due
- ✅ **Day Flags** - Flexible day-of-week scheduling with flags (weekdays, weekends, specific days)
- ✅ **Time Windows** - Schedule within time windows with configurable intervals
- ✅ **Cross-Platform** - Supports .NET Standard 2.0 and .NET 10.0
- ✅ **DateOnly/TimeOnly Support** - `DateOnlyModel` / `TimeOnlyModel` on .NET Standard 2.0; see [DateOnly and TimeOnly on .NET Standard 2.0](#dateonly-and-timeonly-on-net-standard-20). Scheduling APIs use `System.TimeOnly` on .NET 6+.
- ✅ **JSON Serialization** - Built-in JSON converters for DateOnlyModel and TimeOnlyModel
- ✅ **Simple API** - Easy to use static utility class, no instantiation required
- ✅ **Error Handling** - Comprehensive error handling with meaningful exceptions
- ✅ **Input Validation** - Automatic validation of method parameters

## Quick Start

### Use the Static Utility Class

```csharp
// Get current local time for New York
var nyTime = DateAndTime.GetCurrentLocalDateTime(USState.NY);

// Convert UTC to local time
var utcTime = DateTime.UtcNow;
var localTime = DateAndTime.ConvertToLocalTime(USState.CA, utcTime);

// Convert local time to UTC
var localDateTime = new DateTime(2024, 1, 15, 10, 0, 0);
var utcDateTime = DateAndTime.ConvertToUtc(USState.TX, localDateTime);
```

## Core Concepts

### US States and Timezones

The library maps all US states to their respective timezones:

- **Eastern Time Zone** - NY, FL, MA, GA, etc.
- **Central Time Zone** - TX, IL, MO, etc.
- **Mountain Time Zone** - CO, AZ, UT, etc.
- **Pacific Time Zone** - CA, WA, OR, NV
- **Alaska Time Zone** - AK, HI

### Day Flags

Use `DayFlags` enum to specify which days of the week are scheduled:

```csharp
// Weekdays only (Monday-Friday)
DayFlags.Weekdays

// Weekends only (Saturday-Sunday)
DayFlags.Weekends

// Every day
DayFlags.EveryDay

// Specific days
DayFlags.Mon | DayFlags.Wed | DayFlags.Fri

// Custom combination
DayFlags.Mon | DayFlags.Tue | DayFlags.Thu
```

## API Reference

### Timezone Conversion

#### GetCurrentLocalDateTime

Gets the current local DateTime for a given US state.

```csharp
DateTime? GetCurrentLocalDateTime(USState usStateAbbreviation)
```

**Example:**

```csharp
var nyTime = DateAndTime.GetCurrentLocalDateTime(USState.NY);
```

#### ConvertToLocalTime

Converts a UTC DateTime to local time for a given US state.

```csharp
DateTime? ConvertToLocalTime(USState usStateAbbreviation, DateTime utcDateTime)
```

**Example:**

```csharp
var utcTime = new DateTime(2024, 1, 15, 15, 0, 0, DateTimeKind.Utc);
var localTime = DateAndTime.ConvertToLocalTime(USState.CA, utcTime);
```

#### ConvertToUtc

Converts a local DateTime to UTC for a given US state.

```csharp
DateTime? ConvertToUtc(USState usStateAbbreviation, DateTime localDateTime)
```

**Example:**

```csharp
var localTime = new DateTime(2024, 1, 15, 10, 0, 0);
var utcTime = DateAndTime.ConvertToUtc(USState.NY, localTime);
```

### Scheduling

#### GetNextScheduledDateTime (List of Times)

Gets the next scheduled DateTime based on a list of times and day flags.

```csharp
DateTime GetNextScheduledDateTime(
    USState usStateAbbreviation, 
    IEnumerable<TimeOnly> scheduleTimes, 
    DayFlags scheduleFlags)
```

**Example:**

```csharp
var scheduleTimes = new List<TimeOnly>
{
    new TimeOnly(9, 0),  // 9:00 AM
    new TimeOnly(14, 0), // 2:00 PM
    new TimeOnly(18, 0)  // 6:00 PM
};

var nextTime = DateAndTime.GetNextScheduledDateTime(
    USState.NY, 
    scheduleTimes, 
    DayFlags.Weekdays);
```

#### GetNextScheduledDateTime (Time Window)

Gets the next scheduled DateTime within a time window with intervals.

```csharp
DateTime GetNextScheduledDateTime(
    USState usStateAbbreviation, 
    TimeOnly startTime, 
    TimeOnly endTime, 
    int intervalMinutes, 
    DayFlags scheduleFlags)
```

**Example:**

```csharp
var nextTime = DateAndTime.GetNextScheduledDateTime(
    USState.CA,
    new TimeOnly(9, 0),   // Start at 9:00 AM
    new TimeOnly(17, 0),  // End at 5:00 PM
    60,                   // Every 60 minutes
    DayFlags.Weekdays);
```

#### IsPastDue (List of Times)

Checks if a scheduled job is past due based on schedule times and last run time.

```csharp
bool IsPastDue(
    USState usStateAbbreviation, 
    List<TimeOnly> scheduleTimes, 
    DayFlags scheduleFlags, 
    DateTime? lastRunDateTime)
```

**Example:**

```csharp
var scheduleTimes = new List<TimeOnly> { new TimeOnly(9, 0) };
var lastRun = DateTime.UtcNow.AddDays(-2);

var isPastDue = DateAndTime.IsPastDue(
    USState.NY,
    scheduleTimes,
    DayFlags.Weekdays,
    lastRun);
```

#### IsPastDue (Time Window)

Checks if a scheduled job is past due within a time window with intervals.

```csharp
bool IsPastDue(
    USState usStateAbbreviation, 
    TimeOnly startTime, 
    TimeOnly endTime, 
    int minuteInterval, 
    DayFlags scheduleFlags, 
    DateTime? lastRunDateTime)
```

**Example:**

```csharp
var isPastDue = DateAndTime.IsPastDue(
    USState.CA,
    new TimeOnly(9, 0),
    new TimeOnly(17, 0),
    60,
    DayFlags.Weekdays,
    DateTime.UtcNow.AddHours(-3));
```

#### GetScheduledTimesForDay

Gets all scheduled times for a given day.

```csharp
IEnumerable<DateTime> GetScheduledTimesForDay(
    USState usStateAbbreviation, 
    DateTime date, 
    IEnumerable<TimeOnly> scheduleTimes, 
    DayFlags scheduleFlags)

IEnumerable<DateTime> GetScheduledTimesForDay(
    USState usStateAbbreviation, 
    DateTime date, 
    TimeOnly startTime, 
    TimeOnly endTime, 
    int intervalMinutes, 
    DayFlags scheduleFlags)
```

**Example:**

```csharp
var date = new DateTime(2024, 1, 15); // Monday
var scheduleTimes = new List<TimeOnly> { new TimeOnly(9, 0), new TimeOnly(15, 0) };

var times = DateAndTime.GetScheduledTimesForDay(
    USState.NY,
    date,
    scheduleTimes,
    DayFlags.Mon);
```

#### IsScheduledDay

Checks if a given DateTime falls on a scheduled day.

```csharp
bool IsScheduledDay(DateTime dateTime, DayFlags scheduleFlags)
```

**Example:**

```csharp
var date = new DateTime(2024, 1, 15); // Monday
var isScheduled = DateAndTime.IsScheduledDay(date, DayFlags.Weekdays); // true
```

### Timezone Utilities

#### GetTimeZoneByState

Gets the TimeZoneInfo for a given US state abbreviation.

```csharp
TimeZoneInfo? GetTimeZoneByState(USState usStateAbbreviation)
```

**Example:**

```csharp
var timezone = DateAndTime.GetTimeZoneByState(USState.NY);
```

## Configuration

The utility class uses default values for look-ahead and past-due checking:

- **MaxDaysLookAhead**: 7 days (when finding next scheduled time)
- **MaxDaysPastDueCheck**: 7 days (when checking if schedule is past due)

These are internal constants and cannot be configured. If you need different values, you can create wrapper methods or extend the functionality.

## JSON Serialization

The library includes JSON converters for `DateOnlyModel` and `TimeOnlyModel` (used on .NET Standard 2.0):

```csharp
var options = new JsonSerializerOptions();
options.Converters.Add(new DateOnlyModelConverter());
options.Converters.Add(new TimeOnlyModelConverter());

var json = JsonSerializer.Serialize(dateOnlyModel, options);
```

## Error Handling

The library throws meaningful exceptions:

- **`ArgumentNullException`** - When required parameters are null
- **`ArgumentException`** - When parameters are invalid (e.g., empty collections, invalid intervals)
- **`InvalidOperationException`** - When operations cannot be completed (e.g., state not mapped, no scheduled times
  found)
- **`TimeZoneNotFoundException`** - When a timezone is not found on the system

## Examples

### Example 1: Check if Job is Past Due

```csharp
public bool ShouldRunJob(DateTime? lastRunTime)
{
    var scheduleTimes = new List<TimeOnly>
    {
        new TimeOnly(9, 0),   // 9:00 AM
        new TimeOnly(15, 0),   // 3:00 PM
        new TimeOnly(21, 0)   // 9:00 PM
    };

    return DateAndTime.IsPastDue(
        USState.NY,
        scheduleTimes,
        DayFlags.Weekdays,
        lastRunTime);
}
```

### Example 2: Get Next Scheduled Time

```csharp
public DateTime GetNextNotificationTime()
{
    // Send notifications every hour between 9 AM and 5 PM on weekdays
    return DateAndTime.GetNextScheduledDateTime(
        USState.CA,
        new TimeOnly(9, 0),
        new TimeOnly(17, 0),
        60,
        DayFlags.Weekdays);
}
```

### Example 3: Convert Times for Display

```csharp
public string FormatTimeForState(DateTime utcTime, USState state)
{
    var localTime = DateAndTime.ConvertToLocalTime(state, utcTime);
    return localTime?.ToString("g") ?? "N/A";
}
```




<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.DateAndTime.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

*None declared in this project file.*

### Project references

- `Lyo.Common`
- `Lyo.Exceptions`

## Public API (generated)

Top-level `public` types in `*.cs` (*6*). Nested types and file-scoped namespaces may omit some entries.

- `DateAndTime`
- `DateOnlyModel`
- `DateOnlyModelConverter`
- `HolidayDateRule`
- `TimeOnlyModel`
- `TimeOnlyModelConverter`

<!-- LYO_README_SYNC:END -->

## License

Copyright © Lyo

## Repository

[GitHub Repository](https://github.com/mjwherry/Lyo)

