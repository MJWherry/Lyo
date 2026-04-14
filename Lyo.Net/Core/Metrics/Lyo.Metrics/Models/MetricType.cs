namespace Lyo.Metrics.Models;

/// <summary>Represents the type of metric being recorded.</summary>
public enum MetricType
{
    Counter,
    Gauge,
    Histogram,
    Timing,
    Error,
    Event
}