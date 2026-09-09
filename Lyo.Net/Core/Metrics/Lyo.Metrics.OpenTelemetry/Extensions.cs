using Lyo.Configuration;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace Lyo.Metrics.OpenTelemetry;

/// <summary>DI helpers that register OpenTelemetry-backed <see cref="IMetrics"/>.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers OpenTelemetry metrics from a configure delegate.</summary>
        /// <param name="configure">Fills <see cref="OpenTelemetryOptions"/> after defaults.</param>
        /// <param name="configureMeterProvider">Optional extra meter exporters or instrumentation.</param>
        /// <returns>The same collection, for chaining.</returns>
        public IServiceCollection AddLyoMetricsWithOpenTelemetry(Action<OpenTelemetryOptions> configure, Action<MeterProviderBuilder>? configureMeterProvider = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new OpenTelemetryOptions();
            configure(options);
            return services.AddLyoMetricsWithOpenTelemetry(options, configureMeterProvider);
        }

        /// <summary>Binds <see cref="OpenTelemetryOptions"/> from configuration, then registers OpenTelemetry metrics.</summary>
        /// <param name="configuration">Configuration root (for example <c>builder.Configuration</c>).</param>
        /// <param name="configSectionName">Section to bind; defaults to <see cref="OpenTelemetryOptions.SectionName"/>.</param>
        /// <param name="configureMeterProvider">Optional extra meter exporters or instrumentation.</param>
        /// <returns>The same collection, for chaining.</returns>
        public IServiceCollection AddLyoMetricsWithOpenTelemetryFromConfiguration(
            IConfiguration configuration,
            string configSectionName = OpenTelemetryOptions.SectionName,
            Action<MeterProviderBuilder>? configureMeterProvider = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = LyoOptions.Bind<OpenTelemetryOptions>(configuration, configSectionName);
            return services.AddLyoMetricsWithOpenTelemetry(options, configureMeterProvider);
        }

        /// <summary>
        /// Registers <see cref="OpenTelemetryMetrics"/> as <see cref="IMetrics"/> and an OpenTelemetry meter named <see cref="OpenTelemetryOptions.ServiceName"/>.
        /// OTLP export (metrics and <c>ILogger</c>) is enabled when <see cref="OpenTelemetryOptions.Endpoint"/> or <c>OTEL_EXPORTER_OTLP_*</c> supplies a collector URL.
        /// </summary>
        /// <param name="options">Validated registration options.</param>
        /// <param name="configureMeterProvider">Optional extra meter exporters or instrumentation.</param>
        /// <returns>The same collection, for chaining.</returns>
        public IServiceCollection AddLyoMetricsWithOpenTelemetry(OpenTelemetryOptions options, Action<MeterProviderBuilder>? configureMeterProvider = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(Options.Create(options));
            services.TryAddSingleton(options);
            var otel = services.AddOpenTelemetry()
                .ConfigureResource(r => r.AddService(options.ServiceName, serviceVersion: options.ServiceVersion))
                .WithMetrics(builder => {
                    builder.AddMeter(options.ServiceName);
                    configureMeterProvider?.Invoke(builder);
                });

            if (TryResolveOtlpEndpoint(options, out var endpoint)) {
                ApplyMetricExportInterval(options);
                otel.UseOtlpExporter(ResolveOtlpProtocol(options), endpoint);
            }

            services.AddSingleton<IMetrics>(_ => new OpenTelemetryMetrics(options.ServiceName, options.ServiceVersion));
            return services;
        }
    }

    private static bool TryResolveOtlpEndpoint(OpenTelemetryOptions options, out Uri endpoint)
    {
        var raw = FirstNonEmpty(
            options.Endpoint,
            Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"),
            Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT"),
            Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT"));
        if (raw is null) {
            endpoint = null!;
            return false;
        }

        endpoint = UriHelpers.GetValidUri(raw);
        return true;
    }

    private static void ApplyMetricExportInterval(OpenTelemetryOptions options)
    {
        if (options.MetricExportIntervalMs is not { } ms)
            return;
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_METRIC_EXPORT_INTERVAL")))
            return;
        Environment.SetEnvironmentVariable("OTEL_METRIC_EXPORT_INTERVAL", ms.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    private static OtlpExportProtocol ResolveOtlpProtocol(OpenTelemetryOptions options)
    {
        var raw = FirstNonEmpty(options.Protocol, Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL")) ?? "grpc";
        return raw.Equals("http/protobuf", StringComparison.OrdinalIgnoreCase) ? OtlpExportProtocol.HttpProtobuf : OtlpExportProtocol.Grpc;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values) {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }
}
