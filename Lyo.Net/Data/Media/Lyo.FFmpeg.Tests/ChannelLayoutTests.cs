using Lyo.Media.Models;

namespace Lyo.FFmpeg.Tests;

public class ChannelLayoutTests
{
    [Fact]
    public void ChannelLayout_Stereo_CountIs2() => Assert.Equal(2, ChannelLayout.Stereo.Count);
}
