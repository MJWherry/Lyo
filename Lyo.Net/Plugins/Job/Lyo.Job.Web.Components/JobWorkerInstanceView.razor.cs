using System.Globalization;

namespace Lyo.Job.Web.Components;

public partial class JobWorkerInstanceView
{
    [Parameter]
    [EditorRequired]
    public JobWorkerInstanceRes Instance { get; set; } = null!;

    private List<KeyValuePair<string, string?>> _systemPairs = [];
    private List<KeyValuePair<string, string?>> _workerPairs = [];

    private string CpuSummary => Combine(Meta(Constants.WorkerMetadata.ProcessorCount) is { } count ? $"{count} logical" : null, Meta(Constants.WorkerMetadata.CpuModel));

    private string MemorySummary => FormatBytes(Meta(Constants.WorkerMetadata.TotalPhysicalMemoryBytes));

    private string WorkingSetSummary {
        get {
            var workingSet = FormatBytes(Meta(Constants.WorkerMetadata.WorkingSetBytes));
            var gcHeap = FormatBytes(Meta(Constants.WorkerMetadata.GcHeapBytes));
            if (workingSet == "—" && gcHeap == "—")
                return "—";
            return gcHeap == "—" ? workingSet : $"{workingSet} working / {gcHeap} GC";
        }
    }

    private string SubscriptionsSummary => Meta(Constants.WorkerMetadata.Subscriptions) ?? Meta(Constants.WorkerMetadata.Queue) ?? "—";

    protected override void OnParametersSet()
    {
        var pairs = Instance.Metadata?.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase).ToList() ?? [];
        _systemPairs = pairs.Where(p => Constants.WorkerMetadata.IsSystemKey(p.Key)).ToList();
        _workerPairs = pairs.Where(p => !Constants.WorkerMetadata.IsSystemKey(p.Key)).ToList();
    }

    private string? Meta(string key)
    {
        if (Instance.Metadata is null)
            return null;

        return Instance.Metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
    }

    private static string Combine(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left))
            return string.IsNullOrWhiteSpace(right) ? "—" : right!;
        return string.IsNullOrWhiteSpace(right) ? left : $"{left} · {right}";
    }

    private static string FormatMetadataValue(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "—";
        if (key.EndsWith("Bytes", StringComparison.OrdinalIgnoreCase))
            return FormatBytes(value);
        return value;
    }

    private static string FormatBytes(string? raw)
        => long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var bytes) && bytes >= 0 ? FileSizeUnitInfo.FormatBestFit(bytes) : "—";
}
