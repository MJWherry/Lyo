using Lyo.Media.Models;

namespace Lyo.FFmpeg.Tests;

public class MediaBitRateTests
{
    [Fact]
    public void MediaBitRate_K128_IdIs128k() => Assert.Equal("128k", MediaBitRate.K128.Id);
}
