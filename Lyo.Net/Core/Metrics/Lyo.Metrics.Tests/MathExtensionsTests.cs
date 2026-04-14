using Lyo.Metrics.Models;

namespace Lyo.Metrics.Tests;

public class MathExtensionsTests
{
    [Fact]
    public void Histogram_Percentile_ComputesExpectedValue()
    {
        var histogram = new HistogramData { Name = "latency", Values = [1d, 2d, 3d, 4d] };
        var result = histogram.Percentile(75d);
        Assert.NotNull(result);
        Assert.Equal(3.25d, result.Value, 10);
    }

    [Fact]
    public void Histogram_Quartiles_ComputesExpectedValue()
    {
        var histogram = new HistogramData { Name = "latency", Values = [1d, 2d, 3d, 4d, 5d] };
        var result = histogram.Quartiles();
        Assert.True(result.HasValue);
        Assert.Equal(2d, result.Value.Q1, 10);
        Assert.Equal(3d, result.Value.Q2, 10);
        Assert.Equal(4d, result.Value.Q3, 10);
    }

    [Fact]
    public void Histogram_MedianAbsoluteDeviation_ComputesExpectedValue()
    {
        var histogram = new HistogramData { Name = "latency", Values = [10d, 10d, 11d, 12d, 50d] };
        var result = histogram.MedianAbsoluteDeviation();
        Assert.NotNull(result);
        Assert.Equal(1d, result.Value, 10);
    }

    [Fact]
    public void Histogram_LatestZScore_ComputesExpectedValue()
    {
        var histogram = new HistogramData { Name = "latency", Values = [10d, 10d, 10d, 25d] };
        var result = histogram.LatestZScore(false);
        Assert.NotNull(result);
        Assert.True(result > 1.7d);
    }

    [Fact]
    public void Histogram_IsLatestValueAnomalous_FlagsOutlier()
    {
        var histogram = new HistogramData { Name = "latency", Values = [10d, 10d, 10d, 25d] };
        var result = histogram.IsLatestValueAnomalous(1.5d, false);
        Assert.True(result);
    }

    [Fact]
    public void Histogram_IsLatestValueAnomalousByMad_FlagsOutlier()
    {
        var histogram = new HistogramData { Name = "latency", Values = [10d, 10d, 11d, 12d, 50d] };
        var result = histogram.IsLatestValueAnomalousByMad();
        Assert.True(result);
    }

    [Fact]
    public void Histogram_ExponentialMovingAverage_ComputesExpectedValues()
    {
        var histogram = new HistogramData { Name = "latency", Values = [10d, 20d, 30d] };
        var result = histogram.ExponentialMovingAverage(0.5d);
        Assert.Equal([10d, 15d, 22.5d], result);
    }

    [Fact]
    public void MetricsService_DescribeHistogram_UsesMathLibrary()
    {
        using var metrics = new MetricsService();
        metrics.RecordHistogram("response.time", 100d);
        metrics.RecordHistogram("response.time", 200d);
        metrics.RecordHistogram("response.time", 300d);
        var result = metrics.DescribeHistogram("response.time");
        Assert.True(result.HasValue);
        Assert.Equal(200d, result.Value.Mean, 10);
        Assert.Equal(3, result.Value.Count);
    }

    [Fact]
    public void MetricsService_GetHistogramRollingStandardDeviation_ComputesValues()
    {
        using var metrics = new MetricsService();
        metrics.RecordHistogram("cpu", 1d);
        metrics.RecordHistogram("cpu", 2d);
        metrics.RecordHistogram("cpu", 3d);
        metrics.RecordHistogram("cpu", 4d);
        var result = metrics.GetHistogramRollingStandardDeviation("cpu", 2, sample: false);
        Assert.Equal(3, result.Length);
        Assert.All(result, value => Assert.Equal(0.5d, value, 10));
    }

    [Fact]
    public void MetricsService_GetHistogramMeanConfidenceInterval_ComputesBounds()
    {
        using var metrics = new MetricsService();
        metrics.RecordHistogram("cpu", 10d);
        metrics.RecordHistogram("cpu", 12d);
        metrics.RecordHistogram("cpu", 14d);
        metrics.RecordHistogram("cpu", 16d);
        metrics.RecordHistogram("cpu", 18d);
        var result = metrics.GetHistogramMeanConfidenceInterval("cpu");
        Assert.True(result.HasValue);
        Assert.Equal(14d, result.Value.Mean, 10);
        Assert.True(result.Value.LowerBound < 14d);
        Assert.True(result.Value.UpperBound > 14d);
    }

    [Fact]
    public void MetricsService_GetHistogramPearsonCorrelation_ComputesExpectedValue()
    {
        using var metrics = new MetricsService();
        foreach (var value in new[] { 1d, 2d, 3d, 4d }) {
            metrics.RecordHistogram("requests", value);
            metrics.RecordHistogram("latency", value * 2d);
        }

        var result = metrics.GetHistogramPearsonCorrelation("requests", "latency");
        Assert.NotNull(result);
        Assert.Equal(1d, result.Value, 10);
    }

    [Fact]
    public void MetricsSnapshot_GetHistogramPercentiles_ComputesRequestedValues()
    {
        var snapshot = new MetricsSnapshot { Histograms = new() { ["latency"] = new() { Name = "latency", Values = [100d, 200d, 300d, 400d] } } };
        var result = snapshot.GetHistogramPercentiles("latency", 50d, 95d);
        Assert.NotNull(result["50"]);
        Assert.NotNull(result["95"]);
        Assert.Equal(250d, result["50"]!.Value, 10);
        Assert.Equal(385d, result["95"]!.Value, 10);
    }
}