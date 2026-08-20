using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Lyo.Common.Conversion;
using Lyo.Query.Models.Parameters;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Request;
using Lyo.Reporting.Postgres.Database;

namespace Lyo.Reporting.Postgres;

/// <summary>Validates generation parameter values against definition parameter schema.</summary>
internal static class ReportParameterValidator
{
    /// <summary>Upper bound on definition regex length; matches the database column limit.</summary>
    internal const int MaxValidationRegexLength = 500;

    /// <summary>Match timeout guarding against catastrophic backtracking in definition-supplied patterns.</summary>
    internal static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(1);

    public static IReadOnlyList<string> Validate(
        IReadOnlyList<ReportDefinitionParameter> definitionParameters,
        IReadOnlyList<ReportGenerationParameterReq> requestParameters,
        bool rejectUnknownKeys = false)
    {
        var errors = new List<string>();
        var byKey = requestParameters.GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        if (rejectUnknownKeys) {
            var known = new HashSet<string>(definitionParameters.Select(d => d.Key), StringComparer.OrdinalIgnoreCase);
            var unknown = byKey.Keys.Where(k => !known.Contains(k)).OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
            if (unknown.Count > 0)
                errors.Add($"Unknown parameter key(s) not declared on the definition: {string.Join(", ", unknown)}.");
        }

        if (definitionParameters.Count == 0)
            return errors;

        foreach (var defParam in definitionParameters) {
            byKey.TryGetValue(defParam.Key, out var provided);

            // EncryptedValue satisfies "required" — encrypted-only parameters carry no plaintext Value.
            if (defParam.Required && (provided is null || provided.Count == 0 || provided.All(p => string.IsNullOrEmpty(p.Value) && p.EncryptedValue is null))) {
                errors.Add($"Parameter '{defParam.Key}' is required.");
                continue;
            }

            if (provided is null || provided.Count == 0)
                continue;

            if (!defParam.AllowMultiple && provided.Count > 1)
                errors.Add($"Parameter '{defParam.Key}' does not allow multiple values.");

            foreach (var runParam in provided) {
                var value = runParam.Value ?? string.Empty;
                if (string.IsNullOrEmpty(value))
                    continue;

                if (defParam.MinLength.HasValue && value.Length < defParam.MinLength.Value)
                    errors.Add($"Parameter '{defParam.Key}' must be at least {defParam.MinLength} characters.");

                if (defParam.MaxLength.HasValue && value.Length > defParam.MaxLength.Value)
                    errors.Add($"Parameter '{defParam.Key}' must not exceed {defParam.MaxLength} characters.");

                if (!string.IsNullOrEmpty(defParam.ValidationRegex))
                    ValidateAgainstRegex(defParam.Key, defParam.ValidationRegex, value, errors);

                if (!string.IsNullOrEmpty(defParam.AllowedValues)) {
                    var allowed = ParameterListJson.Parse(defParam.AllowedValues);
                    if (!allowed.Contains(value, StringComparer.OrdinalIgnoreCase))
                        errors.Add($"Parameter '{defParam.Key}' value '{value}' is not one of the allowed values: {string.Join(", ", allowed)}.");
                }

                if (!string.IsNullOrEmpty(value) && !IsValidForType(ParseType(defParam.Type), value))
                    errors.Add($"Parameter '{defParam.Key}' value '{value}' is not a valid {defParam.Type}.");
            }
        }

        return errors;
    }

    private static void ValidateAgainstRegex(string key, string pattern, string value, List<string> errors)
    {
        if (pattern.Length > MaxValidationRegexLength) {
            errors.Add($"Parameter '{key}' has a validation pattern exceeding {MaxValidationRegexLength} characters.");
            return;
        }

        try {
            if (!Regex.IsMatch(value, pattern, RegexOptions.None, RegexMatchTimeout))
                errors.Add($"Parameter '{key}' does not match the required pattern.");
        }
        catch (RegexMatchTimeoutException) {
            errors.Add($"Parameter '{key}' validation pattern timed out.");
        }
        catch (ArgumentException) {
            errors.Add($"Parameter '{key}' has an invalid validation pattern.");
        }
    }

    private static ReportParameterType ParseType(string type) => TypeConversion.EnumOrDefault(type, ReportParameterType.Unknown);

    /// <summary>Type coercion check for non-empty values. String/Unknown/Enum accept anything (Enum is constrained via AllowedValues).</summary>
    internal static bool IsValidForType(ReportParameterType type, string value)
        => type switch {
            ReportParameterType.Bool => bool.TryParse(value, out var _),
            ReportParameterType.DateTime => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var _),
            ReportParameterType.DateOnly => DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var _),
            ReportParameterType.TimeOnly => TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var _),
            ReportParameterType.Int => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var _),
            ReportParameterType.Long => long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var _),
            ReportParameterType.Decimal => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var _),
            ReportParameterType.Guid => Guid.TryParse(value, out var _),
            ReportParameterType.Regex => IsValidRegex(value),
            ReportParameterType.Json => IsValidJson(value),
            ReportParameterType.Xml => IsValidXml(value),
            var _ => true
        };

    private static bool IsValidRegex(string pattern)
    {
        if (pattern.Length > MaxValidationRegexLength)
            return false;

        try {
            _ = new Regex(pattern, RegexOptions.None, RegexMatchTimeout);
            return true;
        }
        catch (ArgumentException) {
            return false;
        }
    }

    private static bool IsValidJson(string value)
    {
        try {
            using var _ = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException) {
            return false;
        }
    }

    private static bool IsValidXml(string value)
    {
        try {
            _ = XDocument.Parse(value);
            return true;
        }
        catch (XmlException) {
            return false;
        }
    }
}