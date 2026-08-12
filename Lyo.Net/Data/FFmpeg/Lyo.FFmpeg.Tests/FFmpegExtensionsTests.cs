using Lyo.FFmpeg.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FFmpeg.Tests;

public class FFmpegExtensionsTests
{
    [Fact]
    public void AddFFmpegServices_RegistersServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFFmpegServices();
        var provider = services.BuildServiceProvider();
        var prober = provider.GetRequiredService<IAudioProber>();
        var player = provider.GetRequiredService<IAudioPlayer>();
        var converter = provider.GetRequiredService<IAudioConverter>();
        Assert.NotNull(prober);
        Assert.NotNull(player);
        Assert.NotNull(converter);
    }

    [Fact]
    public void AddFFmpegServices_WithConfigure_RegistersServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFFmpegServices(opts => opts.DefaultSampleRate = 48000);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<FFmpegOptions>();
        Assert.Equal(48000, options.DefaultSampleRate);
    }

    [Fact]
    public void AddFFmpegServices_WithConfiguration_RegistersServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configData = new Dictionary<string, string?> { ["FFmpegOptions:DefaultSampleRate"] = "22050" };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
        services.AddFFmpegServicesFromConfiguration(config);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<FFmpegOptions>();
        Assert.Equal(22050, options.DefaultSampleRate);
    }
}