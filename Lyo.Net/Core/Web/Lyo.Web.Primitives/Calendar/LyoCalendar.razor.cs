using Lyo.DateAndTime;
using Lyo.EntityReference.Models;
using Lyo.Schedule.Models;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

/// <summary>
/// Month, week, and day grid bound to a <see cref="Calendar" />. A month cell groups fires that share a local hour and shows at most three rows. The magnifying glass opens
/// that day's hourly column. Week and day default to the hourly grid, with a list switch. Clicking an entry opens it in a dialog. A muted entry is drawn gray.
/// </summary>
public partial class LyoCalendar
{
    private static readonly string[] WeekdayLabels = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

    private static readonly IReadOnlyList<TimeZoneInfo> Zones = TimeZoneInfo.GetSystemTimeZones().OrderBy(z => z.BaseUtcOffset).ThenBy(z => z.Id, StringComparer.Ordinal).ToArray();
    private bool _agenda;
    private DateOnlyModel? _anchor;
    private DateOnlyModel? _peek;
    private CalendarEntry? _selected;
    private bool _use24Hour = true;

    private CalendarViewMode _view = CalendarViewMode.Month;
    private TimeZoneInfo _zone = TimeZoneInfo.Utc;
    private string? _zoneFromParent;

    /// <summary>Placed range to draw. Null shows an empty state.</summary>
    [Parameter]
    public Calendar? Calendar { get; set; }

    /// <summary>Zone used to place instants and to label the toolbar. Null uses <see cref="Calendar" />'s zone, then UTC.</summary>
    [Parameter]
    public TimeZoneInfo? TimeZone { get; set; }

    /// <summary>Raised when the user picks a zone. The grid rebuckets immediately via <see cref="Lyo.Schedule.Models.Calendar.WithTimeZone" />.</summary>
    [Parameter]
    public EventCallback<TimeZoneInfo> TimeZoneChanged { get; set; }

    /// <summary>True for a 24-hour clock. False for 12-hour labels. Defaults to true.</summary>
    [Parameter]
    public bool Use24Hour { get; set; } = true;

    /// <summary>Raised when the user switches the hour cycle. The switch changes labels only.</summary>
    [Parameter]
    public EventCallback<bool> Use24HourChanged { get; set; }

    /// <summary>Raised when the user moves to a month the current <see cref="Calendar" /> does not cover. The value is the first day of that month.</summary>
    [Parameter]
    public EventCallback<DateOnlyModel> VisibleMonthChanged { get; set; }

    /// <summary>Optional hue degrees by <see cref="CalendarEntry.Category" /> used when an entry has no <see cref="CalendarEntry.Hue" />.</summary>
    [Parameter]
    public IReadOnlyDictionary<string, int>? CategoryHues { get; set; }

    /// <summary>Replaces the default entry body. The host can show its own title, description, and refs.</summary>
    [Parameter]
    public RenderFragment<CalendarEntry>? EntryContent { get; set; }

    /// <summary>Body of the dialog opened by clicking an entry. The host uses this for a job overview. Null shows the title, clock, and description.</summary>
    [Parameter]
    public RenderFragment<CalendarEntry>? DetailContent { get; set; }

    /// <summary>Replaces the default entity-ref chip. A people screen uses this to show a person name.</summary>
    [Parameter]
    public RenderFragment<EntityRef>? RefContent { get; set; }

    private Calendar? Shown => Calendar is null ? null : Calendar.TimeZone.Id == _zone.Id ? Calendar : Calendar.WithTimeZone(_zone);

    private string ZoneLabel => $"{_zone.Id} ({CalendarClock.FormatOffset(_zone, DateTime.UtcNow)})";

    protected override void OnParametersSet()
    {
        var incoming = TimeZone?.Id ?? Calendar?.TimeZone.Id;
        if (incoming is not null && incoming != _zoneFromParent) {
            _zone = TimeZone ?? Calendar?.TimeZone ?? TimeZoneInfo.Utc;
            _zoneFromParent = incoming;
        }

        _use24Hour = Use24Hour;
        if (_anchor is null && Shown is { Days.Count: > 0 } shown)
            _anchor = shown.Days[shown.Days.Count / 2].Date;
    }

