namespace Lyo.Job.Web.Components;

/// <summary>
/// Helpers for editing <c>[Flags]</c> enums in multi-selects. Composite aliases (e.g. <c>EveryDay</c>) must not appear as selectable values because <see cref="Enum.HasFlag" />
/// is true for every subset, which would show EveryDay <em>and</em> every individual day.
/// </summary>
internal static class FlagEnumUi
{
    /// <summary>Single-bit members only (excludes <c>None</c> and composite aliases).</summary>
    public static IReadOnlyList<T> AtomicValues<T>()
        where T : struct, Enum
        => [.. Enum.GetValues<T>().Where(IsAtomic)];

    /// <summary>Atomic members that are set on <paramref name="value" />.</summary>
    public static IReadOnlyList<T> SelectedAtomic<T>(T value)
        where T : struct, Enum
        => [.. AtomicValues<T>().Where(flag => value.HasFlag(flag))];

    private static bool IsAtomic<T>(T value)
        where T : struct, Enum
    {
        var n = Convert.ToInt64(value);
        return n > 0 && (n & (n - 1)) == 0;
    }
}
