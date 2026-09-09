using System.Text.Json;
using Lyo.Common.Metadata.Records;
using Lyo.Parameters;

namespace Lyo.Query.Tests;

/// <summary>
/// Covers the split between what a parameter's value <em>is</em> and how its default is <em>authored</em>. The split exists so a <c>System.DateTime</c> parameter can
/// default to a template and still be validated as a date, so most cases here check that the rendered text lands in a spelling the validator accepts.
/// </summary>
public sealed class LyoParameterDefaultsTests
{
    private static LyoParameterSpec ExpressionSpec(string type, string template, string key = "AsOfDate")
        => new(key, type, DefaultKind: LyoParameterDefaultKind.Expression, DefaultTemplate: template);

    [Fact]
    [Trait("Category", "Fast")]
    public void IsExpression_RequiresBothKindAndTemplate()
    {
        Assert.True(LyoParameterDefaults.IsExpression(ExpressionSpec(LyoTypeInfo.DateTime.FullName, "yesterday")));
        Assert.False(LyoParameterDefaults.IsExpression(ExpressionSpec(LyoTypeInfo.DateTime.FullName, "  ")));
        Assert.False(LyoParameterDefaults.IsExpression(new("AsOfDate", LyoTypeInfo.DateTime.FullName, DefaultTemplate: "yesterday")));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void TryResolve_LiteralDefault_PassesTheValueThroughUntouched()
    {
        Assert.True(LyoParameterDefaults.TryResolve(new("Code", LyoTypeInfo.String.FullName), "\"AB\"", null, out var value, out var error));
        Assert.Equal("\"AB\"", value);
        Assert.Null(error);
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void TryResolve_ExpressionDefault_NormalizesRenderedTextForTheDeclaredType()
    {
        var spec = ExpressionSpec(LyoTypeInfo.DateTime.FullName, "{yesterday}");
        Assert.True(LyoParameterDefaults.TryResolve(spec, null, _ => "2026-08-25", out var value, out var error));
        Assert.Null(error);

        // The rendered text is bare, so it is quoted to the JSON a DateTime accepts — that is what makes the validator pass.
        Assert.Equal("\"2026-08-25\"", value);
        Assert.Empty(LyoParameterValidator.Validate([spec], [new(spec.Key, value)]));
    }

    [Theory]
    [Trait("Category", "Fast")]
    [InlineData("System.Int32", "42", "42")]
    [InlineData("System.String", "hello", "\"hello\"")]
    [InlineData("System.Guid", "0f8fad5b-d9cb-469f-a165-70867728950e", "\"0f8fad5b-d9cb-469f-a165-70867728950e\"")]
    [InlineData("System.Boolean", "true", "true")]
    public void TryResolve_NormalizesPerDeclaredType(string type, string rendered, string expected)
    {
        Assert.True(LyoParameterDefaults.TryResolve(ExpressionSpec(type, "{t}"), null, _ => rendered, out var value, out _));
        Assert.Equal(expected, value);
    }

    [Theory]
    [Trait("Category", "Fast")]
    [InlineData("8/26/2026 10:33:14 AM")]
    [InlineData("8/26/2026")]
    [InlineData("Wed, 26 Aug 2026 10:33:14 GMT")]
    public void TryResolve_CultureSpelledDateTime_IsParsedRatherThanRejected(string rendered)
    {
        // {DateTime.Now} renders through ToString(), so this is the spelling an author gets without writing a format specifier. Rejecting it would make the
        // obvious template the one that fails.
        var spec = ExpressionSpec(LyoTypeInfo.DateTime.FullName, "{DateTime.Now}");
        Assert.True(LyoParameterDefaults.TryResolve(spec, null, _ => rendered, out var value, out var error), error);
        Assert.Empty(LyoParameterValidator.Validate([spec], [new(spec.Key, value)]));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void TryResolve_LenientBooleanText_IsParsedPerDeclaredType()
    {
        // Matches how LyoKeyedValueExtensions reads booleans, so a template rendering "yes" means the same on write as on read.
        Assert.True(LyoParameterDefaults.TryResolve(ExpressionSpec(LyoTypeInfo.Bool.FullName, "{t}"), null, _ => "yes", out var flag, out var error), error);
        Assert.Equal("true", flag);
    }

    /// <summary>
    /// A template renders through <c>ToString()</c> with whatever format specifier the author wrote, so the rendered text is display text rather than JSON. Every scalar type has
    /// to survive that round trip, because the declared type is why a default is authored as an expression.
    /// </summary>
    [Theory]
    [Trait("Category", "Fast")]
    // Integers using display formatting: {Count:N0} and {Count:C0}.
    [InlineData("System.Int32", "1,234", "1234")]
    [InlineData("System.Int32", "$1,234", "1234")]
    [InlineData("System.Int64", "9,876,543,210", "9876543210")]
    // Decimals and doubles, grouped and carrying a currency symbol.
    [InlineData("System.Decimal", "1,234.56", "1234.56")]
    [InlineData("System.Decimal", "$1,234.56", "1234.56")]
    [InlineData("System.Double", "$2,500.50", "2500.5")]
    // Dates and times spelled the way ToString() emits them.
    [InlineData("System.DateOnly", "8/26/2026", "\"2026-08-26\"")]
    [InlineData("System.TimeOnly", "10:33 AM", "\"10:33:00\"")]
    [InlineData("System.TimeSpan", "1.02:03:04", "\"1.02:03:04\"")]
    public void TryResolve_FormattedRenderPerType_IsCoercedAndPassesValidation(string type, string rendered, string expected)
    {
        var spec = ExpressionSpec(type, "{t}");
        Assert.True(LyoParameterDefaults.TryResolve(spec, null, _ => rendered, out var value, out var error), error);
        Assert.Equal(expected, value);
        Assert.Empty(LyoParameterValidator.Validate([spec], [new(spec.Key, value)]));
    }

    /// <summary>
    /// Guid format specifiers (<c>{g:N}</c>, <c>{g:B}</c>) render without dashes or wrapped in braces. <see cref="LyoTypeInfo.Guid" /> validates a Guid as a JSON string holding
    /// any parseable spelling, and <c>GetAs</c> unwraps and parses leniently, so those stay as-is rather than being re-spelled.
    /// </summary>
    [Theory]
    [Trait("Category", "Fast")]
    [InlineData("0f8fad5b-d9cb-469f-a165-70867728950e")]
    [InlineData("0f8fad5bd9cb469fa16570867728950e")]
    [InlineData("{0f8fad5b-d9cb-469f-a165-70867728950e}")]
    public void TryResolve_GuidFormatSpecifiers_RoundTripToTheSameGuid(string rendered)
    {
        var spec = ExpressionSpec(LyoTypeInfo.Guid.FullName, "{t}");
        Assert.True(LyoParameterDefaults.TryResolve(spec, null, _ => rendered, out var value, out var error), error);
        Assert.Empty(LyoParameterValidator.Validate([spec], [new(spec.Key, value)]));
        Assert.Equal(Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e"), Guid.Parse(JsonSerializer.Deserialize<string>(value!)!));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void TryResolve_FractionalTextForAnIntegerParameter_IsRejectedRatherThanTruncated()
    {
        // Truncating would store a number the template never rendered, worse than telling the author their format is wrong.
        Assert.False(LyoParameterDefaults.TryResolve(ExpressionSpec(LyoTypeInfo.Int.FullName, "{t}"), null, _ => "1,234.56", out _, out var error));
        Assert.Contains("not a valid System.Int32", error);
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void TryResolve_PercentFormattedText_IsRejected()
    {
        // {Rate:P} renders "12.34 %" for a stored 0.1234; accepting it would store a value 100 times too large.
        Assert.False(LyoParameterDefaults.TryResolve(ExpressionSpec(LyoTypeInfo.Decimal.FullName, "{t}"), null, _ => "12.34 %", out _, out var error));
        Assert.Contains("not a valid System.Decimal", error);
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void TryResolve_TextThatIsNoDateAtAll_StillFails()
    {
        // Parsing must not become a licence to accept anything: the type check is why the declared type survives an expression default.
        Assert.False(LyoParameterDefaults.TryResolve(ExpressionSpec(LyoTypeInfo.DateTime.FullName, "{t}"), null, _ => "last tuesday", out _, out var error));
        Assert.Contains("not a valid System.DateTime", error);
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void TryResolve_ExpressionDefaultWithNoResolver_FailsWithAnExplicitMessage()
    {
        Assert.False(LyoParameterDefaults.TryResolve(ExpressionSpec(LyoTypeInfo.DateTime.FullName, "{yesterday}"), null, null, out var value, out var error));
        Assert.Null(value);
        Assert.Contains("no template resolver is registered", error);
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void TryResolve_RenderedValueOfTheWrongType_IsReportedRatherThanStored()
    {
        Assert.False(LyoParameterDefaults.TryResolve(ExpressionSpec(LyoTypeInfo.Int.FullName, "{t}"), null, _ => "not a number", out var value, out var error));
        Assert.Null(value);
        Assert.Contains("not a valid System.Int32", error);
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void TryResolve_ResolverThatThrowsOrRendersNothing_IsReported()
    {
        Assert.False(
            LyoParameterDefaults.TryResolve(ExpressionSpec(LyoTypeInfo.DateTime.FullName, "{t}"), null, _ => throw new InvalidOperationException("bad token"), out _, out var thrown));

        Assert.Contains("bad token", thrown);

        Assert.False(LyoParameterDefaults.TryResolve(ExpressionSpec(LyoTypeInfo.DateTime.FullName, "{t}"), null, _ => "", out _, out var empty));
        Assert.Contains("rendered an empty value", empty);
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void HasDefault_CountsLiteralExpressionAndCiphertext()
    {
        var literal = new LyoParameterSpec("Code", LyoTypeInfo.String.FullName);
        Assert.False(LyoParameterDefaults.HasDefault(literal, null));
        Assert.True(LyoParameterDefaults.HasDefault(literal, "\"AB\""));
        Assert.True(LyoParameterDefaults.HasDefault(literal, null, true));
        Assert.True(LyoParameterDefaults.HasDefault(ExpressionSpec(LyoTypeInfo.DateTime.FullName, "{t}"), null));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void ValidateSpec_HalfSetDefaultChannel_IsReportedOnWrite()
    {
        var errors = new List<string>();
        LyoParameterValidator.ValidateSpec(ExpressionSpec(LyoTypeInfo.DateTime.FullName, "   "), errors);
        Assert.Contains(errors, e => e.Contains("DefaultTemplate is empty", StringComparison.OrdinalIgnoreCase));

        errors.Clear();
        LyoParameterValidator.ValidateSpec(new("AsOfDate", LyoTypeInfo.DateTime.FullName, DefaultTemplate: "{t}"), errors);
        Assert.Contains(errors, e => e.Contains("DefaultKind is Literal", StringComparison.OrdinalIgnoreCase));

        errors.Clear();
        LyoParameterValidator.ValidateSpec(ExpressionSpec(LyoTypeInfo.DateTime.FullName, "{t}"), errors);
        Assert.Empty(errors);
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void Normalize_BlankText_StaysUnsetRatherThanBecomingAnEmptyString()
    {
        Assert.Null(LyoParameterValueJson.Normalize(LyoTypeInfo.String.FullName, "  "));
        Assert.Null(LyoParameterValueJson.Normalize(LyoTypeInfo.String.FullName, null));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void Normalize_AlreadyValidJson_IsLeftAlone()
    {
        Assert.Equal("\"AB\"", LyoParameterValueJson.Normalize(LyoTypeInfo.String.FullName, "\"AB\""));
        Assert.Equal("42", LyoParameterValueJson.Normalize(LyoTypeInfo.Int.FullName, "42"));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void Spec_From_CarriesTheDefaultChannel()
    {
        var spec = LyoParameterSpec.From(new StubDefinition());
        Assert.Equal(LyoParameterDefaultKind.Expression, spec.DefaultKind);
        Assert.Equal("{yesterday}", spec.DefaultTemplate);
    }

    private sealed class StubDefinition : ILyoParameterDefinition
    {
        public string Key => "AsOfDate";

        public string? Value => null;

        public string? Description => null;

        public string Type => LyoTypeInfo.DateTime.FullName;

        public byte[]? EncryptedValue => null;

        public bool Required => false;

        public string? ValidationRegex => null;

        public int? MinLength => null;

        public int? MaxLength => null;

        public string? AllowedValues => null;

        public string? Options => null;

        public LyoParameterDefaultKind DefaultKind => LyoParameterDefaultKind.Expression;

        public string? DefaultTemplate => "{yesterday}";
    }
}
