using Lyo.FFmpeg.Models;
using Lyo.Media.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FFmpeg.Tests;

public class FFmpegExtensionsTests
{
    [Fact]
    public void AddFFmpegServices_RegistersDistinctAudioAndVideoTypes()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFFmpegServices();
        var provider = services.BuildServiceProvider();
        var audio = provider.GetRequiredService<IAudioConverter>();
        var video = provider.GetRequiredService<IVideoConverter>();
        Assert.IsType<FFmpegAudioConverter>(audio);
        Assert.IsType<FFmpegVideoConverter>(video);
        Assert.NotSame(audio, video);
        Assert.IsType<FFmpegAudioProber>(provider.GetRequiredService<IAudioProber>());
        Assert.IsType<FFmpegVideoProber>(provider.GetRequiredService<IVideoProber>());
        Assert.IsType<FFmpegAudioPlayer>(provider.GetRequiredService<IAudioPlayer>());
        Assert.IsType<FFmpegVideoPlayer>(provider.GetRequiredService<IVideoPlayer>());
        Assert.NotSame(provider.GetRequiredService<IAudioProber>(), provider.GetRequiredService<IVideoProber>());
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
    public void AddFFmpegServices_WithInstance_ValidatesAndRegisters()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFFmpegServices(new FFmpegOptions { DefaultChannels = 1 });
        var provider = services.BuildServiceProvider();
        Assert.Equal(1, provider.GetRequiredService<FFmpegOptions>().DefaultChannels);
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
