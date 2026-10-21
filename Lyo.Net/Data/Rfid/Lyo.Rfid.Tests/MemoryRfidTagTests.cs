namespace Lyo.Rfid.Tests;

public sealed class MemoryRfidTagTests
{
    [Fact]
    public async Task WriteBlockAsync_WrongLength_Throws()
    {
        var tag = new MemoryRfidTag(4, 8, "E004035012345678");
        await Assert.ThrowsAsync<ArgumentException>(() => tag.WriteBlockAsync(0, new byte[] { 1, 2, 3 }, TestCt));
    }

    [Fact]
    public async Task WriteBlockAsync_ThenReadBlocks_RoundTrips()
    {
        var tag = new MemoryRfidTag(4, 8, "E004035012345678");
        var written = new byte[] { 9, 8, 7, 6 };
        await tag.WriteBlockAsync(1, written, TestCt);
        var read = await tag.ReadBlocksAsync(0, 2, TestCt);
        Assert.Equal(written, read.AsSpan(4, 4).ToArray());
    }
}