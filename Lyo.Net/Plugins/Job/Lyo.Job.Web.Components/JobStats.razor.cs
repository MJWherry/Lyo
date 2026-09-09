using Microsoft.Extensions.Logging;

namespace Lyo.Job.Web.Components;

public partial class JobStats
{
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public string? StatisticsRoute { get; set; }

    private bool _loading;
    private string? _error;
    private IReadOnlyList<SpJobStatistic> _stats = [];

    private IEnumerable<SpJobStatistic> _filtered => _stats;

    private static readonly string[] ResultLabels = ["Success", "Failure", "Warn", "Partial", "Cancelled", "Skipped", "Timeout", "Unknown"];

    private readonly ChartOptions _pieOptions = new() { ChartPalette = ["#4CAF50", "#F44336", "#FF9800", "#2196F3", "#9C27B0", "#607D8B", "#795548", "#FF5722"] };
    private readonly ChartOptions _barOptions = new() { ChartPalette = ["#2196F3", "#4CAF50", "#FF9800"] };

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        if (string.IsNullOrEmpty(StatisticsRoute))
            return;

        _loading = true;
        _error = null;
        try {
            _stats = await ApiClient.GetAsAsync<IReadOnlyList<SpJobStatistic>>(StatisticsRoute) ?? [];
        }
        catch (Exception ex) {
            // Zeros are indistinguishable from "no jobs have run", so a failed load has to say so rather than drawing an empty dashboard.
            _error = ex.Message;
            _stats = [];
            Logger.LogError(ex, "Failed to load job statistics");
        }
        finally {
            _loading = false;
        }
    }

    private double[] ResultData()
    {
        var s = _filtered.ToList();
        return [s.Sum(j => j.SuccessCount), s.Sum(j => j.FailureCount), s.Sum(j => j.SuccessWithWarningsCount), s.Sum(j => j.PartialSuccessCount), s.Sum(j => j.CancelledCount), s.Sum(j => j.SkippedCount), s.Sum(j => j.TimeoutCount), s.Sum(j => j.UnknownCount)];
    }

    private string[] JobLabels() => _filtered.Select(j => j.JobName.Length > 15 ? j.JobName[..12] + "..." : j.JobName).ToArray();

    private List<ChartSeries<double>> RuntimeSeries() => [new() { Name = "Average", Data = _filtered.Select(j => (j.AvgRunTimeMs ?? 0) / 1000).ToArray() }, new() { Name = "Min", Data = _filtered.Select(j => (j.MinRunTimeMs ?? 0) / 1000).ToArray() }, new() { Name = "Max", Data = _filtered.Select(j => (j.MaxRunTimeMs ?? 0) / 1000).ToArray() }];

    private double OverallSuccessRate()
    {
        var total = _filtered.Sum(j => j.TotalRuns);
        return total > 0 ? (double)_filtered.Sum(j => j.SuccessCount) / total * 100 : 0;
    }

    private string AverageRuntime()
    {
        var withData = _filtered.Where(j => j.AvgRunTimeMs.HasValue).ToList();
        return withData.Count == 0 ? "-" : LyoDurationDisplay.FormatClock(withData.Average(j => j.AvgRunTimeMs!.Value));
    }
}
