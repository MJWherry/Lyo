namespace Lyo.Webhook.Tests;

public sealed class WebhookCryptoTests
{
    [Fact]
    public void HmacSha256_known_vector()
    {
        var key = "secret"u8.ToArray();
        var data = "payload"u8.ToArray();
        var mac = WebhookCrypto.HmacSha256(key, data);
        Assert.Equal(32, mac.Length);
        var mac2 = WebhookCrypto.HmacSha256(key, data);
        Assert.True(WebhookCrypto.FixedTimeEquals(mac, mac2));
    }

    [Fact]
    public void TryParseHex_roundtrip()
    {
        var bytes = WebhookCrypto.TryParseHex("deadbeef");
        Assert.NotNull(bytes);
        Assert.Equal(new byte[] { 0xde, 0xad, 0xbe, 0xef }, bytes);
        Assert.Null(WebhookCrypto.TryParseHex("xyz"));
    }

    [Fact]
    public void WebhookHeaders_try_get_is_case_insensitive()
    {
        var h = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["X-Test"] = "a" };
        Assert.True(WebhookHeaders.TryGet(h, "x-test", out var v));
        Assert.Equal("a", v);
    }
}