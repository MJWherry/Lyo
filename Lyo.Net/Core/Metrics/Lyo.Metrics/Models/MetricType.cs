namespace Lyo.Metrics.Models;

/// <summary>Kind of metric being written.</summary>
public enum MetricType
{
    Counter,
    Gauge,
    Histogram,
    Timing,
    Error,
    Event
}