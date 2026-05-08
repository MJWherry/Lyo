# Lyo.DateAndTime

Date, time, US timezone conversion, day-of-week scheduling, and US holiday metadata for .NET. The API is **static** and **thread-safe** (no mutable shared state).

**Target frameworks:** `netstandard2.0`, `net10.0`

## Dependencies

- [`Lyo.Common`](../../Common/Lyo.Common/README.md) — `USState`, `DayFlags`, `GeographicInfo` (IANA timezone IDs per state)
- [`Lyo.Exceptions`](../../Lyo.Exceptions/README.md)

---

## Time zones and `DateAndTime`

`DateAndTime` resolves `TimeZoneInfo` via `GeographicInfo.FromState(usState)` in `Lyo.Common`. Each US state maps to an **IANA** identifier (for example `America/New_York`, `America/Chicago`, `America/Denver`, `America/Los_Angeles`, `America/Phoenix`, `America/Anchorage`, `Pacific/Honolulu`). If the OS does not recognize the ID, `GetTimeZoneByState` returns `null` and conversion helpers return `null` instead of throwing.

### `DateAndTime` — timezone

| Member | Description |
|--------|-------------|
| `GetTimeZoneByState(USState)` | `TimeZoneInfo?` for the state’s primary mapping, or `null`. |
| `GetLocalDateTime(USState, DateTime? utc = null)` | Local `DateTime` in that zone; `utc` defaults to `DateTime.UtcNow`. |
| `GetCurrentLocalDateTime(USState)` | Same as `GetLocalDateTime(state, DateTime.UtcNow)`. |
| `ConvertToLocalTime(USState, DateTime utcDateTime)` | Alias of `GetLocalDateTime` with an explicit UTC instant. |
| `ConvertToUtc(USState, DateTime localDateTime)` | Interprets `localDateTime` in the state’s zone and returns UTC, or `null` if unmapped. |

### `DateAndTime` — scheduling

Scheduling uses **local** `DateTime` in the given state (`GetCurrentLocalDateTime` / conversions). `DayFlags` is a `[Flags]` enum from `Lyo.Common` (weekday bits). Internally the day flag is `1 << (int)date.DayOfWeek`.

**Constants (not configurable):** next-slot search looks up to **7 days** ahead; discrete-time past-due scans up to **7 days** of history when `lastRunDateTime` is null (default anchor is UTC “now minus 7 days”).

| Member | Description |
|--------|-------------|
| `GetNextScheduledDateTime(USState, IEnumerable<TimeOnly> times, DayFlags)` | Next local instant strictly after “now” on an allowed day, for one of the listed clock times. |
| `GetNextScheduledDateTime(USState, TimeOnly start, TimeOnly end, int intervalMinutes, DayFlags)` | Next instant after “now” on an allowed day, stepping by `intervalMinutes` within `[start, end]` inclusive. |
| `IsPastDue(USState, IEnumerable<TimeOnly> times, DayFlags, DateTime? lastRunUtc)` | `true` if any scheduled slot **after** the localized last run and **on or before** localized “now” falls on an allowed day within the scan window. |
| `IsPastDue(USState, TimeOnly start, TimeOnly end, int minuteInterval, DayFlags, DateTime? lastRunUtc)` | **Window mode:** only considers **today** (local calendar). Returns `true` only if today is scheduled, “now” is inside `[start, end]`, and the latest tick **at or before** “now” is after the localized last run. |
| `GetScheduledTimesForDay(DateTime date, IEnumerable<TimeOnly> times, DayFlags)` | Yields local `DateTime` values on `date.Date` for each time if `date` matches `DayFlags`. **Does not** take `USState` (caller supplies the calendar date). |
| `GetScheduledTimesForDay(DateTime date, TimeOnly start, TimeOnly end, int intervalMinutes, DayFlags)` | Same, for interval ticks in the window on that calendar date. |
| `IsScheduledDay(DateTime dateTime, DayFlags)` | Whether `dateTime`’s day-of-week is included in `scheduleFlags`. |

**`TimeOnly` vs `TimeOnlyModel`:** On `net6.0` or greater, scheduling APIs use `System.TimeOnly`. On `netstandard2.0`, use `Lyo.DateAndTime.TimeOnlyModel`, which is source-compatible for typical call sites via conditional compilation in this library (`TimeOnly` type alias in `DateAndTime.cs`).

