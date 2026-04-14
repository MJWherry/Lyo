using Lyo.Mathematics.Functions;
using Lyo.Mathematics.Models;
using Lyo.Metrics.Models;

namespace Lyo.Metrics;

/// <summary>Convenience helpers that apply math/statistics functions to recorded metrics data.</summary>
public static class MathExtensions
{
    public static IReadOnlyDictionary<string, double?> GetHistogramPercentiles(this MetricsSnapshot snapshot, string name, params double[] percentiles)
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));

        if (percentiles == null)
            throw new ArgumentNullException(nameof(percentiles));

        var histogram = snapshot.Histograms.Values.FirstOrDefault(h => string.Equals(h.Name, name, StringComparison.Ordinal));
        return percentiles.ToDictionary(p => p.ToString("0.###"), p => histogram.Percentile(p));
    }

    extension(HistogramData? histogram)
    {
        public DescriptiveStatisticsResult? Describe(bool sample = false)
            => histogram == null || histogram.Values.Count == 0 ? null : StatisticsFunctions.Describe([..histogram.Values], sample);

        public QuartilesResult? Quartiles() => histogram == null || histogram.Values.Count == 0 ? null : StatisticsFunctions.Quartiles([..histogram.Values]);

        public double? InterquartileRange() => histogram == null || histogram.Values.Count == 0 ? null : StatisticsFunctions.InterquartileRange([..histogram.Values]);

        public double? Percentile(double percentile) => histogram == null || histogram.Values.Count == 0 ? null : StatisticsFunctions.Percentile([..histogram.Values], percentile);

        public double[] MovingAverage(int windowSize)
            => histogram == null || histogram.Values.Count == 0 ? [] : StatisticsFunctions.MovingAverage([..histogram.Values], windowSize);

        public double[] ExponentialMovingAverage(double smoothingFactor)
            => histogram == null || histogram.Values.Count == 0 ? [] : StatisticsFunctions.ExponentialMovingAverage([..histogram.Values], smoothingFactor);

        public double[] RollingStandardDeviation(int windowSize, bool sample = true)
            => histogram == null || histogram.Values.Count == 0 ? [] : StatisticsFunctions.RollingStandardDeviation([..histogram.Values], windowSize, sample);

        public double[] RollingMedian(int windowSize)
            => histogram == null || histogram.Values.Count == 0 ? [] : StatisticsFunctions.RollingMedian([..histogram.Values], windowSize);

        public double[] RollingMinimum(int windowSize)
            => histogram == null || histogram.Values.Count == 0 ? [] : StatisticsFunctions.RollingMinimum([..histogram.Values], windowSize);

        public double[] RollingMaximum(int windowSize)
            => histogram == null || histogram.Values.Count == 0 ? [] : StatisticsFunctions.RollingMaximum([..histogram.Values], windowSize);

        public double? MedianAbsoluteDeviation() => histogram == null || histogram.Values.Count == 0 ? null : StatisticsFunctions.MedianAbsoluteDeviation([..histogram.Values]);

        public double? LatestZScore(bool sample = true) => histogram == null || histogram.Values.Count == 0 ? null : StatisticsFunctions.LatestZScore([..histogram.Values], sample);

        public bool? IsLatestValueAnomalous(double threshold = 3d, bool sample = true)
            => histogram == null || histogram.Values.Count == 0 ? null : StatisticsFunctions.IsAnomalyByZScore([..histogram.Values], threshold, sample);

        public bool? IsLatestValueAnomalousByMad(double threshold = 3.5d)
            => histogram == null || histogram.Values.Count == 0 ? null : StatisticsFunctions.IsAnomalyByMad([..histogram.Values], threshold);

        public ConfidenceIntervalResult? MeanConfidenceInterval(double confidenceLevel = 0.95d, bool sample = true)
            => histogram == null || histogram.Values.Count == 0 ? null : StatisticsFunctions.MeanConfidenceInterval([..histogram.Values], confidenceLevel, sample);

        public double? PearsonCorrelation(HistogramData? other)
        {
            if (histogram == null || other == null || histogram.Values.Count == 0 || other.Values.Count == 0)
                return null;

            return StatisticsFunctions.PearsonCorrelation([..histogram.Values], [..other.Values]);
        }
    }

    extension(MetricsService metrics)
    {
        public DescriptiveStatisticsResult? DescribeHistogram(string name, IEnumerable<(string, string)>? tags = null, bool sample = false)
            => metrics.GetHistogram(name, tags).Describe(sample);

        public QuartilesResult? GetHistogramQuartiles(string name, IEnumerable<(string, string)>? tags = null) => metrics.GetHistogram(name, tags).Quartiles();

        public double? GetHistogramInterquartileRange(string name, IEnumerable<(string, string)>? tags = null) => metrics.GetHistogram(name, tags).InterquartileRange();

        public double? GetHistogramPercentile(string name, double percentile, IEnumerable<(string, string)>? tags = null)
            => metrics.GetHistogram(name, tags).Percentile(percentile);

        public double[] GetHistogramMovingAverage(string name, int windowSize, IEnumerable<(string, string)>? tags = null)
            => metrics.GetHistogram(name, tags).MovingAverage(windowSize);

        public double[] GetHistogramExponentialMovingAverage(string name, double smoothingFactor, IEnumerable<(string, string)>? tags = null)
            => metrics.GetHistogram(name, tags).ExponentialMovingAverage(smoothingFactor);

        public double[] GetHistogramRollingStandardDeviation(string name, int windowSize, IEnumerable<(string, string)>? tags = null, bool sample = true)
            => metrics.GetHistogram(name, tags).RollingStandardDeviation(windowSize, sample);

        public double[] GetHistogramRollingMedian(string name, int windowSize, IEnumerable<(string, string)>? tags = null)
            => metrics.GetHistogram(name, tags).RollingMedian(windowSize);

        public double[] GetHistogramRollingMinimum(string name, int windowSize, IEnumerable<(string, string)>? tags = null)
            => metrics.GetHistogram(name, tags).RollingMinimum(windowSize);

        public double[] GetHistogramRollingMaximum(string name, int windowSize, IEnumerable<(string, string)>? tags = null)
            => metrics.GetHistogram(name, tags).RollingMaximum(windowSize);

        public double? GetHistogramMedianAbsoluteDeviation(string name, IEnumerable<(string, string)>? tags = null) => metrics.GetHistogram(name, tags).MedianAbsoluteDeviation();

        public double? GetLatestHistogramZScore(string name, IEnumerable<(string, string)>? tags = null, bool sample = true)
            => metrics.GetHistogram(name, tags).LatestZScore(sample);

        public bool? IsLatestHistogramValueAnomalous(string name, IEnumerable<(string, string)>? tags = null, double threshold = 3d, bool sample = true)
            => metrics.GetHistogram(name, tags).IsLatestValueAnomalous(threshold, sample);

        public bool? IsLatestHistogramValueAnomalousByMad(string name, IEnumerable<(string, string)>? tags = null, double threshold = 3.5d)
            => metrics.GetHistogram(name, tags).IsLatestValueAnomalousByMad(threshold);

        public ConfidenceIntervalResult? GetHistogramMeanConfidenceInterval(
            string name,
            IEnumerable<(string, string)>? tags = null,
            double confidenceLevel = 0.95d,
            bool sample = true)
            => metrics.GetHistogram(name, tags).MeanConfidenceInterval(confidenceLevel, sample);

        public double? GetHistogramPearsonCorrelation(
            string firstHistogramName,
            string secondHistogramName,
            IEnumerable<(string, string)>? firstTags = null,
            IEnumerable<(string, string)>? secondTags = null)
            => metrics.GetHistogram(firstHistogramName, firstTags).PearsonCorrelation(metrics.GetHistogram(secondHistogramName, secondTags));
    }
}