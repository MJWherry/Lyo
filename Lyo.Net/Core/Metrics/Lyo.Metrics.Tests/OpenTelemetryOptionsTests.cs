using Lyo.Metrics.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Metrics.Tests;

public class OpenTelemetryOptionsTests
{
    [Fact]
    public void Validate_Default_DoesNotThrow() => new OpenTelemetryOptions().Validate();

    [Fact]
    public void Validate_EmptyServiceName_Throws()
    {
        var options = new OpenTelemetryOptions { ServiceName = " " };
        Assert.ThrowsAny<ArgumentException>(options.Validate);
    }

    [Fact]
    public void Validate_InvalidEndpoint_Throws()
    {
        var options = new OpenTelemetryOptions { Endpoint = "not-a-uri" };
        Assert.ThrowsAny<ArgumentException>(options.Validate);
    }

    [Fact]
    public void Validate_UnknownProtocol_Throws()
    {
        var options = new OpenTelemetryOptions { Protocol = "udp" };
        Assert.ThrowsAny<ArgumentException>(options.Validate);
    }

    [Theory]
    [InlineData("grpc")]
    [InlineData("http/protobuf")]
    [InlineData("GRPC")]
    public void Validate_KnownProtocol_DoesNotThrow(string protocol)
        => new OpenTelemetryOptions { Protocol = protocol }.Validate();

    [Fact]
    public void Validate_ZeroMetricInterval_Throws()
    {
        var options = new OpenTelemetryOptions { MetricExportIntervalMs = 0 };
        Assert.ThrowsAny<ArgumentException>(options.Validate);
    }

    [Fact]
    public void AddLyoMetricsWithOpenTelemetryFromConfiguration_BindsSection()
    {
        var configuration = new ConfigurationManager();
        configuration["OpenTelemetry:ServiceName"] = "Bound.Service";
        configuration["OpenTelemetry:Protocol"] = "grpc";
        configuration["OpenTelemetry:MetricExportIntervalMs"] = "1000";

        var services = new ServiceCollection();
        services.AddLyoMetricsWithOpenTelemetryFromConfiguration(configuration);
        using var provider = services.BuildServiceProvider();
        var bound = provider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;
        Assert.Equal("Bound.Service", bound.ServiceName);
        Assert.Equal("grpc", bound.Protocol);
        Assert.Equal(1000, bound.MetricExportIntervalMs);
        Assert.IsType<OpenTelemetryMetrics>(provider.GetRequiredService<IMetrics>());
    }
}