---

## `DateOnlyModel` and `TimeOnlyModel` (.NET Standard 2.0)

`System.DateOnly` and `System.TimeOnly` are not in .NET Standard 2.0. This library provides:

- **`DateOnlyModel`** — day number from 0001-01-01; `Year`/`Month`/`Day`, `Today`, `AddDays`/`AddMonths`/`AddYears`, `FromDateTime`/`Parse`/`TryParse`/`ParseExact`/`TryParseExact`, comparison operators, `ToDateTime()`, `ToDateTime(TimeOnlyModel)`.
- **`TimeOnlyModel`** — ticks within one day; constructors from hour/minute/(second)/(ms), `Ticks`, `FromTimeSpan`/`FromDateTime`, parse APIs, `ToTimeSpan()`, `ToDateTime(DateOnlyModel)`, `Add`/`AddHours`/`AddMinutes`/`AddSeconds`/`AddMilliseconds` (wraps within a day).

---

## US holidays — `HolidayInfo` and `HolidayDateRule`

**`HolidayDateRule`** enum: `Unknown`, `FixedDate`, `NthWeekdayOfMonth`, `LastWeekdayOfMonth`.

**`HolidayInfo`** is an immutable `record` with: `Name`, `Slug`, `Description`, `IsFederal`, `IsObservedWhenWeekend`, `DateRule`, `Month`, `DayOfMonth`, `DayOfWeek`, `Occurrence`, `Aliases`, plus `CanonicalName` (`Slug`).

Static instances include (among others): `NewYearsDay`, `MartinLutherKingJrDay`, `ValentinesDay`, `PresidentsDay`, `StPatricksDay`, `EasterSunday`, `MothersDay`, `MemorialDay`, `Juneteenth`, `FathersDay`, `IndependenceDay`, `LaborDay`, `ColumbusDay`, `Halloween`, `VeteransDay`, `ThanksgivingDay`, `ChristmasDay`, `NewYearsEve`, and `Unknown`.

| API | Description |
|-----|-------------|
| `HolidayInfo.All` | All registered holidays (excluding duplicates from reflection order). |
| `HolidayInfo.FederalHolidays` | Subset with `IsFederal`. |
| `HolidayInfo.FromName` / `FromSlug` / `FromAlias` | Case-insensitive lookup; blank → `Unknown`. |
| `GetDate(int year)` | Calendar date; Easter uses Meeus/Jones/Butcher Gregorian algorithm. |
| `GetObservedDate(int year)` | If `IsObservedWhenWeekend`, weekend dates shift to adjacent weekday (Sat → Fri, Sun → Mon). |
| `OccursOn(DateTime date, bool includeObservedDate = true)` | True if `date` is the actual or observed holiday date for `date.Year`. |
| `HolidayInfo.FromDate(DateTime date, bool includeObservedDate = true)` | First non-`Unknown` holiday matching that calendar date, or `null`. |

---

## JSON — `Lyo.DateAndTime.Json`

| Type | Behavior |
|------|----------|
| `DateOnlyModelConverter` | `JsonConverter<DateOnlyModel?>` — ISO `yyyy-MM-dd` strings; null/whitespace → null. |
| `TimeOnlyModelConverter` | `JsonConverter<TimeOnlyModel?>` — `HH:mm:ss.fffffff`; null/whitespace → null. |
| `LyoJsonSerializerOptionsExtensions.AddLyoDateOnlyModelConverters(JsonSerializerOptions)` | Registers both converters and returns `options`. |

---

## Exceptions

| Type | Typical cause |
|------|----------------|
| `ArgumentNullException` | Null collections or required arguments. |
| `ArgumentException` | Empty schedule lists, invalid intervals (`<= 0`), `startTime > endTime`. |
| `InvalidOperationException` | Unmapped state, or no matching next schedule within the look-ahead window. |
| `FormatException` | `Parse` / `ParseExact` failures on models. |
| `TimeZoneNotFoundException` | Rare; thrown by BCL if a zone id is invalid in edge environments (library usually returns `null` from `GeographicInfo.TimeZone`). |

---

## Repository

[GitHub Repository](https://github.com/mjwherry/Lyo)
