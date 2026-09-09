using System.Diagnostics.Metrics;
using Lyo.Metrics.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Metrics.Tests;

public class OpenTelemetryMetricsTests
{
    [Fact]
    public void AddLyoMetricsWithOpenTelemetry_RegistersOpenTelemetryMetrics()
    {
        var services = new ServiceCollection();
        services.AddLyoMetricsWithOpenTelemetry(o => o.ServiceName = "Lyo.Metrics.Tests");
        using var provider = services.BuildServiceProvider();
        Assert.IsType<OpenTelemetryMetrics>(provider.GetRequiredService<IMetrics>());
    }

    [Fact]
    public void IncrementCounter_IsObservedOnOpenTelemetryMeter()
    {
        const string meterName = "Lyo.Metrics.Tests.Listener";
        var observed = 0L;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (instrument.Meter.Name == meterName && instrument.Name == "requests_total")
                meterListener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, value, _, _) => Interlocked.Add(ref observed, value));
        listener.Start();

        var services = new ServiceCollection();
        services.AddLyoMetricsWithOpenTelemetry(o => o.ServiceName = meterName);
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IMetrics>().IncrementCounter("requests.total", 3);

        Assert.Equal(3, observed);
    }
}
