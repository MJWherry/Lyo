# Lyo.Metrics.Statistics

Histogram statistics on top of `Lyo.Metrics`: percentile, quartile, moving-average, and anomaly-detection helpers sitting on the metrics primitives in `Lyo.Metrics`.

The base `Lyo.Metrics` package stays dependency-light because this package exists: the heavy `Lyo.Mathematics` + `Lyo.Mathematics.Functions` (F#) chain is pulled in only when callers opt in by referencing `Lyo.Metrics.Statistics`.

## Examples

### How to use it

```csharp
using Lyo.Metrics;

var snapshot = metrics.GetSnapshot();
var p95 = snapshot.Histograms.Values
    .First(h => h.Name == "request.latency")
    .Percentile(95d);

// Or via MetricsService directly
var p95 = metrics.GetHistogramPercentile("request.latency", 95d);
```

## What ships

- On `HistogramData`: `Describe`, `Quartiles`, `InterquartileRange`, `Percentile`, `MovingAverage`, `ExponentialMovingAverage`, `RollingStandardDeviation`, `RollingMedian`, `RollingMinimum`, `RollingMaximum`, `MedianAbsoluteDeviation`, `LatestZScore`, `IsLatestValueAnomalous`, `IsLatestValueAnomalousByMad`, `MeanConfidenceInterval`, `PearsonCorrelation`.
- On `MetricsService`: `DescribeHistogram`, `GetHistogramQuartiles`, `GetHistogramInterquartileRange`, `GetHistogramPercentile`, `GetHistogramMovingAverage`, `GetHistogramExponentialMovingAverage`, `GetHistogramRollingStandardDeviation`, `GetHistogramRollingMedian`, `GetHistogramRollingMinimum`, `GetHistogramRollingMaximum`, `GetHistogramMedianAbsoluteDeviation`, `GetLatestHistogramZScore`, `IsLatestHistogramValueAnomalous`, `IsLatestHistogramValueAnomalousByMad`, `GetHistogramMeanConfidenceInterval`, `GetHistogramPearsonCorrelation`.
- On `MetricsSnapshot`: `GetHistogramPercentiles` (batch percentile lookup).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Mathematics` (direct, lyo)
- `Lyo.Mathematics.Functions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Scientific` (transitive, lyo)
- `FSharp.Core` `10.0.100` (transitive, third-party)