using Lyo.Reporting.Models.Builders;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Models;
using Lyo.Reporting.Models.Sanitization;

namespace Lyo.Reporting.Tests;

public sealed class ReportLayoutCssTests
{
    [Fact]
    public void ResolvePadding_Unset_UsesThemeFallback()
    {
        Assert.Equal("24px", ReportLayoutCss.ResolvePadding(new(), true));
        Assert.Equal("40px", ReportLayoutCss.ResolvePadding(new(), false));
    }

    [Fact]
    public void ResolvePadding_ShorthandAndPerSide_Resolves()
    {
        var layout = new Layout { Padding = "16px", PaddingTop = "8px" };
        Assert.Equal("8px 16px 16px 16px", ReportLayoutCss.ResolvePadding(layout, false));
    }

    [Fact]
    public void ResolvePadding_ExpressionInjection_DropsToFallback()
    {
        var layout = new Layout { Padding = "expression(alert(1))" };
        Assert.Equal("40px", ReportLayoutCss.ResolvePadding(layout, false));
    }

    [Fact]
    public void ResolvePageRule_LetterPortraitWithMargin_EmitsCss()
    {
        var css = ReportLayoutCss.ResolvePageRule(new() { PageSize = "Letter", Orientation = "Portrait", Margin = "1in" });
        Assert.Contains("size: letter portrait", css, StringComparison.Ordinal);
        Assert.Contains("margin: 1in", css, StringComparison.Ordinal);
        Assert.StartsWith("@page {", css, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolvePageRule_AutoWithoutMargin_ReturnsEmpty() => Assert.Equal(string.Empty, ReportLayoutCss.ResolvePageRule(new()));

    [Theory]
    [InlineData("Letter", "Portrait", "letter portrait")]
    [InlineData("Letter", "Landscape", "letter landscape")]
    [InlineData("Legal", "Portrait", "legal portrait")]
    [InlineData("Legal", "Landscape", "legal landscape")]
    [InlineData("Tabloid", "Portrait", "tabloid portrait")]
    [InlineData("Tabloid", "Landscape", "tabloid landscape")]
    [InlineData("A3", "Portrait", "A3 portrait")]
    [InlineData("A3", "Landscape", "A3 landscape")]
    [InlineData("A4", "Portrait", "A4 portrait")]
    [InlineData("A4", "Landscape", "A4 landscape")]
    [InlineData("A5", "Portrait", "A5 portrait")]
    [InlineData("A5", "Landscape", "A5 landscape")]
    public void ResolvePageRule_NamedPapers_EmitsSize(string paper, string orientation, string expected)
    {
        var css = ReportLayoutCss.ResolvePageRule(new() { PageSize = paper, Orientation = orientation });
        Assert.Contains("size: " + expected, css, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_NewLayoutFields_RoundTrips()
    {
        var report = ReportBuilder<object>.New()
            .SetTitle("T")
            .SetLayout(l => l.SetPadding("12px").SetMargin("1in").SetRootComponentType("Lyo.Reporting.Business.Example.Invoice"))
            .Build();

        var json = ReportJson.Serialize(report);
        var back = ReportJson.Deserialize<object>(json);
        Assert.Equal("12px", back.Layout.Padding);
        Assert.Equal("1in", back.Layout.Margin);
        Assert.Equal("Lyo.Reporting.Business.Example.Invoice", back.Layout.RootComponentType);
    }

    [Fact]
    public void Deserialize_OldPayload_OmitsNewFields()
    {
        const string json = """{"title":"Old","sections":[]}""";
        var report = ReportJson.Deserialize<object>(json);
        Assert.Equal("Old", report.Title);
        Assert.Null(report.Layout.Padding);
        Assert.Null(report.Layout.Margin);
        Assert.Null(report.Layout.RootComponentType);
    }

    [Fact]
    public void SanitizeCss_JavascriptDeclaration_IsStripped()
        => Assert.DoesNotContain("javascript", ReportHtmlSanitizer.SanitizeCss("width: 10px; background: url(javascript:alert(1))"), StringComparison.OrdinalIgnoreCase);
}