using System.Text;
using Lyo.Web.Automation.Playwright.Record;

namespace Lyo.Web.Automation.Playwright.Tests;

public sealed class NetworkBodyFormatterTests
{
    [Fact]
    public void Format_JsonContentType_IndentsTheDocument()
    {
        var body = NetworkBodyFormatter.Format("application/json; charset=utf-8", "{\"a\":1,\"b\":[2]}"u8.ToArray());

        Assert.Equal("json", body.Kind);
        Assert.Contains("\"a\": 1", body.Text, StringComparison.Ordinal);
        Assert.Contains("\n", body.Text, StringComparison.Ordinal);
        Assert.False(body.Truncated);
        Assert.Null(body.ImageDataUrl);
    }

    [Fact]
    public void Format_XmlContentType_IndentsTheDocument()
    {
        var body = NetworkBodyFormatter.Format("text/xml", "<root><child>1</child></root>"u8.ToArray());

        Assert.Equal("xml", body.Kind);
        Assert.Contains("<child>1</child>", body.Text, StringComparison.Ordinal);
        Assert.Contains("\n", body.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_PngContentType_ReturnsAnImageDataUrl()
    {
        byte[] png = [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
            0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
            0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
            0x42, 0x60, 0x82
        ];

        var body = NetworkBodyFormatter.Format("image/png", png);

        Assert.Equal("image", body.Kind);
        Assert.StartsWith("data:image/png;base64,", body.ImageDataUrl, StringComparison.Ordinal);
        Assert.Null(body.Text);
    }

    [Fact]
    public void Format_PlainText_KeepsTheText()
    {
        var body = NetworkBodyFormatter.Format("text/plain", "hello"u8.ToArray());

        Assert.Equal("text", body.Kind);
        Assert.Equal("hello", body.Text);
    }

    [Fact]
    public void Format_TextOverTheLimit_MarksItTruncated()
    {
        var raw = Encoding.UTF8.GetBytes(new string('a', NetworkBodyFormatter.TextLimit + 20));

        var body = NetworkBodyFormatter.Format("text/plain", raw);

        Assert.Equal("text", body.Kind);
        Assert.True(body.Truncated);
        Assert.Equal(NetworkBodyFormatter.TextLimit, body.Text!.Length);
    }
}
