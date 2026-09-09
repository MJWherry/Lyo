using Lyo.Common.Metadata.Records;
using Lyo.Parameters;

namespace Lyo.Formatter.Tests;

/// <summary>
/// The author-time counterpart to <see cref="LyoParameterDefaults" />: a default template is checked against the parameter's declared type while it is being written, so the
/// caption in the editor is the message that would otherwise have blocked the save.
/// </summary>
public sealed class FormatterParameterDefaultsTests
{
    private static readonly FormatterService Formatter = new(static () => new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    [Trait("Category", "Fast")]
    public void TryValidate_YesterdayOnADateTimeParameter_Passes()
    {
        Assert.True(FormatterParameterDefaults.TryValidate(Formatter, LyoTypeInfo.DateTime.FullName, "{DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}", out var rendered, out var error));
        Assert.Equal("2026-08-25", rendered);
        Assert.Null(error);
    }

    [Theory]
    [Trait("Category", "Fast")]
    [InlineData("{DateTime.Now}")]
    [InlineData("{DateTime.UtcNow}")]
    [InlineData("{DateTime.Today}")]
    public void TryValidate_BareDateTimeTokenOnADateTimeParameter_Passes(string template)
    {
        // Without a format specifier these render through ToString(), which is culture-spelled rather than ISO. That is the template an author writes first,
        // so it has to succeed.
        Assert.True(FormatterParameterDefaults.TryValidate(Formatter, LyoTypeInfo.DateTime.FullName, template, out _, out var error), error);
    }

    /// <summary>
    /// A numeric format specifier is display formatting, not a different type: <c>{Count:N0}</c> on an int parameter is still an int. The editor has to accept it; the
    /// alternative is telling the author their own format string made the value the wrong type.
    /// </summary>
    [Theory]
    [Trait("Category", "Fast")]
    [InlineData("System.Int32", "{Count:N0}", "1,234")]
    [InlineData("System.Int32", "{Count:C0}", "$1,234")]
    [InlineData("System.Decimal", "{Total:N2}", "1,234.50")]
    [InlineData("System.Decimal", "{Total:C}", "$1,234.50")]
    public void TryValidate_NumericFormatSpecifier_Passes(string type, string template, string expectedRender)
    {
        Assert.True(FormatterParameterDefaults.TryValidate(Formatter, type, template, out var rendered, out var error, new { Count = 1234, Total = 1234.50m }), error);
        Assert.Equal(expectedRender, rendered);
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void TryValidate_FractionalRenderOnAnIntParameter_StillFails()
    {
        // Leniency about formatting is not leniency about the type: accepting this would drop the ".5" without warning.
        Assert.False(FormatterParameterDefaults.TryValidate(Formatter, LyoTypeInfo.Int.FullName, "{Total:N2}", out _, out var error, new { Total = 1234.50m }));
        Assert.Contains("not a valid System.Int32", error);
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void TryValidate_RenderedValueOfTheWrongType_NamesTheDeclaredType()
    {
        Assert.False(FormatterParameterDefaults.TryValidate(Formatter, LyoTypeInfo.Int.FullName, "{DateTime.UtcNow:yyyy-MM-dd}", out _, out var error));
        Assert.Contains("not a valid System.Int32", error);
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void TryValidate_BlankTemplate_IsReportedRatherThanTreatedAsValid()
    {
        Assert.False(FormatterParameterDefaults.TryValidate(Formatter, LyoTypeInfo.DateTime.FullName, "  ", out _, out var error));
        Assert.Contains("required", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void TryValidate_ResolvesAgainstASuppliedContext()
    {
        Assert.True(
            FormatterParameterDefaults.TryValidate(
                Formatter, LyoTypeInfo.String.FullName, "{Client.Name}", out var rendered, out var error, new { Client = new { Name = "ClientName" } }));

        Assert.Equal("ClientName", rendered);
        Assert.Null(error);
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void CreateResolver_AgreesWithCoreResolution()
    {
        var spec = new LyoParameterSpec(
            "AsOfDate", LyoTypeInfo.DateTime.FullName, DefaultKind: LyoParameterDefaultKind.Expression, DefaultTemplate: "{DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}");

        Assert.True(LyoParameterDefaults.TryResolve(spec, null, FormatterParameterDefaults.CreateResolver(Formatter), out var value, out var error));
        Assert.Null(error);

        // Core normalizes the rendered text to the JSON the declared type accepts; that is why both paths share the same helper.
        Assert.Equal("\"2026-08-25\"", value);
        Assert.Empty(LyoParameterValidator.Validate([spec], [new(spec.Key, value)]));
    }
}
