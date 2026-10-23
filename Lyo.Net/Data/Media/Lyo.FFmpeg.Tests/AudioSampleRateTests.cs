using Lyo.Media.Models;

namespace Lyo.FFmpeg.Tests;

public class AudioSampleRateTests
{
    [Fact]
    public void AudioSampleRate_Hz48000_HertzIs48000() => Assert.Equal(48000, AudioSampleRate.Hz48000.Hertz);
}