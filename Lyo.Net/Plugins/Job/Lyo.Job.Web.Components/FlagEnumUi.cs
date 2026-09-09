namespace Lyo.Job.Web.Components;

/// <summary>
/// Helpers for editing <c>[Flags]</c> enums in multi-selects. Composite aliases (for example <c>EveryDay</c>) must stay off the selectable list because
/// <see cref="Enum.HasFlag" /> is true for every subset, which would show EveryDay <em>and</em> every individual day.
/// </summary>
internal static class FlagEnumUi
{
    /// <summary>Members with a single bit only (skips <c>None</c> and composite aliases).</summary>
    public static IReadOnlyList<T> AtomicValues<T>()
        where T : struct, Enum
        => [.. Enum.GetValues<T>().Where(IsAtomic)];

    /// <summary>Atomic members currently set on <paramref name="value" />.</summary>
    public static IReadOnlyList<T> SelectedAtomic<T>(T value)
        where T : struct, Enum
        => [.. AtomicValues<T>().Where(flag => value.HasFlag(flag))];

    /// <summary>
    /// True when the OR of <paramref name="selected" /> equals <paramref name="preset" /> exactly. Use this for preset chips so EveryDay does not also light up Weekdays.
    /// </summary>
    public static bool MatchesPreset<T>(IEnumerable<T>? selected, T preset)
        where T : struct, Enum
    {
        long combined = 0;
        if (selected != null) {
            foreach (var flag in selected)
                combined |= Convert.ToInt64(flag);
        }

        return combined == Convert.ToInt64(preset);
    }

    private static bool IsAtomic<T>(T value)
        where T : struct, Enum
    {
        var n = Convert.ToInt64(value);
        return n > 0 && (n & (n - 1)) == 0;
    }
}
