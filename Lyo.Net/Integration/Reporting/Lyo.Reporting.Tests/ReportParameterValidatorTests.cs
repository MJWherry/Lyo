using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Postgres;
using Lyo.Reporting.Postgres.Database;

namespace Lyo.Reporting.Tests;

public sealed class ReportParameterValidatorTests
{
    [Fact]
    public void Validate_requires_missing_required_parameter()
    {
        var def = new List<ReportDefinitionParameter> { new() { Key = "ClientId", Type = nameof(ReportParameterType.Guid), Required = true } };
        var errors = ReportParameterValidator.Validate(def, []);
        Assert.Contains(errors, e => e.Contains("ClientId", StringComparison.Ordinal) && e.Contains("required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_enforces_regex_minmax_and_allowed_values()
    {
        var def = new List<ReportDefinitionParameter> {
            new() {
                Key = "Code",
                Type = nameof(ReportParameterType.String),
                Required = true,
                MinLength = 2,
                MaxLength = 4,
                ValidationRegex = "^[A-Z]+$",
                AllowedValues = """["AB","CD"]"""
            }
        };

        Assert.Contains(ReportParameterValidator.Validate(def, [new("Code", ReportParameterType.String, "A")]), e => e.Contains("at least", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ReportParameterValidator.Validate(def, [new("Code", ReportParameterType.String, "ABCDE")]), e => e.Contains("exceed", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ReportParameterValidator.Validate(def, [new("Code", ReportParameterType.String, "ab")]), e => e.Contains("pattern", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ReportParameterValidator.Validate(def, [new("Code", ReportParameterType.String, "XY")]), e => e.Contains("allowed", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(ReportParameterValidator.Validate(def, [new("Code", ReportParameterType.String, "AB")]));
    }

    [Fact]
    public void Validate_enforces_numeric_allowed_values_json()
    {
        var def = new List<ReportDefinitionParameter> { new() { Key = "PageSize", Type = nameof(ReportParameterType.Int), AllowedValues = "[1,2]" } };
        Assert.Contains(ReportParameterValidator.Validate(def, [new("PageSize", ReportParameterType.Int, "3")]), e => e.Contains("allowed", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(ReportParameterValidator.Validate(def, [new("PageSize", ReportParameterType.Int, "1")]));
    }

    [Fact]
    public void Validate_rejects_multiple_when_not_allowed()
    {
        var def = new List<ReportDefinitionParameter> {
            new() {
                Key = "Tag",
                Type = nameof(ReportParameterType.String),
                Required = true,
                AllowMultiple = false
            }
        };

        var errors = ReportParameterValidator.Validate(def, [new("Tag", ReportParameterType.String, "a"), new("Tag", ReportParameterType.String, "b")]);
        Assert.Contains(errors, e => e.Contains("multiple", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(nameof(ReportParameterType.Guid), "not-a-guid", false)]
    [InlineData(nameof(ReportParameterType.Guid), "8f14e45f-ceea-467f-9538-2f1f7f7c2d5e", true)]
    [InlineData(nameof(ReportParameterType.Int), "abc", false)]
    [InlineData(nameof(ReportParameterType.Int), "42", true)]
    [InlineData(nameof(ReportParameterType.Long), "12.5", false)]
    [InlineData(nameof(ReportParameterType.Long), "9999999999", true)]
    [InlineData(nameof(ReportParameterType.Decimal), "x", false)]
    [InlineData(nameof(ReportParameterType.Decimal), "12.34", true)]
    [InlineData(nameof(ReportParameterType.Bool), "maybe", false)]
    [InlineData(nameof(ReportParameterType.Bool), "true", true)]
    [InlineData(nameof(ReportParameterType.DateTime), "not-a-date", false)]
    [InlineData(nameof(ReportParameterType.DateTime), "2026-07-22T10:00:00Z", true)]
    [InlineData(nameof(ReportParameterType.DateOnly), "2026-13-40", false)]
    [InlineData(nameof(ReportParameterType.DateOnly), "2026-07-22", true)]
    [InlineData(nameof(ReportParameterType.TimeOnly), "25:99", false)]
    [InlineData(nameof(ReportParameterType.TimeOnly), "13:45", true)]
    [InlineData(nameof(ReportParameterType.Json), "{not json", false)]
    [InlineData(nameof(ReportParameterType.Json), """{"a":1}""", true)]
    [InlineData(nameof(ReportParameterType.Regex), "[unclosed", false)]
    [InlineData(nameof(ReportParameterType.Regex), "^[a-z]+$", true)]
    public void Validate_enforces_typed_values(string type, string value, bool valid)
    {
        var def = new List<ReportDefinitionParameter> { new() { Key = "P", Type = type, Required = true } };
        var errors = ReportParameterValidator.Validate(def, [new("P", ReportParameterType.Unknown, value)]);
        if (valid)
            Assert.Empty(errors);
        else
            Assert.Contains(errors, e => e.Contains("not a valid", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_treats_invalid_definition_pattern_as_error_not_exception()
    {
        var def = new List<ReportDefinitionParameter> { new() { Key = "Code", Type = nameof(ReportParameterType.String), ValidationRegex = "[unclosed" } };
        var errors = ReportParameterValidator.Validate(def, [new("Code", ReportParameterType.String, "x")]);
        Assert.Contains(errors, e => e.Contains("invalid validation pattern", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_rejects_oversized_definition_pattern()
    {
        var def = new List<ReportDefinitionParameter> {
            new() { Key = "Code", Type = nameof(ReportParameterType.String), ValidationRegex = new('a', ReportParameterValidator.MaxValidationRegexLength + 1) }
        };

        var errors = ReportParameterValidator.Validate(def, [new("Code", ReportParameterType.String, "x")]);
        Assert.Contains(errors, e => e.Contains("exceeding", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_rejects_unknown_keys_when_generating_from_definition()
    {
        var def = new List<ReportDefinitionParameter> { new() { Key = "Known", Type = nameof(ReportParameterType.String) } };
        var errors = ReportParameterValidator.Validate(def, [new("Known", ReportParameterType.String, "x"), new("Mystery", ReportParameterType.String, "y")], true);
        Assert.Contains(errors, e => e.Contains("Mystery", StringComparison.Ordinal) && e.Contains("Unknown parameter key", StringComparison.OrdinalIgnoreCase));
        var allowed = ReportParameterValidator.Validate(def, [new("Known", ReportParameterType.String, "x"), new("Mystery", ReportParameterType.String, "y")]);
        Assert.Empty(allowed);
    }

    [Fact]
    public void Validate_required_is_satisfied_by_encrypted_value_alone()
    {
        var def = new List<ReportDefinitionParameter> { new() { Key = "Secret", Type = nameof(ReportParameterType.String), Required = true } };
        var errors = ReportParameterValidator.Validate(def, [new() { Key = "Secret", Type = ReportParameterType.String, EncryptedValue = [1, 2, 3] }]);
        Assert.Empty(errors);
    }
}