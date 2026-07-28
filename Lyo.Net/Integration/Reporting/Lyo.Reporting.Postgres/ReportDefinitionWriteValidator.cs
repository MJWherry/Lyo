using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lyo.Common.Conversion;
using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Postgres.Database;

namespace Lyo.Reporting.Postgres;

/// <summary>
/// Write-time validation for definitions and definition parameters so bad data (invalid regex, unknown format, malformed composition JSON) fails at create/update instead of
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

        var duplicateKeys = definition.Parameters.Where(p => !string.IsNullOrWhiteSpace(p.Key))
            .GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var key in duplicateKeys)
            errors.Add($"Parameter key '{key}' appears more than once; parameter keys must be unique per definition.");

        ThrowIfAny(errors);
    }

    public static void ValidateParameter(ReportDefinitionParameter parameter)
    {
        var errors = new List<string>();
        CollectParameterErrors(parameter, errors);
        ThrowIfAny(errors);
    }

    private static void CollectParameterErrors(ReportDefinitionParameter parameter, List<string> errors)
    {
        var label = string.IsNullOrWhiteSpace(parameter.Key) ? "(no key)" : parameter.Key;
        if (string.IsNullOrWhiteSpace(parameter.Key))
            errors.Add("Parameter Key is required.");

        if (string.IsNullOrWhiteSpace(parameter.Type) || TypeConversion.EnumOrNull<ReportParameterType>(parameter.Type) is null)
            errors.Add($"Parameter '{label}' Type '{parameter.Type}' is not a valid ReportParameterType.");

        if (!string.IsNullOrEmpty(parameter.ValidationRegex)) {
            if (parameter.ValidationRegex.Length > ReportParameterValidator.MaxValidationRegexLength)
                errors.Add($"Parameter '{label}' ValidationRegex exceeds {ReportParameterValidator.MaxValidationRegexLength} characters.");
            else {
                try {
                    _ = new Regex(parameter.ValidationRegex, RegexOptions.None, ReportParameterValidator.RegexMatchTimeout);
                }
                catch (ArgumentException) {
                    errors.Add($"Parameter '{label}' ValidationRegex is not a valid regular expression.");
                }
            }
        }

        if (parameter.MinLength is < 0)
            errors.Add($"Parameter '{label}' MinLength must not be negative.");

        if (parameter.MaxLength is < 0)
            errors.Add($"Parameter '{label}' MaxLength must not be negative.");

        if (parameter is { MinLength: { } min, MaxLength: { } max } && min > max)
            errors.Add($"Parameter '{label}' MinLength ({min}) must not exceed MaxLength ({max}).");
    }

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