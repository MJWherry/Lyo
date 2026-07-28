using Lyo.Api.Models;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Error;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Api.ApiEndpoint;

/// <summary>
/// Enforces the <c>DeniedSelectFields</c> deny-list on projected queries and exports. Projections read raw entities and bypass response mapping, so sensitive columns (e.g.
/// encrypted values that mapping would mask) must be rejected before the query runs.
/// </summary>
public static class DeniedSelectFieldPolicy
{
    /// <summary>A bare denied name blocks the field itself, any nested path ending in it, and any path passing through it.</summary>
    public static bool IsDeniedField(string field, IReadOnlyCollection<string> deniedFields)
    {
        var trimmed = field.Trim();
        foreach (var denied in deniedFields) {
            if (trimmed.Equals(denied, StringComparison.OrdinalIgnoreCase) || trimmed.EndsWith("." + denied, StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith(denied + ".", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("." + denied + ".", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>Coarse containment check: SmartFormat placeholder parsing happens later, so any mention of a denied name rejects the template.</summary>
    public static bool TemplateReferencesDeniedField(string? template, IReadOnlyCollection<string> deniedFields)
        => !string.IsNullOrEmpty(template) && deniedFields.Any(denied => template.Contains(denied, StringComparison.OrdinalIgnoreCase));

    /// <summary>Validates projected select fields and computed templates against the deny-list.</summary>
    public static List<ApiError> ValidateProjection(IEnumerable<string> selectFields, IEnumerable<ComputedField> computedFields, IReadOnlyCollection<string> deniedFields)
    {
        var errors = new List<ApiError>();
        foreach (var field in selectFields) {
            if (IsDeniedField(field, deniedFields))
                errors.Add(new(Constants.ApiErrorCodes.InvalidQuery, $"Select field '{field}' is not allowed on this endpoint."));
        }

        foreach (var computed in computedFields) {
            if (TemplateReferencesDeniedField(computed.Template, deniedFields))
                errors.Add(new(Constants.ApiErrorCodes.InvalidQuery, $"Computed field '{computed.Name}' references a field that is not allowed on this endpoint."));
        }

        return errors;
    }

    /// <summary>Validates an export request: projected query plus column mappings (property names or SmartFormat templates).</summary>
    public static List<ApiError> ValidateExport(ExportRequest request, IReadOnlyCollection<string>? deniedFields)
    {
        if (deniedFields is not { Count: > 0 })
            return [];

        var errors = ValidateProjection(request.Query?.Select ?? [], request.Query?.ComputedFields ?? [], deniedFields);
        var columnValues = (request.Columns?.Select(c => c.Value) ?? []).Concat(request.ColumnList?.Select(c => c.Value) ?? []);
        foreach (var value in columnValues) {
            if (string.IsNullOrEmpty(value))
                continue;

            var isTemplate = value.Contains('{');
            if (isTemplate ? TemplateReferencesDeniedField(value, deniedFields) : IsDeniedField(value, deniedFields))
                errors.Add(new(Constants.ApiErrorCodes.InvalidQuery, $"Export column '{value}' references a field that is not allowed on this endpoint."));
        }

        return errors;
    }
}