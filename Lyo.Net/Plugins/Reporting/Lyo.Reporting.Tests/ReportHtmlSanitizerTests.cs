using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Sanitization;

namespace Lyo.Reporting.Tests;

public sealed class ReportHtmlSanitizerTests
{
    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<SCRIPT SRC='//evil/x.js'></SCRIPT>")]
    [InlineData("<style>body{background:url(javascript:alert(1))}</style>")]
    [InlineData("<iframe src='//evil'></iframe>")]
    [InlineData("<object data='x'></object>")]
    [InlineData("<svg><script>alert(1)</script></svg>")]
    [InlineData("<!-- <script>alert(1)</script> -->")]
    public void Sanitize_DangerousElement_RemovesElementAndContent(string html)
    {
        var result = ReportHtmlSanitizer.Sanitize(html);
        Assert.DoesNotContain("script", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<", result, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("<img src=x onerror=alert(1)>", "onerror")]
    [InlineData("<div onclick=\"alert(1)\">hi</div>", "onclick")]
    [InlineData("<a href=\"javascript:alert(1)\">x</a>", "javascript")]
    [InlineData("<a href=\"JaVaScRiPt:alert(1)\">x</a>", "javascript")]
    [InlineData("<img src=\"data:text/html;base64,PHNjcmlwdD4=\">", "data:text/html")]
    [InlineData("<div style=\"width: expression(alert(1))\">x</div>", "expression")]
    public void Sanitize_DangerousAttribute_DropsAttribute(string html, string forbidden)
        => Assert.DoesNotContain(forbidden, ReportHtmlSanitizer.Sanitize(html), StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void Sanitize_AllowedMarkup_IsPreserved()
    {
        const string html = "<div style=\"margin: 10px\"><p>Hello <strong>world</strong></p><ul><li>one</li></ul></div>";
        var result = ReportHtmlSanitizer.Sanitize(html);
        Assert.Contains("<div style=\"margin: 10px\">", result, StringComparison.Ordinal);
        Assert.Contains("<strong>world</strong>", result, StringComparison.Ordinal);
        Assert.Contains("<li>one</li>", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Sanitize_TableMarkup_KeepsStructureAndSpans()
    {
        var result = ReportHtmlSanitizer.Sanitize("<table><tbody><tr><td colspan=\"2\">a</td></tr></tbody></table>");
        Assert.Contains("<td colspan=\"2\">a</td>", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Sanitize_ChartCanvas_KeepsConfigDataAttribute()
    {
        var html = $"<canvas id=\"c1\" style=\"max-height: 300px;\" {Constants.Charts.ConfigAttribute}=\"{{&quot;type&quot;:&quot;bar&quot;}}\"></canvas>";
        var result = ReportHtmlSanitizer.Sanitize(html);
        Assert.Contains("<canvas", result, StringComparison.Ordinal);
        Assert.Contains(Constants.Charts.ConfigAttribute, result, StringComparison.Ordinal);
        Assert.Contains("&quot;type&quot;:&quot;bar&quot;", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Sanitize_RelativeAndHttpLinks_AreKept()
    {
        Assert.Contains("href=\"/reports/1\"", ReportHtmlSanitizer.Sanitize("<a href=\"/reports/1\">x</a>"), StringComparison.Ordinal);
        Assert.Contains("href=\"https://example.com/\"", ReportHtmlSanitizer.Sanitize("<a href=\"https://example.com/\">x</a>"), StringComparison.Ordinal);
    }

    [Fact]
    public void Sanitize_TargetBlankLink_GetsOpenerIsolation()
        => Assert.Contains("rel=\"noopener noreferrer\"", ReportHtmlSanitizer.Sanitize("<a href=\"https://example.com\" target=\"_blank\">x</a>"), StringComparison.Ordinal);

    [Fact]
    public void Sanitize_ObfuscatedScheme_IsRejected()
        => Assert.DoesNotContain("href", ReportHtmlSanitizer.Sanitize("<a href=\"java\nscript:alert(1)\">x</a>"), StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void Sanitize_UnknownElement_DropsTagButKeepsText()
    {
        var result = ReportHtmlSanitizer.Sanitize("<marquee>scrolling</marquee>");
        Assert.Equal("scrolling", result);
    }

    [Fact]
    public void Sanitize_StrayAngleBracket_IsEscaped()
        => Assert.Equal("1 &lt; 2 &gt; 0", ReportHtmlSanitizer.Sanitize("1 < 2 > 0"));

    [Fact]
    public void Sanitize_HttpImageSrc_IsKept()
        => Assert.Contains("src=\"https://example.com/a.png\"", ReportHtmlSanitizer.Sanitize("<img src=\"https://example.com/a.png\" alt=\"x\" />"), StringComparison.Ordinal);

    [Fact]
    public void Sanitize_UnclosedDangerousElement_DropsRemainder()
        => Assert.Equal(string.Empty, ReportHtmlSanitizer.Sanitize("<script>alert(1)"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Sanitize_NullOrEmpty_ReturnsEmpty(string? html) => Assert.Equal(string.Empty, ReportHtmlSanitizer.Sanitize(html));
}
