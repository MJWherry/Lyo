using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace Lyo.Metrics.OpenTelemetry;

/// <summary>Extension methods for registering OpenTelemetry metrics with dependency injection.</summary>
public static class Extensions
{
    /// <summary>Adds OpenTelemetry metrics to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="meterName">The name of the meter (defaults to "Lyo.Metrics")</param>
    /// <param name="meterVersion">Optional version of the meter</param>
    /// <param name="configureMeterProvider">Optional action to configure the MeterProviderBuilder</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLyoMetricsWithOpenTelemetry(
        this IServiceCollection services,
        string meterName = "Lyo.Metrics",
        string? meterVersion = null,
        Action<MeterProviderBuilder>? configureMeterProvider = null)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(meterName, nameof(meterName));
        services.AddOpenTelemetry()
            .WithMetrics(builder => {
                builder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(meterName, serviceVersion: meterVersion)).AddMeter(meterName);
                builder.AddConsoleExporter();
                configureMeterProvider?.Invoke(builder);
            });

        services.AddSingleton<IMetrics>(_ => new OpenTelemetryMetrics(meterName, meterVersion));
        return services;
    }
}