using Lyo.Ffmpeg.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Ffmpeg.Tests;

public class FfmpegExtensionsTests
{
    [Fact]
    public void AddFfmpegServices_RegistersServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFfmpegServices();
        var provider = services.BuildServiceProvider();
        var prober = provider.GetRequiredService<IAudioProber>();
        var player = provider.GetRequiredService<IAudioPlayer>();
        var converter = provider.GetRequiredService<IAudioConverter>();
        Assert.NotNull(prober);
        Assert.NotNull(player);
        Assert.NotNull(converter);
    }

    [Fact]
    public void AddFfmpegServices_WithConfigure_RegistersServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFfmpegServices(opts => opts.DefaultSampleRate = 48000);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<FfmpegOptions>();
        Assert.Equal(48000, options.DefaultSampleRate);
    }

    [Fact]
    public void AddFfmpegServices_WithConfiguration_RegistersServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configData = new Dictionary<string, string?> { ["FfmpegOptions:DefaultSampleRate"] = "22050" };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
        services.AddFfmpegServicesFromConfiguration(config);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<FfmpegOptions>();
        Assert.Equal(22050, options.DefaultSampleRate);
    }
}