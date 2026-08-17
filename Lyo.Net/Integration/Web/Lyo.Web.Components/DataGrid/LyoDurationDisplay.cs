using MudBlazor;

namespace Lyo.Web.Components.DataGrid;

/// <summary>Shared duration text and MudBlazor chip colors for report generations and job runs.</summary>
public static class LyoDurationDisplay
{
    /// <summary>Human-readable duration from milliseconds.</summary>
    public static string Format(double? ms)
    {
        if (ms is null)
            return "—";

        if (ms < 1_000)
            return $"{ms:F0} ms";

        if (ms < 60_000)
            return $"{ms / 1_000:F1} s";

        return $"{ms / 60_000:F1} min";
    }

    /// <summary>Elapsed time in milliseconds, or null when the operation has not started.</summary>
    public static double? GetDurationMs(DateTime? started, DateTime? finished)
    {
        if (started is null)
            return null;

        return ((finished ?? DateTime.UtcNow) - started.Value).TotalMilliseconds;
    }

    /// <summary>Elapsed time. In-flight rows (started, not finished) show time-so-far with a running suffix.</summary>
    public static string FormatFromDates(DateTime? started, DateTime? finished)
    {
        var ms = GetDurationMs(started, finished);
        if (ms is null)
            return "—";

        var text = Format(ms);
        return finished is null ? $"{text} (running)" : text;
    }

    /// <summary>Chip color for duration. Running stays info; completed uses speed buckets via <see cref="ForDurationMs" />.</summary>
    public static Color ForDuration(DateTime? started, DateTime? finished)
    {
        if (started is null)
            return Color.Default;

        if (finished is null)
            return Color.Info;

        return ForDurationMs(GetDurationMs(started, finished)!.Value);
    }

    /// <summary>Chip color for a completed duration: fast success, then info / warning / error.</summary>
    public static Color ForDurationMs(double ms)
        => ms switch {
            < 2_000 => Color.Success,
            < 10_000 => Color.Info,
            < 30_000 => Color.Warning,
            var _ => Color.Error
        };
}
