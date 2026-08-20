using System.Globalization;

namespace Lyo.Web.Components.DataGrid;

/// <summary>Builds Dev-menu JSON dialog chips from the last grid query.</summary>
internal static class DataGridDevChips
{
    /// <summary>Elapsed time, HTTP status, page size / total, query score, and has-more.</summary>
    public static IReadOnlyList<JsonViewChip> ForResponse(long? elapsedMs, int? statusCode, int? itemCount, int? total, int? queryScore, bool? hasMore)
    {
        var chips = new List<JsonViewChip>();
        if (elapsedMs is { } ms)
            chips.Add(new(LyoDurationDisplay.Format(ms), LyoDurationDisplay.ForDurationMs(ms), Icons.Material.Filled.Timer));

        if (statusCode is { } code)
            chips.Add(new(code.ToString(CultureInfo.InvariantCulture), ForStatus(code)));

        if (itemCount is not null || total is not null)
            chips.Add(new($"{FormatCount(itemCount)} / {FormatCount(total)}"));

        if (queryScore is { } score)
            chips.Add(new($"Score {score}"));

        if (hasMore == true)
            chips.Add(new("Has more", Color.Info));

        return chips;
    }

    private static string FormatCount(int? value) => value is { } n ? n.ToString("N0", CultureInfo.CurrentCulture) : "—";

    private static Color ForStatus(int code)
        => code switch {
            >= 200 and < 300 => Color.Success,
            >= 400 and < 500 => Color.Warning,
            >= 500 => Color.Error,
            var _ => Color.Default
        };
}
