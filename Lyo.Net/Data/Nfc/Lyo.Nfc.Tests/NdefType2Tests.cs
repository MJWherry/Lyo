namespace Lyo.Nfc.Tests;

public sealed class NdefType2Tests
{
    [Fact]
    public void Encode_TextRecord_RoundTripsLanguageAndText()
    {
        var encoded = NdefType2.Encode(NdefMessage.Of(NdefTextRecord.Create("en", "hello")));
        var text = Assert.IsType<NdefTextRecord>(Assert.Single(NdefType2.Decode(encoded).Records));
        Assert.Equal("en", text.Language);
        Assert.Equal("hello", text.Text);
    }

    [Fact]
    public void Encode_UriRecord_RoundTripsHttps()
    {
        var record = NdefUriRecord.Create("https://example.com");
        Assert.Equal(0x04, record.Payload[0]);
        var encoded = NdefType2.Encode(NdefMessage.Of(record));
        var uri = Assert.IsType<NdefUriRecord>(Assert.Single(NdefType2.Decode(encoded).Records));
        Assert.Equal("https://example.com", uri.Uri);
        Assert.Equal(0x04, uri.Payload[0]);
    }

    [Fact]
    public void Decode_MissingTerminator_Throws()
    {
        var encoded = NdefType2.Encode(NdefMessage.Of(NdefTextRecord.Create("en", "hello")));
        Assert.Equal(0xFE, encoded[^1]);
        Assert.Throws<NfcException>(() => NdefType2.Decode(encoded.AsSpan(0, encoded.Length - 1)));
    }
}