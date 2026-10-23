using System.Diagnostics;
using Lyo.EntityReference.Models;
using Lyo.Exceptions;

namespace Lyo.Schedule.Models;

/// <summary>One item placed on a <see cref="Calendar" />. A span stays a single entry and is listed on every civil day it intersects.</summary>
/// <param name="Start">Inclusive UTC start. Unspecified values are treated as UTC. Equal to <paramref name="End" /> for a point.</param>
/// <param name="End">Inclusive UTC end. Unspecified values are treated as UTC.</param>
/// <param name="Key">Stable id. Several days may list the same key when a span crosses midnight.</param>
/// <param name="Title">Short label shown on the calendar.</param>
/// <param name="Description">Longer text the view can show with the title.</param>
/// <param name="Hue">Optional color wheel degree, 0–359, matching the chip hue wheel. Null lets the host color by <paramref name="Category" />.</param>
/// <param name="Category">Optional grouping key a host can map to a hue when <paramref name="Hue" /> is unset.</param>
/// <param name="Refs">Entity references attached to the entry. The calendar stores them and does not load the entities.</param>
/// <param name="Muted">True when the entry should be drawn gray, such as a fire a blackout would skip.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record CalendarEntry(
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
    /// <summary>Entity references attached to the entry. Empty when the caller passed none.</summary>
    public IReadOnlyList<EntityRef> Refs { get; init; } = CopyRefs(Refs);

    /// <summary>True when the entry is a single instant.</summary>
    public bool IsPoint => AsUtc(Start) == AsUtc(End);

    internal static void Validate(DateTime start, DateTime end, string key, string title, int? hue)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(title);
        ArgumentHelpers.ThrowIf(AsUtc(end) < AsUtc(start), "End must be at or after Start.", nameof(end));
        if (hue is null)
            return;

        ArgumentHelpers.ThrowIfNegative(hue.Value);
        ArgumentHelpers.ThrowIfGreaterThan(hue.Value, 359);
    }

    internal static IReadOnlyList<EntityRef> CopyRefs(IReadOnlyList<EntityRef>? refs) => refs is { Count: > 0 } ? refs.ToArray() : [];

    internal static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}