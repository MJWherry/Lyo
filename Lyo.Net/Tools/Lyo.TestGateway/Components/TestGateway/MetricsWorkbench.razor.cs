using System.Globalization;
using System.Net.Http.Json;
using Lyo.Api.Client;
using Lyo.Images;
using Lyo.Metrics;
using Lyo.Metrics.Models;
using Lyo.TestGateway.Components;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using MudBlazor;
using static Microsoft.AspNetCore.Components.Web.RenderMode;

namespace Lyo.TestGateway.Components.TestGateway;

public partial class MetricsWorkbench
{
    private bool _busy;
    private string _filterText = string.Empty;
    private DateTime _lastRefresh;
    private MetricsSnapshot? _snapshot;
    private List<MetricData> _events = [];

    private MetricsService? MetricsStore => Metrics as MetricsService;

    private List<KeyValuePair<string, CounterData>> FilteredCounters => (_snapshot?.Counters ?? []).Where(MatchesFilter).OrderBy(i => i.Value.Name).ToList();

    private List<KeyValuePair<string, GaugeData>> FilteredGauges => (_snapshot?.Gauges ?? []).Where(MatchesFilter).OrderBy(i => i.Value.Name).ToList();

    private List<KeyValuePair<string, HistogramData>> FilteredHistograms => (_snapshot?.Histograms ?? []).Where(MatchesFilter).OrderBy(i => i.Value.Name).ToList();

    private List<MetricData> FilteredEvents => _events.Where(MatchesFilter).OrderByDescending(i => i.Timestamp).ToList();

    protected override Task OnInitializedAsync() => RefreshAsync();

    private Task RefreshAsync()
    {
        if (MetricsStore == null) {
            SetStatus("Metrics viewer requires the in-memory MetricsService implementation.", Severity.Warning);
            _snapshot = null;
            _events = [];
            return Task.CompletedTask;
        }

        _busy = true;
        try {
            _snapshot = MetricsStore.Export();
            _events = MetricsStore.GetEvents(250).OrderByDescending(i => i.Timestamp).ToList();
            _lastRefresh = DateTime.UtcNow;
            SetStatus("Metrics refreshed.", Severity.Success);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }

        return Task.CompletedTask;
    }

    private Task ClearAsync()
    {
        if (MetricsStore == null) {
            SetStatus("Metrics viewer requires the in-memory MetricsService implementation.", Severity.Warning);
            return Task.CompletedTask;
        }

        MetricsStore.Clear();
        _snapshot = MetricsStore.Export();
        _events = [];
        _lastRefresh = DateTime.UtcNow;
        SetStatus("Metrics cleared.", Severity.Success);
        return Task.CompletedTask;
    }

    private bool MatchesFilter(KeyValuePair<string, CounterData> entry) => MatchesFilter(entry.Value.Name, entry.Value.Tags);

    private bool MatchesFilter(KeyValuePair<string, GaugeData> entry) => MatchesFilter(entry.Value.Name, entry.Value.Tags);

    private bool MatchesFilter(KeyValuePair<string, HistogramData> entry) => MatchesFilter(entry.Value.Name, entry.Value.Tags);

    private bool MatchesFilter(MetricData entry) => MatchesFilter(entry.Name, entry.Tags);

    private bool MatchesFilter(string name, Dictionary<string, string>? tags)
    {
        if (string.IsNullOrWhiteSpace(_filterText))
            return true;

        var filter = _filterText.Trim();
        if (name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            return true;

        return tags?.Any(i => i.Key.Contains(filter, StringComparison.OrdinalIgnoreCase) || i.Value.Contains(filter, StringComparison.OrdinalIgnoreCase)) == true;
    }

    private static string FormatTags(Dictionary<string, string>? tags) => tags == null || tags.Count == 0 ? "(none)" : string.Join(", ", tags.OrderBy(i => i.Key).Select(i => $"{i.Key}={i.Value}"));

    private static string FormatEventTags(MetricData metric)
    {
        var tags = metric.Tags == null || metric.Tags.Count == 0 ? "(none)" : string.Join(", ", metric.Tags.OrderBy(i => i.Key).Select(i => $"{i.Key}={i.Value}"));
        return metric.Exception == null ? tags : $"{tags} | exception={metric.Exception.Message}";
    }
}