    private IReadOnlyList<CalendarDay> VisibleDays()
    {
        var days = Shown?.Days ?? [];
        if (days.Count == 0 || _view == CalendarViewMode.Month)
            return days;

        var anchor = _anchor ?? days[days.Count / 2].Date;
        if (_view == CalendarViewMode.Day)
            return days.Where(day => day.Date == anchor).ToArray();

        var index = -1;
        for (var i = 0; i < days.Count; i++) {
            if (days[i].Date == anchor) {
                index = i;
                break;
            }
        }

        if (index < 0)
            index = 0;

        var start = index - index % 7;
        return days.Skip(start).Take(7).ToArray();
    }

    private IReadOnlyList<string> Headers(IReadOnlyList<CalendarDay> days)
    {
        if (days.Count == 0)
            return WeekdayLabels;

        if (_view == CalendarViewMode.Day)
            return [WeekdayLabels[(int)days[0].Date.ToDateTime().DayOfWeek]];

        var start = (int)days[0].Date.ToDateTime().DayOfWeek;
        return Enumerable.Range(0, 7).Select(i => WeekdayLabels[(start + i) % 7]).ToArray();
    }

    private string MonthTitle()
    {
        var days = Shown?.Days;
        if (days is not { Count: > 0 })
            return "";

        var focus = days[days.Count / 2].Date;
        return focus.ToDateTime().ToString("MMMM yyyy");
    }

