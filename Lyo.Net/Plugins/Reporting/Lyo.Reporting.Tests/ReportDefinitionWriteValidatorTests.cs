using Lyo.Common.Metadata.Records;
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
    public void ValidateDefinition_ValidDefinition_Accepts() => ReportDefinitionWriteValidator.ValidateDefinition(ValidDefinition(), MaxJsonBytes);

    [Fact]
    public void ValidateDefinition_MalformedJson_Rejects()
    {
        var definition = ValidDefinition();
        definition.ReportDataJson = "{not json";
        var ex = Assert.Throws<ReportValidationException>(() => ReportDefinitionWriteValidator.ValidateDefinition(definition, MaxJsonBytes));
        Assert.Contains("not valid JSON", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateDefinition_OversizedJson_Rejects()
    {
        var definition = ValidDefinition();
        definition.ReportDataJson = $$"""{"Title":"{{new string('x', MaxJsonBytes)}}"}""";
        var ex = Assert.Throws<ReportValidationException>(() => ReportDefinitionWriteValidator.ValidateDefinition(definition, MaxJsonBytes));
        Assert.Contains("exceeds", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateDefinition_UnknownDefaultFormat_Rejects()
    {
        var definition = ValidDefinition();
        definition.DefaultFormat = "Docx";
        var ex = Assert.Throws<ReportValidationException>(() => ReportDefinitionWriteValidator.ValidateDefinition(definition, MaxJsonBytes));
        Assert.Contains("DefaultFormat", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateDefinition_NestedParameters_Validates()
    {
        var definition = ValidDefinition();
        definition.Parameters = [new() { Key = "P", Type = "" }];
        var ex = Assert.Throws<ReportValidationException>(() => ReportDefinitionWriteValidator.ValidateDefinition(definition, MaxJsonBytes));
        Assert.Contains("Type is required", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateDefinition_DuplicateKeysCaseInsensitive_Rejects()
    {
        var definition = ValidDefinition();
        definition.Parameters = [new() { Key = "ClientId", Type = LyoTypeInfo.String.FullName }, new() { Key = "clientid", Type = LyoTypeInfo.String.FullName }];
        var ex = Assert.Throws<ReportValidationException>(() => ReportDefinitionWriteValidator.ValidateDefinition(definition, MaxJsonBytes));
        Assert.Contains("more than once", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateDefinition_DistinctKeys_Accepts()
    {
        var definition = ValidDefinition();
        definition.Parameters = [new() { Key = "ClientId", Type = LyoTypeInfo.String.FullName }, new() { Key = "Region", Type = LyoTypeInfo.String.FullName }];
        ReportDefinitionWriteValidator.ValidateDefinition(definition, MaxJsonBytes);
    }

    [Fact]
    public void ValidateParameter_InvalidRegex_Rejects()
    {
        var parameter = new ReportDefinitionParameter { Key = "P", Type = LyoTypeInfo.String.FullName, ValidationRegex = "[unclosed" };
        var ex = Assert.Throws<ReportValidationException>(() => ReportDefinitionWriteValidator.ValidateParameter(parameter));
        Assert.Contains("regular expression", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateParameter_OversizedRegex_Rejects()
    {
        var parameter = new ReportDefinitionParameter { Key = "P", Type = LyoTypeInfo.String.FullName, ValidationRegex = new('a', 501) };
        var ex = Assert.Throws<ReportValidationException>(() => ReportDefinitionWriteValidator.ValidateParameter(parameter));
        Assert.Contains("exceeds", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateParameter_InvertedMinMax_Rejects()
    {
        var parameter = new ReportDefinitionParameter {
            Key = "P",
            Type = LyoTypeInfo.String.FullName,
            MinLength = 10,
            MaxLength = 5
        };

        var ex = Assert.Throws<ReportValidationException>(() => ReportDefinitionWriteValidator.ValidateParameter(parameter));
        Assert.Contains("MinLength", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateParameter_ValidParameter_Accepts()
        => ReportDefinitionWriteValidator.ValidateParameter(
            new() {
                Key = "P",
                Type = LyoTypeInfo.Guid.FullName,
                ValidationRegex = "^[0-9a-f-]+$",
                MinLength = 1,
                MaxLength = 64
            });
}