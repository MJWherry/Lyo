using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Postgres;
using Lyo.Reporting.Postgres.Database;

namespace Lyo.Reporting.Tests;

public sealed class ReportDefinitionWriteValidatorTests
{
    private const int MaxJsonBytes = 1024;

    private static ReportDefinition ValidDefinition()
        => new() {
            Id = Guid.NewGuid(),
            Name = "Valid",
            ReportDataJson = """{"Title":"x"}""",
            DefaultFormat = nameof(ReportFormat.Csv)
        };

    [Fact]
    public void ValidateDefinition_accepts_valid_definition() => ReportDefinitionWriteValidator.ValidateDefinition(ValidDefinition(), MaxJsonBytes);

    [Fact]
    public void ValidateDefinition_rejects_malformed_json()
    {
        var definition = ValidDefinition();
        definition.ReportDataJson = "{not json";
        var ex = Assert.Throws<ReportValidationException>(() => ReportDefinitionWriteValidator.ValidateDefinition(definition, MaxJsonBytes));
        Assert.Contains("not valid JSON", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateDefinition_rejects_oversized_json()
    {
        var definition = ValidDefinition();
        definition.ReportDataJson = $$"""{"Title":"{{new string('x', MaxJsonBytes)}}"}""";
        var ex = Assert.Throws<ReportValidationException>(() => ReportDefinitionWriteValidator.ValidateDefinition(definition, MaxJsonBytes));
        Assert.Contains("exceeds", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateDefinition_rejects_unknown_default_format()
    {
        var definition = ValidDefinition();
        definition.DefaultFormat = "Docx";
        var ex = Assert.Throws<ReportValidationException>(() => ReportDefinitionWriteValidator.ValidateDefinition(definition, MaxJsonBytes));
        Assert.Contains("DefaultFormat", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateDefinition_validates_nested_parameters()
    {
        var definition = ValidDefinition();
        definition.Parameters = [new() { Key = "P", Type = "NotAType" }];
        var ex = Assert.Throws<ReportValidationException>(() => ReportDefinitionWriteValidator.ValidateDefinition(definition, MaxJsonBytes));
        Assert.Contains("ReportParameterType", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateDefinition_rejects_duplicate_parameter_keys_case_insensitive()
    {
        var definition = ValidDefinition();
        definition.Parameters = [new() { Key = "ClientId", Type = nameof(ReportParameterType.String) }, new() { Key = "clientid", Type = nameof(ReportParameterType.String) }];
        var ex = Assert.Throws<ReportValidationException>(() => ReportDefinitionWriteValidator.ValidateDefinition(definition, MaxJsonBytes));
        Assert.Contains("more than once", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateDefinition_accepts_distinct_parameter_keys()
    {
        var definition = ValidDefinition();
        definition.Parameters = [new() { Key = "ClientId", Type = nameof(ReportParameterType.String) }, new() { Key = "Region", Type = nameof(ReportParameterType.String) }];
        ReportDefinitionWriteValidator.ValidateDefinition(definition, MaxJsonBytes);
    }

    [Fact]
    public void ValidateParameter_rejects_invalid_regex()
    {
        var parameter = new ReportDefinitionParameter { Key = "P", Type = nameof(ReportParameterType.String), ValidationRegex = "[unclosed" };
        var ex = Assert.Throws<ReportValidationException>(() => ReportDefinitionWriteValidator.ValidateParameter(parameter));
        Assert.Contains("regular expression", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateParameter_rejects_oversized_regex()
    {
        var parameter = new ReportDefinitionParameter { Key = "P", Type = nameof(ReportParameterType.String), ValidationRegex = new('a', 501) };
        var ex = Assert.Throws<ReportValidationException>(() => ReportDefinitionWriteValidator.ValidateParameter(parameter));
        Assert.Contains("exceeds", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateParameter_rejects_inverted_min_max()
    {
        var parameter = new ReportDefinitionParameter {
            Key = "P",
            Type = nameof(ReportParameterType.String),
            MinLength = 10,
            MaxLength = 5
        };

        var ex = Assert.Throws<ReportValidationException>(() => ReportDefinitionWriteValidator.ValidateParameter(parameter));
        Assert.Contains("MinLength", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateParameter_accepts_valid_parameter()
        => ReportDefinitionWriteValidator.ValidateParameter(
            new() {
                Key = "P",
                Type = nameof(ReportParameterType.Guid),
                ValidationRegex = "^[0-9a-f-]+$",
                MinLength = 1,
                MaxLength = 64
            });
}