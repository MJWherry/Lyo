using Lyo.Exceptions;

namespace Lyo.Metrics.OpenTelemetry;

/// <summary>Settings for OpenTelemetry-backed <see cref="IMetrics"/> registration.</summary>
/// <remarks>
/// Bind the <see cref="SectionName"/> section from appsettings. Leave <see cref="Endpoint"/> empty so a JetBrains OpenTelemetry plugin (or any collector) can supply
/// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> at debug time — that port changes per IDE session and does not belong in committed config.
/// </remarks>
public sealed class OpenTelemetryOptions
{
    /// <summary>Configuration section name used by <c>AddLyoMetricsWithOpenTelemetryFromConfiguration</c>.</summary>
    public const string SectionName = "OpenTelemetry";

    /// <summary>Meter name and resource <c>service.name</c>. Default: <c>Lyo.Metrics</c>.</summary>
    public string ServiceName { get; set; } = "Lyo.Metrics";

    /// <summary>Optional meter / service version.</summary>
    public string? ServiceVersion { get; set; }

    /// <summary>
    /// OTLP collector URL. When empty, falls back to <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> (then the metrics- and logs-specific endpoint variables). When none are set, <see cref="IMetrics"/> still records and nothing is exported.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// OTLP protocol: <c>grpc</c> or <c>http/protobuf</c>. When empty, falls back to <c>OTEL_EXPORTER_OTLP_PROTOCOL</c>, then <c>grpc</c>.
    /// </summary>
    public string? Protocol { get; set; }

    /// <summary>
    /// Metric flush interval in milliseconds. When unset, falls back to <c>OTEL_METRIC_EXPORT_INTERVAL</c>, then the SDK default (60s).
    /// </summary>
    public int? MetricExportIntervalMs { get; set; }

    /// <summary>Throws when required settings are missing or invalid.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(ServiceName);
        if (!string.IsNullOrWhiteSpace(Endpoint))
            UriHelpers.ThrowIfInvalidAbsoluteUri(Endpoint);
        if (!string.IsNullOrWhiteSpace(Protocol))
            ArgumentHelpers.ThrowIf(!IsOtlpProtocol(Protocol), $"{nameof(Protocol)} must be grpc or http/protobuf.", nameof(Protocol));
        if (MetricExportIntervalMs is { } interval)
            ArgumentHelpers.ThrowIfNotInRange(interval, min: 1);
    }

    internal static bool IsOtlpProtocol(string protocol)
        => protocol.Equals("grpc", StringComparison.OrdinalIgnoreCase) || protocol.Equals("http/protobuf", StringComparison.OrdinalIgnoreCase);
}
