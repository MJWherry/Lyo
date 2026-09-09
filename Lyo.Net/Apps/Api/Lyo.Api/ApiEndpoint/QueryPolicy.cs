using Lyo.Api.Models;
using Lyo.Api.Models.Error;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Api.ApiEndpoint;

/// <summary>
/// Request-shape limits for a query endpoint, independent of the entity CLR type so the typed and dynamic builders share one implementation.
/// </summary>
/// <remarks>
/// Every limit is optional and unset means unbounded. The dynamic routes used to bypass these entirely, which let a caller ask for a hundred include paths against a route the
/// host believed was capped at five.
/// </remarks>
public sealed record QueryPolicy
{
    /// <summary>Most navigation include paths allowed per request.</summary>
    public int? MaxIncludePathCount { get; init; }

    /// <summary>Largest page size once at least one include path is present, since each include multiplies the rows the database materializes.</summary>
    public int? MaxIncludePageSize { get; init; }

    /// <summary>Most key sets allowed in a key-scoped request.</summary>
    public int? MaxKeySetCount { get; init; }

    /// <summary>Most projected select fields allowed.</summary>
    public int? MaxSelectFieldCount { get; init; }

    /// <summary>Most computed fields allowed.</summary>
    public int? MaxComputedFieldCount { get; init; }

    /// <summary>Longest character length of one computed field template.</summary>
    public int? MaxComputedTemplateLength { get; init; }

    /// <summary>Field names the caller may not select, filter, sort, or include. See <see cref="DeniedSelectFieldPolicy" />.</summary>
    public IReadOnlyCollection<string> DeniedSelectFields { get; init; } = [];
}

/// <summary>Applies a <see cref="QueryPolicy" /> to incoming query requests. Used by both the typed and dynamic endpoint builders.</summary>
public static class QueryPolicyValidator
{
    /// <summary>Checks an entity-shaped query request against the policy.</summary>
    /// <param name="request">Incoming request.</param>
    /// <param name="policy">Limits to apply.</param>
    public static List<ApiError> Validate(QueryConcreteReq request, QueryPolicy policy)
    {
        var errors = ValidateShared(request.Include.Count, request.Amount, request.Keys.Count, policy);
        errors.AddRange(DeniedSelectFieldPolicy.ValidateRequestPaths(request, policy.DeniedSelectFields));
        return errors;
    }

    /// <summary>Checks a projected query request against the policy, including select, computed field, and template limits.</summary>
    /// <param name="request">Incoming request.</param>
    /// <param name="policy">Limits to apply.</param>
    public static List<ApiError> Validate(ProjectionQueryReq request, QueryPolicy policy)
    {
        var errors = ValidateShared(request.Include.Count, request.Amount, request.Keys.Count, policy);
        if (policy.MaxSelectFieldCount is { } maxSelect && request.Select.Count > maxSelect)
            errors.Add(new(Constants.ApiErrorCodes.InvalidSelectField, $"Select field count ({request.Select.Count}) exceeds endpoint maximum ({maxSelect})."));

        if (policy.MaxComputedFieldCount is { } maxComputed && request.ComputedFields.Count > maxComputed)
            errors.Add(new(Constants.ApiErrorCodes.InvalidComputedField, $"Computed field count ({request.ComputedFields.Count}) exceeds endpoint maximum ({maxComputed})."));

        if (policy.MaxComputedTemplateLength is { } maxTemplateLength) {
            foreach (var computed in request.ComputedFields.Where(c => c.Template.Length > maxTemplateLength)) {
                errors.Add(
                    new(
                        Constants.ApiErrorCodes.InvalidComputedField,
                        $"Computed field '{computed.Name}' template length ({computed.Template.Length}) exceeds endpoint maximum ({maxTemplateLength})."));
            }
        }

        if (policy.DeniedSelectFields.Count > 0) {
            errors.AddRange(DeniedSelectFieldPolicy.ValidateProjection(request.Select, request.ComputedFields, policy.DeniedSelectFields));
            errors.AddRange(DeniedSelectFieldPolicy.ValidateRequestPaths(request, policy.DeniedSelectFields));
        }

        return errors;
    }

    private static List<ApiError> ValidateShared(int includeCount, int? amount, int keyCount, QueryPolicy policy)
    {
        var errors = new List<ApiError>();
        if (policy.MaxIncludePathCount is { } maxIncludes && includeCount > maxIncludes)
            errors.Add(new(Constants.ApiErrorCodes.InvalidInclude, $"Include path count ({includeCount}) exceeds endpoint maximum ({maxIncludes})."));

        if (policy.MaxIncludePageSize is { } maxIncludePageSize && includeCount > 0 && (amount ?? 0) > maxIncludePageSize)
            errors.Add(new(Constants.ApiErrorCodes.InvalidPaging, $"Page size ({amount ?? 0}) exceeds endpoint include-query maximum ({maxIncludePageSize})."));

        if (policy.MaxKeySetCount is { } maxKeySets && keyCount > maxKeySets)
            errors.Add(new(Constants.ApiErrorCodes.InvalidQuery, $"Key set count ({keyCount}) exceeds endpoint maximum ({maxKeySets})."));

        return errors;
    }
}
