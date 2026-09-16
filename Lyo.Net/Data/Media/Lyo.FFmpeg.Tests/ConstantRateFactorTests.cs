using Lyo.Media.Models;

namespace Lyo.FFmpeg.Tests;

public class ConstantRateFactorTests
{
    [Fact]
    public void ConstantRateFactor_Standard_ValueIs23() => Assert.Equal(23, ConstantRateFactor.Standard.Value);
}