    private Task<IEnumerable<string>> SearchZones(string? value, CancellationToken _)
    {
        IEnumerable<TimeZoneInfo> query = Zones;
        if (!string.IsNullOrWhiteSpace(value)) {
            query = query.Where(zone => zone.Id.Contains(value, StringComparison.OrdinalIgnoreCase) || zone.DisplayName.Contains(value, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(query.Take(40).Select(zone => zone.Id));
    }

    private async Task OnZoneChanged(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        var zone = TimeZoneInfo.FindSystemTimeZoneById(id);
        _zone = zone;
        _zoneFromParent = zone.Id;
        await TimeZoneChanged.InvokeAsync(zone);
    }

    private async Task OnHourCycleChanged(bool use24Hour)
    {
        _use24Hour = use24Hour;
        await Use24HourChanged.InvokeAsync(use24Hour);
    }

    private async Task Shift(int direction)
    {
        var shown = Shown;
        if (shown is null || shown.Days.Count == 0)
            return;

        if (_view == CalendarViewMode.Month) {
            var focus = shown.Days[shown.Days.Count / 2].Date;
            await VisibleMonthChanged.InvokeAsync(new DateOnlyModel(focus.Year, focus.Month, 1).AddMonths(direction));
            return;
        }

        var anchor = _anchor ?? shown.Days[shown.Days.Count / 2].Date;
        var next = anchor.AddDays(_view == CalendarViewMode.Week ? 7 * direction : direction);
        _anchor = next;
        if (next < shown.Start || next > shown.End)
            await VisibleMonthChanged.InvokeAsync(new(next.Year, next.Month, 1));
    }

    private void OpenDay(DateOnlyModel date)
    {
        _anchor = date;
        _view = CalendarViewMode.Day;
    }

    private void OpenEntry(CalendarEntry entry) => _selected = entry;

    private void CloseEntry() => _selected = null;

    private void TogglePeek(DateOnlyModel date) => _peek = _peek is not null && _peek.Equals(date) ? null : date;

    private bool Peeking(DateOnlyModel date) => _peek is not null && _peek.Equals(date);

    private bool IsSelected(CalendarEntry entry) => ReferenceEquals(_selected, entry);

    private bool IsOutside(DateOnlyModel date)
    {
        var days = Shown?.Days;
        if (days is not { Count: > 0 } || _view != CalendarViewMode.Month)
            return false;

        var focus = days[days.Count / 2].Date;
        return date.Year != focus.Year || date.Month != focus.Month;
    }

    private bool IsToday(DateOnlyModel date)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zone);
        return date.Year == local.Year && date.Month == local.Month && date.Day == local.Day;
    }

    private string DayClass(CalendarDay day)
    {
        var css = "lyo-cal-day";
        if (IsOutside(day.Date))
            css += " lyo-cal-day-outside";

        if (IsToday(day.Date))
            css += " lyo-cal-day-today";

        return css;
    }

    private CalendarMonthSummary.Result MonthRows(CalendarDay day) => CalendarMonthSummary.Build(day.Entries.Select(entry => (entry.Start, entry.End)).ToArray(), day.Date, _zone);

    private string GroupLabel(CalendarDay day, CalendarMonthSummary.Row row)
    {
        if (row.AllDay)
            return row.Indexes.Count == 1 ? day.Entries[row.Indexes[0]].Title : $"All day · {row.Indexes.Count}";

        return $"{HourLabel(row.Hour)} · {row.Indexes.Count}";
    }

    private string GroupTitle(CalendarDay day, CalendarMonthSummary.Row row) => string.Join("\n", row.Indexes.Select(index => EventTooltip(day.Entries[index])));

    private IReadOnlyList<CalendarEventLayout.Block> BlocksFor(CalendarDay day)
        => CalendarEventLayout.Place(day.Entries.Select(entry => (entry.Start, entry.End)).ToArray(), day.Date, _zone);

    private bool HasAllDay(IReadOnlyList<CalendarDay> days) => days.Any(day => BlocksFor(day).Any(block => block.AllDay));

    private int HueDegrees(CalendarEntry entry)
    {
        var degrees = entry.Hue;
        if (degrees is null && entry.Category is not null && CategoryHues is not null && CategoryHues.TryGetValue(entry.Category, out var mapped))
            degrees = mapped;

        if (degrees is not int hue)
            return 207;

        hue %= LyoChipHue.CircleDegrees;
        return hue < 0 ? hue + LyoChipHue.CircleDegrees : hue;
    }

    private string EventStyle(CalendarEntry entry) => FormattableString.Invariant($"--lyo-cal-hue:{HueDegrees(entry)}");

    private string GroupClass(CalendarDay day, CalendarMonthSummary.Row row) => RowMuted(day, row) ? "lyo-cal-event lyo-cal-muted" : "lyo-cal-event";

    private bool RowMuted(CalendarDay day, CalendarMonthSummary.Row row)
    {
        foreach (var index in row.Indexes) {
            if (!day.Entries[index].Muted)
                return false;
        }

        return row.Indexes.Count > 0;
    }

    private string EventClass(CalendarEntry entry) => WithMuted(IsSelected(entry) ? "lyo-cal-event lyo-cal-event-selected" : "lyo-cal-event", entry.Muted);

    private string BlockClass(CalendarEntry entry) => WithMuted(IsSelected(entry) ? "lyo-cal-block lyo-cal-event-selected" : "lyo-cal-block", entry.Muted);

    private static string WithMuted(string css, bool muted) => muted ? css + " lyo-cal-muted" : css;

    private string BlockStyle(CalendarEventLayout.Block block, CalendarEntry entry)
    {
        var width = 100d / block.LaneCount;
        var left = block.Lane * width;
        return FormattableString.Invariant(
            $"--lyo-cal-hue:{HueDegrees(entry)};top:{block.TopPercent:0.###}%;height:{block.HeightPercent:0.###}%;left:calc({left:0.###}% + 1px);width:calc({width:0.###}% - 2px)");
    }

    private string HourLabel(int hour)
    {
        if (_use24Hour)
            return FormattableString.Invariant($"{hour:00}:00");

        var suffix = hour < 12 ? "AM" : "PM";
        var clock = hour % 12;
        if (clock == 0)
            clock = 12;

        return FormattableString.Invariant($"{clock} {suffix}");
    }

    private static string WeekdayLabel(DateOnlyModel date) => WeekdayLabels[(int)date.ToDateTime().DayOfWeek];

    private static string DayTitle(CalendarDay day) => day.Entries.Count == 1 ? "1 event" : $"{day.Entries.Count} events";

    private static string EventTooltip(CalendarEntry entry) => string.IsNullOrWhiteSpace(entry.Description) ? entry.Title : entry.Title + " — " + entry.Description;

    private string EventLabel(CalendarEntry entry) => Clock(entry) + " " + entry.Title;

    private LyoChipHue? HueOf(CalendarEntry entry)
    {
        var degrees = entry.Hue;
        if (degrees is null && entry.Category is not null && CategoryHues is not null && CategoryHues.TryGetValue(entry.Category, out var mapped))
            degrees = mapped;

        return degrees is int value ? new LyoChipHue(value) : null;
    }

    private string Clock(CalendarEntry entry)
    {
        var start = CalendarClock.Format(entry.Start, _zone, _use24Hour);
        if (entry.IsPoint)
            return start;

        return $"{start}–{CalendarClock.Format(entry.End, _zone, _use24Hour)}";
    }

    private static string RefLabel(EntityRef entity)
    {
        var id = entity.EntityId.Length <= 8 ? entity.EntityId : entity.EntityId[..8];
        return $"{entity.EntityType} · {id}";
    }

    private enum CalendarViewMode
    {
        Month,
        Week,
        Day
    }
}