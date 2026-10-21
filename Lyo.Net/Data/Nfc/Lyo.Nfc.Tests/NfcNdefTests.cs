namespace Lyo.Nfc.Tests;

public sealed class NfcNdefTests
{
    [Fact]
    public async Task WriteAsync_TextOnMemoryTag_ReadAsyncReturnsSameMessage()
    {
        var tag = new MemoryNfcTag(NtagMemory.Ntag213, "04AABBCCDDEEFF");
        var written = NdefMessage.Of(NdefTextRecord.Create("en", "hello"));
        await NfcNdef.WriteAsync(tag, NtagMemory.Ntag213, written, TestCt);
        var text = Assert.IsType<NdefTextRecord>(Assert.Single((await NfcNdef.ReadAsync(tag, NtagMemory.Ntag213, TestCt)).Records));
        Assert.Equal("en", text.Language);
        Assert.Equal("hello", text.Text);
    }

    [Fact]
    public async Task ReadAsync_ShortText_StopsAfterFirstFourPages()
    {
        var inner = new MemoryNfcTag(NtagMemory.Ntag213, "04AABBCCDDEEFF");
        await NfcNdef.WriteAsync(inner, NtagMemory.Ntag213, NdefMessage.Of(NdefTextRecord.Create("en", "hello")), TestCt);
        var tag = new CountingTag(inner);
        var text = Assert.IsType<NdefTextRecord>(Assert.Single((await NfcNdef.ReadAsync(tag, NtagMemory.Ntag213, TestCt)).Records));
        Assert.Equal("hello", text.Text);
        Assert.Equal(4, tag.PagesRead);
    }

    [Fact]
    public async Task ReadUserAreaAsync_NoNdef_ReadsEachPageOnce()
    {
        var tag = new CountingTag(new(NtagMemory.Ntag213, "04AABBCCDDEEFF"));
        var user = await NfcNdef.ReadUserAreaAsync(tag, NtagMemory.Ntag213, TestCt);
        Assert.Equal(NtagMemory.Ntag213.UserCapacityBytes, user.Length);
        Assert.Equal(NtagMemory.Ntag213.UserPageCount, tag.PagesRead);
        Assert.Throws<NfcException>(() => NdefType2.Decode(user));
        Assert.Equal(NtagMemory.Ntag213.UserPageCount, tag.PagesRead);
    }

    private sealed class CountingTag(MemoryNfcTag inner) : INfcTag
    {
        public int PagesRead { get; private set; }

        public string UidHex => inner.UidHex;

        public async Task<byte[]> ReadPagesAsync(int startPage, int pageCount, CancellationToken cancellationToken)
        {
            PagesRead += pageCount;
            return await inner.ReadPagesAsync(startPage, pageCount, cancellationToken).ConfigureAwait(false);
        }

        public Task WritePageAsync(int page, ReadOnlyMemory<byte> fourBytes, CancellationToken cancellationToken) => inner.WritePageAsync(page, fourBytes, cancellationToken);

        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}