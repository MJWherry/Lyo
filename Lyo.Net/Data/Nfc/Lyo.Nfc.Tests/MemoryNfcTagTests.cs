namespace Lyo.Nfc.Tests;

public sealed class MemoryNfcTagTests
{
    [Fact]
    public async Task WritePageAsync_WrongLength_Throws()
    {
        var tag = new MemoryNfcTag(NtagMemory.Ntag213, "04AABBCCDDEEFF");
        await Assert.ThrowsAsync<ArgumentException>(() => tag.WritePageAsync(4, new byte[] { 1, 2, 3 }, TestCt));
    }
}
