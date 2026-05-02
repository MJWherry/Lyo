using Lyo.Metrics;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Internal;

namespace Lyo.Privacy.Metrics;

internal static class PrivacyMetricsRecorder
{
    private static readonly (string, string)[] NoTags = [];

    public static void RecordTextRedactionResult(IMetrics metrics, RedactionResult result, string? policyName)
    {
        if (result.TotalRuns > 0)
            metrics.IncrementCounter(PrivacyMetricNames.TextRedactionRuns, result.TotalRuns, PrivacyObservation.TagsForPolicy(policyName));

        foreach (var kv in result.CountsByKind) {
            if (kv.Value <= 0)
                continue;

            metrics.IncrementCounter(PrivacyMetricNames.TextRedactionsByKind, kv.Value, PrivacyObservation.TagsForKind(kv.Key, policyName));
        }
    }

    public static void RecordJsonRedactionResult(IMetrics metrics, RedactionResult result, string? policyName)
    {
        if (result.CountsByKind.TryGetValue(RedactionKind.JsonKey, out var jsonKeys) && jsonKeys > 0)
            metrics.IncrementCounter(PrivacyMetricNames.JsonKeyRedactions, jsonKeys, PrivacyObservation.TagsForPolicy(policyName));

        foreach (var kv in result.CountsByKind) {
            if (kv.Value <= 0)
                continue;

            metrics.IncrementCounter(PrivacyMetricNames.JsonRedactionsByKind, kv.Value, PrivacyObservation.TagsForKind(kv.Key, policyName));
        }
    }
}