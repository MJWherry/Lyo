using System.Text;
using System.Text.Json;
using Lyo.Common.Core.Conversion;
using Lyo.Parameters;
using Lyo.Query.Models.Parameters;
using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Postgres.Database;

namespace Lyo.Reporting.Postgres;

/// <summary>
/// Write-time checks for definitions and definition parameters so bad data (invalid regex, unknown format, malformed composition JSON) fails at create/update instead of
/// at generate time. Shared by the API CRUD hooks.
/// </summary>
public static class ReportDefinitionWriteValidator
{
    public static void ValidateDefinition(ReportDefinition definition, int maxReportDataJsonBytes)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(definition.ReportDataJson))
            errors.Add("ReportDataJson is required.");
        else {
            var bytes = Encoding.UTF8.GetByteCount(definition.ReportDataJson);
            if (bytes > maxReportDataJsonBytes)
                errors.Add($"ReportDataJson exceeds MaxReportDataJsonBytes ({bytes} > {maxReportDataJsonBytes}).");
            else if (!IsParseableJson(definition.ReportDataJson))
                errors.Add("ReportDataJson is not valid JSON.");
        }

        if (!string.IsNullOrWhiteSpace(definition.DefaultFormat) && TypeConversion.EnumOrNull<ReportFormat>(definition.DefaultFormat) is null)
            errors.Add($"DefaultFormat '{definition.DefaultFormat}' is not a valid ReportFormat ({string.Join(", ", Enum.GetNames<ReportFormat>())}).");

        foreach (var parameter in definition.Parameters)
            CollectParameterErrors(parameter, errors);

        LyoParameterValidator.ValidateUniqueKeys(definition.Parameters.Select(ReportParameterValidator.ToSpec), errors);
        ThrowIfAny(errors);
    }

    public static void ValidateParameter(ReportDefinitionParameter parameter)
    {
        var errors = new List<string>();
        CollectParameterErrors(parameter, errors);
        ThrowIfAny(errors);
    }

    private static void CollectParameterErrors(ReportDefinitionParameter parameter, List<string> errors)
        => LyoParameterValidator.ValidateSpec(ReportParameterValidator.ToSpec(parameter), errors);

    private static bool IsParseableJson(string json)
    {
        try {
            using var _ = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException) {
            return false;
        }
    }

    private static void ThrowIfAny(List<string> errors)
    {
        if (errors.Count > 0)
            throw new ReportValidationException(string.Join(" ", errors));
    }
}