using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Web.Primitives;

/// <summary>
/// Virtualized log pane with a minimum-level filter, text search, follow-tail, copy, and download. Prefer it for job runs, diagnostics, seed, automation, and queue
/// traces instead of a growing <c>&lt;pre&gt;</c> that locks the UI once a few thousand lines arrive.
/// </summary>
/// <remarks>
/// Pass the current snapshot as <see cref="Entries" />. The viewer does not subscribe to a stream itself; the parent appends and re-renders, and follow-tail
/// scrolls to the newest row when the count grows.
/// </remarks>
public partial class LyoLogViewer
{
    private static readonly LyoLogLevel[] FilterLevels = [LyoLogLevel.Trace, LyoLogLevel.Debug, LyoLogLevel.Information, LyoLogLevel.Warning, LyoLogLevel.Error, LyoLogLevel.Critical];

    private LyoLogLevel _minLevel = LyoLogLevel.Trace;
    private string? _search;
    private bool _followTail = true;
    private int _lastCount;
    private Virtualize<LyoLogEntry>? _virtualize;

    /// <summary>Log rows, oldest first. The viewer does not mutate the list.</summary>
    [Parameter]
    public IReadOnlyList<LyoLogEntry> Entries { get; set; } = [];

    /// <summary>Initial minimum level. The toolbar still lets the user raise it.</summary>
    [Parameter]
    public LyoLogLevel MinLevel { get; set; } = LyoLogLevel.Trace;

    /// <summary>File name used by the download button.</summary>
    [Parameter]
    public string DownloadFileName { get; set; } = "log.txt";

    /// <summary>CSS class on the wrapper.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Inline style on the wrapper.</summary>
    [Parameter]
    public string? Style { get; set; }

    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    private List<LyoLogEntry> Visible { get; set; } = [];

    private bool HasRows => Visible.Count > 0;

    private string EmptyHint => Entries.Count == 0 ? "Lines will appear here as they arrive." : "Nothing matches the current level and search.";

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (_lastCount == 0)
            _minLevel = MinLevel;

        RebuildVisible();
        if (_followTail && Visible.Count > _lastCount)
            _ = RefreshVirtualizeAsync();

        _lastCount = Visible.Count;
    }

    private void SetMinLevel(LyoLogLevel level)
    {
        _minLevel = level;
        RebuildVisible();
    }

    private void OnSearchChanged() => RebuildVisible();

    private void RebuildVisible()
    {
        var search = _search?.Trim();
        Visible = Entries.Where(entry => entry.Level >= _minLevel && Matches(entry, search)).ToList();
    }

    private static bool Matches(LyoLogEntry entry, string? search)
        => string.IsNullOrWhiteSpace(search)
            || entry.Message.Contains(search, StringComparison.OrdinalIgnoreCase)
            || (entry.Source?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false);

    private async Task CopyAsync()
    {
        if (Services.GetService<IJsInterop>() is not { } js)
            return;

        await js.SendToClipboard(FormatVisible());
    }

    private async Task DownloadAsync()
    {
        if (Services.GetService<IJsInterop>() is not { } js)
            return;

        await js.DownloadFile(Encoding.UTF8.GetBytes(FormatVisible()), DownloadFileName, "text/plain");
    }

    private string FormatVisible()
    {
        var builder = new StringBuilder();
        foreach (var entry in Visible)
            builder.Append(entry.Timestamp.ToString("O")).Append(' ').Append(entry.Level).Append(' ').Append(entry.Source).Append(' ').AppendLine(entry.Message);

        return builder.ToString();
    }

    private async Task RefreshVirtualizeAsync()
    {
        if (_virtualize is null)
            return;

        await _virtualize.RefreshDataAsync();
    }

    private static Color ColorFor(LyoLogLevel level)
        => level switch {
            LyoLogLevel.Warning => Color.Warning,
            LyoLogLevel.Error or LyoLogLevel.Critical => Color.Error,
            LyoLogLevel.Information => Color.Info,
            var _ => Color.Default
        };
}
