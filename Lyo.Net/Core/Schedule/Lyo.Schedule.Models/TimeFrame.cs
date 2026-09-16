using Lyo.EntityReference.Models;
using Lyo.Exceptions;

namespace Lyo.Schedule.Models;

/// <summary>One concrete span of time that can be placed on a <see cref="Calendar" />.</summary>
/// <param name="Start">Inclusive UTC start. Unspecified values are treated as UTC.</param>
/// <param name="End">Inclusive UTC end. Equal to <paramref name="Start" /> for a point. Unspecified values are treated as UTC.</param>
/// <param name="Key">Stable id for this span. Callers use it to tell one frame from another.</param>
/// <param name="Title">Short label shown on the calendar.</param>
/// <param name="Description">Longer text the view can show with the title.</param>
/// <param name="Hue">Optional color wheel degree, 0–359, matching the chip hue wheel.</param>
/// <param name="Category">Optional grouping key a host can map to a hue when <paramref name="Hue" /> is unset.</param>
/// <param name="Refs">Entity references attached to the span, such as the people in a meeting. The calendar stores them and does not load the entities.</param>
/// <param name="Muted">True when the span should be drawn gray, such as a fire a blackout would skip.</param>
public sealed record TimeFrame(
    DateTime Start,
    DateTime End,
    string Key,
    string Title,
    string? Description = null,
    int? Hue = null,
    string? Category = null,
    IReadOnlyList<EntityRef>? Refs = null,
    bool Muted = false)
{
    /// <summary>Entity references attached to the span. Empty when the caller passed none.</summary>
    public IReadOnlyList<EntityRef> Refs { get; init; } = CalendarEntry.CopyRefs(Refs);

    /// <summary>Checks the span. Throws when the end is before the start, the key or title is blank, or the hue is outside 0–359.</summary>
    public void Validate() => CalendarEntry.Validate(Start, End, Key, Title, Hue);

    /// <summary>Copies this span onto a calendar entry. Instants stay UTC.</summary>
    public CalendarEntry ToEntry()
    {
        Validate();
        return new(Start, End, Key, Title, Description, Hue, Category, Refs, Muted);
    }
}
