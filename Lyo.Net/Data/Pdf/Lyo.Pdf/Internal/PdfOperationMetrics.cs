using Lyo.Metrics;

namespace Lyo.Pdf.Internal;

internal static class PdfOperationMetrics
{
    public static T Execute<T>(IMetrics metrics, string durationMetric, string successMetric, string failureMetric, Func<T> operation)
    {
        using var timer = metrics.StartTimer(durationMetric);
        try {
            var result = operation();
            metrics.IncrementCounter(successMetric);
            return result;
        }
        catch (Exception ex) {
            metrics.IncrementCounter(failureMetric);
            metrics.RecordError(durationMetric, ex);
            throw;
        }
    }

    public static void Execute(IMetrics metrics, string durationMetric, string successMetric, string failureMetric, Action operation)
    {
        using var timer = metrics.StartTimer(durationMetric);
        try {
            operation();
            metrics.IncrementCounter(successMetric);
        }
        catch (Exception ex) {
            metrics.IncrementCounter(failureMetric);
            metrics.RecordError(durationMetric, ex);
            throw;
        }
    }
}