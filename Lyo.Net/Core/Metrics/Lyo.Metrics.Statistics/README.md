# Lyo.Metrics.Statistics

Statistical analysis extensions for `Lyo.Metrics` histograms. Provides percentile / quartile / moving-average / anomaly-detection helpers on top of the metrics primitives in `Lyo.Metrics`.

This package exists so the base `Lyo.Metrics` package can stay dependency-light: the heavy `Lyo.Mathematics` + `Lyo.Mathematics.Functions` (F#) chain only gets pulled in when callers explicitly opt in by referencing `Lyo.Metrics.Statistics`.

## Usage

```csharp
using Lyo.Metrics;

var snapshot = metrics.GetSnapshot();
var p95 = snapshot.Histograms.Values
    .First(h => h.Name == "request.latency")
    .Percentile(95d);

// Or via MetricsService directly
var p95 = metrics.GetHistogramPercentile("request.latency", 95d);
```

## What's included

- `HistogramData` extensions: `Describe`, `Quartiles`, `InterquartileRange`, `Percentile`, `MovingAverage`, `ExponentialMovingAverage`, `RollingStandardDeviation`, `RollingMedian`, `RollingMinimum`, `RollingMaximum`, `MedianAbsoluteDeviation`, `LatestZScore`, `IsLatestValueAnomalous`, `IsLatestValueAnomalousByMad`, `MeanConfidenceInterval`, `PearsonCorrelation`.
- `MetricsService` extensions: `DescribeHistogram`, `GetHistogramQuartiles`, `GetHistogramInterquartileRange`, `GetHistogramPercentile`, `GetHistogramMovingAverage`, `GetHistogramExponentialMovingAverage`, `GetHistogramRollingStandardDeviation`, `GetHistogramRollingMedian`, `GetHistogramRollingMinimum`, `GetHistogramRollingMaximum`, `GetHistogramMedianAbsoluteDeviation`, `GetLatestHistogramZScore`, `IsLatestHistogramValueAnomalous`, `IsLatestHistogramValueAnomalousByMad`, `GetHistogramMeanConfidenceInterval`, `GetHistogramPearsonCorrelation`.
- `MetricsSnapshot` extension: `GetHistogramPercentiles` (batch percentile lookup).
