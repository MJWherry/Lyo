using Lyo.Api.Models;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Error;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Services.WhereClause;

namespace Lyo.Api.ApiEndpoint;

/// <summary>
/// Enforces the <c>DeniedSelectFields</c> deny-list on projected queries and exports. Projections read raw entities and bypass response mapping, so sensitive columns (for example
/// encrypted values that mapping would mask) must be rejected before the query runs.
/// </summary>
/// <remarks>
/// The deny-list covers filtering, sorting, and including a field as well as selecting it. A caller who can filter on a value they cannot read can recover it a bit at a time by
/// watching which rows come back, so denying only the select path leaves the value readable.
/// </remarks>
public static class DeniedSelectFieldPolicy
{
    /// <summary>A bare denied name blocks the field itself, any nested path ending in it, and any path that passes through it.</summary>
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

    /// <summary>Coarse containment check. SmartFormat placeholder parsing happens later, so any mention of a denied name rejects the template.</summary>
    public static bool TemplateReferencesDeniedField(string? template, IReadOnlyCollection<string> deniedFields)
        => !string.IsNullOrEmpty(template) && deniedFields.Any(denied => template.Contains(denied, StringComparison.OrdinalIgnoreCase));

    /// <summary>Checks projected select fields and computed templates against the deny-list.</summary>
    public static List<ApiError> ValidateProjection(IEnumerable<string> selectFields, IEnumerable<ComputedField> computedFields, IReadOnlyCollection<string> deniedFields)
    {
        var errors = new List<ApiError>();
        foreach (var field in selectFields) {
            if (IsDeniedField(field, deniedFields))
                errors.Add(new(Constants.ApiErrorCodes.InvalidSelectField, $"Select field '{field}' is not allowed on this endpoint."));
        }

        foreach (var computed in computedFields) {
            if (TemplateReferencesDeniedField(computed.Template, deniedFields))
                errors.Add(new(Constants.ApiErrorCodes.InvalidComputedField, $"Computed field '{computed.Name}' references a field that is not allowed on this endpoint."));
        }

        return errors;
    }

    /// <summary>
    /// Checks the filter, sort, and include paths of any query request against the deny-list. Complements <see cref="ValidateProjection" />, which covers select.
    /// </summary>
    public static List<ApiError> ValidateRequestPaths(QueryRequestBase request, IReadOnlyCollection<string>? deniedFields)
    {
        if (deniedFields is not { Count: > 0 })
            return [];

        var errors = new List<ApiError>();
        AppendWhereClauseErrors(request.WhereClause, deniedFields, "Filter", errors, 0);
        foreach (var sort in request.SortBy) {
            if (!string.IsNullOrWhiteSpace(sort.PropertyName) && IsDeniedField(sort.PropertyName, deniedFields))
                errors.Add(new(Constants.ApiErrorCodes.InvalidSortByField, $"Sort field '{sort.PropertyName}' is not allowed on this endpoint."));
        }

        foreach (var include in request.Include) {
            if (!string.IsNullOrWhiteSpace(include) && IsDeniedField(include, deniedFields))
                errors.Add(new(Constants.ApiErrorCodes.InvalidInclude, $"Include path '{include}' is not allowed on this endpoint."));
        }

        return errors;
    }

    /// <summary>Checks a root <c>/Query</c> request: select, computed templates, outer filter/sort, join ON paths, and the nested From/Join filter scopes.</summary>
    public static List<ApiError> ValidateRootQuery(QueryReq request, IReadOnlyCollection<string>? deniedFields)
    {
        if (deniedFields is not { Count: > 0 })
            return [];

        var errors = ValidateProjection(request.Select, request.ComputedFields, deniedFields);
        errors.AddRange(ValidateRequestPaths(request, deniedFields));
        AppendWhereClauseErrors(request.From.Query?.WhereClause, deniedFields, "From.Query filter", errors, 0);
        for (var i = 0; i < request.Joins.Count; i++) {
            var join = request.Joins[i];
            AppendWhereClauseErrors(join.Query?.WhereClause, deniedFields, $"Joins[{i}].Query filter", errors, 0);
            foreach (var on in join.On) {
                foreach (var path in new[] { on.From, on.To }) {
                    if (!string.IsNullOrWhiteSpace(path) && IsDeniedField(path, deniedFields))
                        errors.Add(new(Constants.ApiErrorCodes.InvalidQuery, $"Join path '{path}' is not allowed on this endpoint."));
                }
            }
        }

        return errors;
    }

    /// <summary>Checks an export request: projected query plus column mappings (property names or SmartFormat templates).</summary>
    public static List<ApiError> ValidateExport(ExportRequest request, IReadOnlyCollection<string>? deniedFields)
    {
        if (deniedFields is not { Count: > 0 })
            return [];

        var errors = ValidateProjection(request.Query?.Select ?? [], request.Query?.ComputedFields ?? [], deniedFields);
        if (request.Query is not null)
            errors.AddRange(ValidateRequestPaths(request.Query, deniedFields));

        var columnValues = (request.Columns?.Select(c => c.Value) ?? []).Concat(request.ColumnList?.Select(c => c.Value) ?? []);
        foreach (var value in columnValues) {
            if (string.IsNullOrEmpty(value))
                continue;

            var isTemplate = value.Contains('{');
            if (isTemplate ? TemplateReferencesDeniedField(value, deniedFields) : IsDeniedField(value, deniedFields))
                errors.Add(new(Constants.ApiErrorCodes.InvalidSelectField, $"Export column '{value}' references a field that is not allowed on this endpoint."));
        }

        return errors;
    }

    private static void AppendWhereClauseErrors(WhereClause? clause, IReadOnlyCollection<string> deniedFields, string context, List<ApiError> errors, int depth)
    {
        if (clause is null)
            return;

        if (depth > WhereClauseHelpers.MaxClauseDepth) {
            errors.Add(new(Constants.ApiErrorCodes.InvalidQuery, $"{context} nesting exceeds the maximum depth of {WhereClauseHelpers.MaxClauseDepth}."));
            return;
        }

        switch (clause) {
            case ConditionClause condition:
                if (!string.IsNullOrWhiteSpace(condition.Field) && IsDeniedField(condition.Field, deniedFields))
                    errors.Add(new(Constants.ApiErrorCodes.InvalidQuery, $"{context} field '{condition.Field}' is not allowed on this endpoint."));

                break;
            case GroupClause group:
                foreach (var child in group.Children)
                    AppendWhereClauseErrors(child, deniedFields, context, errors, depth + 1);

                break;
        }

        AppendWhereClauseErrors(clause.SubClause, deniedFields, context, errors, depth + 1);
    }
}