namespace Lyo.Html.Tests;

public class HtmlServiceTests
{
    private readonly HtmlService _html = new();

    [Fact]
    public void Text_SkipsScriptAndStyle()
    {
        var text = _html.VisibleText("<p>Hello</p><script>secret()</script><style>.hidden{color:red}</style>");
        Assert.Contains("Hello", text);
        Assert.DoesNotContain("secret", text);
        Assert.DoesNotContain("hidden", text);
    }

    [Fact]
    public void Sanitize_DropsScriptEventHandlersAndJavascriptUrls()
    {
        var clean = _html.Sanitize("<p onclick=\"steal()\">Hi<script>bad()</script></p><a href=\"javascript:alert(1)\">x</a>");
        Assert.DoesNotContain("script", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", clean, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hi", clean);
    }

    [Fact]
    public void Sanitize_KeepsAllowlistedTextAndLinks()
    {
        var clean = _html.Sanitize("<p>Hello <a href=\"https://example.com\">there</a></p>");
        Assert.Contains("Hello", clean);
        Assert.Contains("there", clean);
        Assert.Contains("https://example.com", clean);
        Assert.Contains("<a", clean, StringComparison.OrdinalIgnoreCase);
    }
}