using Lyo.Common.Identifiers;
using Lyo.Exceptions.Models;

namespace Lyo.Common.Tests;

public class SnowflakeGeneratorTests
{
    [Fact]
    public void Next_ReturnsDifferentValuesOnEachCall()
    {
        var gen = new SnowflakeGenerator();
        Assert.NotEqual(gen.Next(), gen.Next());
    }

    [Fact]
    public void Next_ReturnsMonotonicallyIncreasingValues()
    {
        var gen = new SnowflakeGenerator();
        var previous = gen.Next();
        for (var i = 0; i < 100; i++) {
            var current = gen.Next();
            Assert.True(current.CompareTo(previous) > 0, $"Snowflake at step {i} was not greater than the previous one.");
            previous = current;
        }
    }

    [Fact]
    public void Next_TimestampRoundTrips()
    {
        var gen = new SnowflakeGenerator();
        var before = DateTimeOffset.UtcNow;
        var sf = gen.Next();
        var after = DateTimeOffset.UtcNow;

        var ts = sf.GetTimestampUtc(gen.Layout);
        Assert.True(ts >= before.AddMilliseconds(-1), "Embedded timestamp is before generation time.");
        Assert.True(ts <= after.AddMilliseconds(1), "Embedded timestamp is after generation time.");
    }

    [Fact]
    public void Next_MachineIdIsEmbedded()
    {
        const int machineId = 42;
        var gen = new SnowflakeGenerator(machineId);
        var sf = gen.Next();

        // Machine ID occupies bits 12–21 (the 10 bits above the sequence field).
        var extractedMachineId = (int)((sf.Value >> 12) & 0x3FF);
        Assert.Equal(machineId, extractedMachineId);
    }

    [Fact]
    public void Next_TwoGeneratorsWithDifferentMachineIdsProduceDifferentIds()
    {
        var gen1 = new SnowflakeGenerator(1);
        var gen2 = new SnowflakeGenerator(2);
        Assert.NotEqual(gen1.Next(), gen2.Next());
    }

    [Fact]
    public void NextBulk_ReturnsCorrectCount()
    {
        var gen = new SnowflakeGenerator();
        Assert.Equal(100, gen.NextBulk(100).Length);
    }

    [Fact]
    public void NextBulk_AllUnique()
    {
        var gen = new SnowflakeGenerator();
        var ids = gen.NextBulk(500);
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void NextBulk_AreStrictlyAscending()
    {
        var gen = new SnowflakeGenerator();
        var ids = gen.NextBulk(200);
        for (var i = 1; i < ids.Length; i++)
            Assert.True(ids[i].CompareTo(ids[i - 1]) > 0, $"Element {i} is not greater than element {i - 1}.");
    }

    [Fact]
    public void NextBulk_ThrowsOnZero() => Assert.Throws<ArgumentOutsideRangeException>(() => new SnowflakeGenerator().NextBulk(0));

    [Fact]
    public void NextBulk_ThrowsOnNegative() => Assert.Throws<ArgumentOutsideRangeException>(() => new SnowflakeGenerator().NextBulk(-1));

    [Fact]
    public void Constructor_ThrowsOnNegativeMachineId() => Assert.Throws<ArgumentException>(() => new SnowflakeGenerator(-1));

    [Fact]
    public void Constructor_ThrowsOnMachineIdAboveMax() => Assert.Throws<ArgumentException>(() => new SnowflakeGenerator(1024));

    [Fact]
    public void Constructor_AcceptsBoundaryMachineIds()
    {
        _ = new SnowflakeGenerator(0);
        _ = new SnowflakeGenerator(1023);
    }

    [Fact]
    public void Layout_TimestampShiftBitsIs22()
    {
        var gen = new SnowflakeGenerator();
        Assert.Equal(22, gen.Layout.TimestampShiftBits);
    }

    [Fact]
    public void Layout_EpochMatchesDefaultEpoch()
    {
        var gen = new SnowflakeGenerator();
        Assert.Equal(SnowflakeGenerator.DefaultEpochMs, gen.Layout.EpochMillisecondsSinceUnixEpoch);
    }

    [Fact]
    public void Shared_IsNotNull() => Assert.NotNull(SnowflakeGenerator.Shared);

    [Fact]
    public void Shared_ProducesValidSnowflakes()
    {
        var sf = SnowflakeGenerator.Shared.Next();
        Assert.True(sf.Value > 0);
    }

    [Fact]
    public void CustomEpoch_IsReflectedInTimestamp()
    {
        var customEpochMs = new DateTimeOffset(2015, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var gen = new SnowflakeGenerator(0, customEpochMs);
        Assert.Equal(customEpochMs, gen.Layout.EpochMillisecondsSinceUnixEpoch);
    }
}
