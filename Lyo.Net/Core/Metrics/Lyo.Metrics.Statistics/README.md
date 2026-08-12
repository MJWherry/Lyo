# Lyo.Metrics.Statistics

Statistical analysis extensions for `Lyo.Metrics` histograms. Provides percentile / quartile / moving-average / anomaly-detection helpers on top of the metrics primitives in
`Lyo.Metrics`.

This package exists so the base `Lyo.Metrics` package can stay dependency-light: the heavy `Lyo.Mathematics` + `Lyo.Mathematics.Functions` (F#) chain only gets pulled in when
callers explicitly opt in by referencing `Lyo.Metrics.Statistics`.

## Examples

### Usage

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

- `HistogramData` extensions: `Describe`, `Quartiles`, `InterquartileRange`, `Percentile`, `MovingAverage`, `ExponentialMovingAverage`, `RollingStandardDeviation`, `RollingMedian`,
  `RollingMinimum`, `RollingMaximum`, `MedianAbsoluteDeviation`, `LatestZScore`, `IsLatestValueAnomalous`, `IsLatestValueAnomalousByMad`, `MeanConfidenceInterval`,
  `PearsonCorrelation`.
- `MetricsService` extensions: `DescribeHistogram`, `GetHistogramQuartiles`, `GetHistogramInterquartileRange`, `GetHistogramPercentile`, `GetHistogramMovingAverage`,
  `GetHistogramExponentialMovingAverage`, `GetHistogramRollingStandardDeviation`, `GetHistogramRollingMedian`, `GetHistogramRollingMinimum`, `GetHistogramRollingMaximum`,
  `GetHistogramMedianAbsoluteDeviation`, `GetLatestHistogramZScore`, `IsLatestHistogramValueAnomalous`, `IsLatestHistogramValueAnomalousByMad`,
  `GetHistogramMeanConfidenceInterval`, `GetHistogramPearsonCorrelation`.
- `MetricsSnapshot` extension: `GetHistogramPercentiles` (batch percentile lookup).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Mathematics` — (direct, lyo)
- `Lyo.Mathematics.Functions` — (direct, lyo)
- `Lyo.Metrics` — (direct, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Scientific` — (transitive, lyo)
- `FSharp.Core` `10.0.100` — (transitive, third-party)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)