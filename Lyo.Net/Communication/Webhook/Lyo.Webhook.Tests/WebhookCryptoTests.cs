namespace Lyo.Webhook.Tests;

public sealed class WebhookCryptoTests
{
    [Fact]
    public void HmacSha256_Known_Vector()
    {
        var key = "secret"u8.ToArray();
        var data = "payload"u8.ToArray();
        var mac = WebhookCrypto.HmacSha256(key, data);
        Assert.Equal(32, mac.Length);
        var mac2 = WebhookCrypto.HmacSha256(key, data);
        Assert.True(WebhookCrypto.FixedTimeEquals(mac, mac2));
    }

    [Fact]
    public void TryParseHex_Roundtrip()
    {
        var bytes = WebhookCrypto.TryParseHex("deadbeef");
        Assert.NotNull(bytes);
        Assert.Equal(new byte[] { 0xde, 0xad, 0xbe, 0xef }, bytes);
        Assert.Null(WebhookCrypto.TryParseHex("xyz"));
    }

    [Fact]
    public void WebhookHeaders_Try_Get_Is_Case_Insensitive()
    {
        var h = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["X-Test"] = "a" };
        Assert.True(WebhookHeaders.TryGet(h, "x-test", out var v));
        Assert.Equal("a", v);
    }
}