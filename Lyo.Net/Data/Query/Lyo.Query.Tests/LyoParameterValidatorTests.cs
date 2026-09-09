using Lyo.Common.Metadata.Records;
using Lyo.Parameters;
using Lyo.Query.Models.Parameters;

namespace Lyo.Query.Tests;

/// <summary>
/// Covers the rules Job and Reporting now share. Several cases document a deliberate delta from the older job-side validation, which measured lengths against the JSON encoding,
/// treated an encrypted-only value as missing, and compiled definition regexes with no timeout or length cap.
/// </summary>
public sealed class LyoParameterValidatorTests
{
    private static LyoParameterSpec StringSpec(
        string key = "Code",
        bool required = false,
        string? regex = null,
        int? min = null,
        int? max = null,
        string? allowed = null)
        => new(key, LyoTypeInfo.String.FullName, required, regex, min, max, allowed);

    [Fact]
    [Trait("Category", "Fast")]
    public void Validate_NoSpecs_ReturnsNoErrors() => Assert.Empty(LyoParameterValidator.Validate([], [new("Anything", "\"x\"")]));

    [Fact]
    [Trait("Category", "Fast")]
    public void Validate_RequiredWithNoValue_ReportsRequired()
    {
        var errors = LyoParameterValidator.Validate([StringSpec(required: true)], []);
        Assert.Contains(errors, e => e.Contains("is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void Validate_RequiredWithEncryptedValueOnly_Passes()
        => Assert.Empty(LyoParameterValidator.Validate([StringSpec(required: true)], [new("Code", null, true)]));

    [Fact]
    [Trait("Category", "Fast")]
    public void Validate_LengthChecks_MeasureTheUnwrappedString()
    {
        // "\"AB\"" is four characters as stored and two as typed; the limits apply to what the user typed.
        Assert.Empty(LyoParameterValidator.Validate([StringSpec(min: 2, max: 2)], [new("Code", LyoTypeInfo.String.ToJson("AB"))]));
        Assert.Contains(
            LyoParameterValidator.Validate([StringSpec(min: 2)], [new("Code", LyoTypeInfo.String.ToJson("A"))]),
            e => e.Contains("at least", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(
            LyoParameterValidator.Validate([StringSpec(max: 2)], [new("Code", LyoTypeInfo.String.ToJson("ABC"))]),
            e => e.Contains("exceed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void Validate_PatternChecks_MatchTheUnwrappedString()
    {
        var spec = StringSpec(regex: "^[A-Z]{2}$");
        Assert.Empty(LyoParameterValidator.Validate([spec], [new("Code", LyoTypeInfo.String.ToJson("AB"))]));
        Assert.Contains(
            LyoParameterValidator.Validate([spec], [new("Code", LyoTypeInfo.String.ToJson("ab"))]),
            e => e.Contains("pattern", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void Validate_OverlongPattern_IsReportedRatherThanCompiled()
    {
        var spec = StringSpec(regex: new('a', LyoParameterValidator.MaxValidationRegexLength + 1));
        var errors = LyoParameterValidator.Validate([spec], [new("Code", LyoTypeInfo.String.ToJson("x"))]);
        Assert.Contains(errors, e => e.Contains($"exceeding {LyoParameterValidator.MaxValidationRegexLength}", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void Validate_InvalidPattern_IsReportedRatherThanThrown()
    {
        var errors = LyoParameterValidator.Validate([StringSpec(regex: "([")], [new("Code", LyoTypeInfo.String.ToJson("x"))]);
        Assert.Contains(errors, e => e.Contains("invalid validation pattern", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void Validate_AllowedValues_AcceptsEitherEncoding()
    {
        var spec = StringSpec(allowed: ParameterListJson.Serialize(["AB", "CD"]));
        Assert.Empty(LyoParameterValidator.Validate([spec], [new("Code", LyoTypeInfo.String.ToJson("AB"))]));
        Assert.Contains(
            LyoParameterValidator.Validate([spec], [new("Code", LyoTypeInfo.String.ToJson("XY"))]),
            e => e.Contains("allowed values", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void Validate_EmptyValue_IsTreatedAsUnset()
        => Assert.Empty(LyoParameterValidator.Validate([StringSpec(min: 5, regex: "^[A-Z]+$")], [new("Code", "")]));

    [Fact]
    [Trait("Category", "Fast")]
    public void Validate_ValueOfWrongType_ReportsTypeError()
    {
        var errors = LyoParameterValidator.Validate([new("Count", LyoTypeInfo.Int.FullName)], [new("Count", "\"abc\"")]);
        Assert.Contains(errors, e => e.Contains("is not a valid", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void Validate_UnquotedScalarValue_IsAccepted()
    {
        // Reporting stores JSON-encoded values; jobs store what the user typed. Both conventions have to pass for scalars.
        Assert.Empty(LyoParameterValidator.Validate([StringSpec()], [new("Code", "plain text")]));
        Assert.Empty(LyoParameterValidator.Validate([new("When", LyoTypeInfo.DateTime.FullName)], [new("When", "2026-01-02T03:04:05Z")]));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void Validate_UnquotedJsonValue_StillReportsTypeError()
    {
        // Structured types get no slack: quoting a malformed payload would make it a valid string and hide the error.
        var errors = LyoParameterValidator.Validate([new("Payload", LyoTypeInfo.JsonNode.FullName)], [new("Payload", "{not json")]);
        Assert.Contains(errors, e => e.Contains("is not a valid", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void Validate_UnknownKeys_ReportedOnlyWhenRejected()
    {
        LyoParameterSpec[] specs = [StringSpec("Known")];
        LyoParameterValueSpec[] values = [new("Known", LyoTypeInfo.String.ToJson("x")), new("Mystery", LyoTypeInfo.String.ToJson("y"))];
        Assert.Contains(LyoParameterValidator.Validate(specs, values, true), e => e.Contains("Unknown parameter key", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(LyoParameterValidator.Validate(specs, values));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void ValidateSpec_MissingKeyOrType_ReportsBoth()
    {
        var errors = new List<string>();
        LyoParameterValidator.ValidateSpec(new("", ""), errors);
        Assert.Contains(errors, e => e.Contains("Key is required", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, e => e.Contains("Type is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void ValidateSpec_BadLengthBounds_ReportsEach()
    {
        var errors = new List<string>();
        LyoParameterValidator.ValidateSpec(StringSpec(min: -1, max: -2), errors);
        Assert.Contains(errors, e => e.Contains("MinLength must not be negative", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, e => e.Contains("MaxLength must not be negative", StringComparison.OrdinalIgnoreCase));

        errors.Clear();
        LyoParameterValidator.ValidateSpec(StringSpec(min: 5, max: 2), errors);
        Assert.Contains(errors, e => e.Contains("must not exceed MaxLength", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void ValidateSpec_UncompilablePattern_IsReported()
    {
        var errors = new List<string>();
        LyoParameterValidator.ValidateSpec(StringSpec(regex: "(["), errors);
        Assert.Contains(errors, e => e.Contains("not a valid regular expression", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void ValidateUniqueKeys_DuplicateIgnoringCase_IsReported()
    {
        var errors = new List<string>();
        LyoParameterValidator.ValidateUniqueKeys([StringSpec("Code"), StringSpec("code")], errors);
        Assert.Single(errors);
    }
}
