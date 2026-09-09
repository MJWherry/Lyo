using Lyo.Common.Metadata.Records;
using Lyo.Reporting.Postgres;
using Lyo.Reporting.Postgres.Database;

namespace Lyo.Reporting.Tests;

public sealed class ReportParameterValidatorTests
{
    [Fact]
    public void Validate_MissingRequiredParameter_Reports()
    {
        var def = new List<ReportDefinitionParameter> { new() { Key = "ClientId", Type = LyoTypeInfo.Guid.FullName, Required = true } };
        var errors = ReportParameterValidator.Validate(def, []);
        Assert.Contains(errors, e => e.Contains("ClientId", StringComparison.Ordinal) && e.Contains("required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_RegexMinMaxAndAllowedValues_Enforces()
    {
        var def = new List<ReportDefinitionParameter> {
            new() {
                Key = "Code",
                Type = LyoTypeInfo.String.FullName,
                Required = true,
                MinLength = 2,
                MaxLength = 4,
                ValidationRegex = "^[A-Z]+$",
                AllowedValues = """["AB","CD"]"""
            }
        };

        Assert.Contains(ReportParameterValidator.Validate(def, [new("Code", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("A"))]), e => e.Contains("at least", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ReportParameterValidator.Validate(def, [new("Code", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("ABCDE"))]), e => e.Contains("exceed", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ReportParameterValidator.Validate(def, [new("Code", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("ab"))]), e => e.Contains("pattern", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ReportParameterValidator.Validate(def, [new("Code", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("XY"))]), e => e.Contains("allowed", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(ReportParameterValidator.Validate(def, [new("Code", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("AB"))]));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_OptionalUnset_SkipsAllowedValues(string? value)
    {
        var def = new List<ReportDefinitionParameter> {
            new() {
                Key = "Direction",
                Type = LyoTypeInfo.String.FullName,
                Required = false,
                AllowedValues = """["inbound","outbound"]"""
            }
        };

        Assert.Empty(ReportParameterValidator.Validate(def, [new("Direction", LyoTypeInfo.String, value)]));
    }

    [Fact]
    public void Validate_NumericAllowedValuesJson_Enforces()
    {
        var def = new List<ReportDefinitionParameter> { new() { Key = "PageSize", Type = LyoTypeInfo.Int.FullName, AllowedValues = "[1,2]" } };
        Assert.Contains(ReportParameterValidator.Validate(def, [new("PageSize", LyoTypeInfo.Int, "3")]), e => e.Contains("allowed", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(ReportParameterValidator.Validate(def, [new("PageSize", LyoTypeInfo.Int, "1")]));
    }

    [Fact]
    public void Validate_CollectionJsonArrayRow_Accepts()
    {
        var def = new List<ReportDefinitionParameter> {
            new() {
                Key = "Tag",
                Type = LyoTypeInfo.StringList.FullName,
                Required = true
            }
        };

        Assert.Empty(ReportParameterValidator.Validate(def, [new("Tag", LyoTypeInfo.StringList, """["a","b"]""")]));
    }

    [Theory]
    [InlineData("System.Guid", "not-a-guid", false)]
    [InlineData("System.Guid", "\"8f14e45f-ceea-467f-9538-2f1f7f7c2d5e\"", true)]
    [InlineData("System.Int32", "abc", false)]
    [InlineData("System.Int32", "42", true)]
    [InlineData("System.Int64", "12.5", false)]
    [InlineData("System.Int64", "9999999999", true)]
    [InlineData("System.Decimal", "x", false)]
    [InlineData("System.Decimal", "12.34", true)]
    [InlineData("System.Boolean", "maybe", false)]
    [InlineData("System.Boolean", "true", true)]
    [InlineData("System.DateTime", "not-a-date", false)]
    [InlineData("System.DateTime", "\"2026-07-22T10:00:00Z\"", true)]
    [InlineData("System.DateOnly", "2026-13-40", false)]
    [InlineData("System.DateOnly", "\"2026-07-22\"", true)]
    [InlineData("System.TimeOnly", "25:99", false)]
    [InlineData("System.TimeOnly", "\"13:45\"", true)]
    [InlineData("System.Text.Json.Nodes.JsonNode", "{not json", false)]
    [InlineData("System.Text.Json.Nodes.JsonNode", """{"a":1}""", true)]
    [InlineData("System.Text.RegularExpressions.Regex", "\"[unclosed\"", false)]
    [InlineData("System.Text.RegularExpressions.Regex", "\"^[a-z]+$\"", true)]
    public void Validate_TypedValues_Enforces(string type, string value, bool valid)
    {
        var def = new List<ReportDefinitionParameter> { new() { Key = "P", Type = type, Required = true } };
        var errors = ReportParameterValidator.Validate(def, [new("P", type, value)]);
        if (valid)
            Assert.Empty(errors);
        else
            Assert.Contains(errors, e => e.Contains("not a valid", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_InvalidDefinitionPattern_ReportsError()
    {
        var def = new List<ReportDefinitionParameter> { new() { Key = "Code", Type = LyoTypeInfo.String.FullName, ValidationRegex = "[unclosed" } };
        var errors = ReportParameterValidator.Validate(def, [new("Code", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("x"))]);
        Assert.Contains(errors, e => e.Contains("invalid validation pattern", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_OversizedDefinitionPattern_Rejects()
    {
        var def = new List<ReportDefinitionParameter> {
            new() { Key = "Code", Type = LyoTypeInfo.String.FullName, ValidationRegex = new('a', ReportParameterValidator.MaxValidationRegexLength + 1) }
        };

        var errors = ReportParameterValidator.Validate(def, [new("Code", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("x"))]);
        Assert.Contains(errors, e => e.Contains("exceeding", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_UnknownKeysFromDefinition_Rejects()
    {
        var def = new List<ReportDefinitionParameter> { new() { Key = "Known", Type = LyoTypeInfo.String.FullName } };
        var errors = ReportParameterValidator.Validate(def, [new("Known", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("x")), new("Mystery", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("y"))], true);
        Assert.Contains(errors, e => e.Contains("Mystery", StringComparison.Ordinal) && e.Contains("Unknown parameter key", StringComparison.OrdinalIgnoreCase));
        var allowed = ReportParameterValidator.Validate(def, [new("Known", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("x")), new("Mystery", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("y"))]);
        Assert.Empty(allowed);
    }

    [Fact]
    public void Validate_EncryptedValueAlone_SatisfiesRequired()
    {
        var def = new List<ReportDefinitionParameter> { new() { Key = "Secret", Type = LyoTypeInfo.String.FullName, Required = true } };
        var errors = ReportParameterValidator.Validate(def, [new() { Key = "Secret", Type = LyoTypeInfo.String.FullName, EncryptedValue = [1, 2, 3] }]);
        Assert.Empty(errors);
    }
}
