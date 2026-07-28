using Lyo.Api.Models.Error;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using ApiErrorCodes = Lyo.Api.Models.Constants.ApiErrorCodes;

namespace Lyo.Api.Services.Crud.Read.Query.Root;

/// <summary>Validates root <see cref="QueryReq" /> against an entity registry (allowlist, aliases, From-only outer where/sort, nested scope rules).</summary>
public static class RootQueryValidator
{
    public static List<ApiError> Validate(QueryReq request, RootQueryEntityRegistry registry)
    {
        var errors = new List<ApiError>();
        if (request.Include.Count > 0)
            errors.Add(Err("Include is not supported on root /Query."));

        if (string.IsNullOrWhiteSpace(request.From.Alias))
            errors.Add(Err("From.Alias is required."));

        if (string.IsNullOrWhiteSpace(request.From.EntityType))
            errors.Add(Err("From.EntityType is required."));

        if (request.Select.Count == 0)
            errors.Add(Err("Select requires at least one field."));

        if (!string.IsNullOrWhiteSpace(request.From.EntityType) && !registry.TryGet(request.From.EntityType, out var _))
            errors.Add(Err($"Unknown or disallowed From.EntityType '{request.From.EntityType}'."));

        ValidateSourceScope(request.From.Query, "From.Query", errors);
        var aliases = new Dictionary<string, RootQueryEntityEntry>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(request.From.Alias) && registry.TryGet(request.From.EntityType, out var fromEntry))
            aliases[request.From.Alias.Trim()] = fromEntry;

        for (var i = 0; i < request.Joins.Count; i++) {
            var join = request.Joins[i];
            var prefix = $"Joins[{i}]";
            if (join.Type is not (JoinType.Inner or JoinType.Left))
                errors.Add(Err($"{prefix}.Type must be Inner or Left."));

            if (string.IsNullOrWhiteSpace(join.Alias))
                errors.Add(Err($"{prefix}.Alias is required."));
            else if (aliases.ContainsKey(join.Alias.Trim()))
                errors.Add(Err($"{prefix}.Alias '{join.Alias}' is duplicated."));

            if (string.IsNullOrWhiteSpace(join.EntityType))
                errors.Add(Err($"{prefix}.EntityType is required."));
            else if (!registry.TryGet(join.EntityType, out var joinEntry))
                errors.Add(Err($"{prefix}.EntityType '{join.EntityType}' is unknown or disallowed."));
            else if (!string.IsNullOrWhiteSpace(join.Alias))
                aliases[join.Alias.Trim()] = joinEntry;

            if (join.On.Count == 0)
                errors.Add(Err($"{prefix}.On requires at least one clause."));

            ValidateSourceScope(join.Query, $"{prefix}.Query", errors);
            foreach (var on in join.On) {
                ValidateAliasPropertyPath(on.From, aliases, $"{prefix}.On.From", errors, true);
                ValidateAliasPropertyPath(on.To, aliases, $"{prefix}.On.To", errors, true);
            }
        }

        var fromAlias = request.From.Alias.Trim();
        foreach (var path in request.Select)
            ValidateSelectPath(path, aliases, errors);

        if (request.WhereClause != null)
            ValidateOuterPathsFromAliasOnly(request.WhereClause, fromAlias, "WhereClause", errors);

        for (var i = 0; i < request.SortBy.Count; i++) {
            var sortPath = request.SortBy[i].PropertyName;
            if (!IsFromAliasPath(sortPath, fromAlias))
                errors.Add(Err($"SortBy[{i}].PropertyName '{sortPath}' must use From alias '{fromAlias}' (join-alias sort is not supported in v1)."));
        }

        return errors;
    }

    private static void ValidateSourceScope(SourceQueryScope? scope, string prefix, List<ApiError> errors)
    {
        if (scope is null)
            return;

        // Nested Start/Amount/Sort/Include are not on SourceQueryScope; Keys + Where only by design.
        _ = prefix;
        _ = errors;
        _ = scope;
    }

    private static void ValidateSelectPath(string path, IReadOnlyDictionary<string, RootQueryEntityEntry> aliases, List<ApiError> errors)
    {
        if (string.IsNullOrWhiteSpace(path)) {
            errors.Add(Err("Select paths must be non-empty."));
            return;
        }

        ValidateAliasPropertyPath(path, aliases, "Select", errors, true);
    }

    private static void ValidateAliasPropertyPath(
        string path,
        IReadOnlyDictionary<string, RootQueryEntityEntry> aliases,
        string context,
        List<ApiError> errors,
        bool requireKnownAlias)
    {
        var parts = path.Split('.', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) {
            errors.Add(Err($"{context} path '{path}' must be alias.property."));
            return;
        }

        if (!aliases.TryGetValue(parts[0], out var entry)) {
            if (requireKnownAlias)
                errors.Add(Err($"{context} path '{path}' uses unknown alias '{parts[0]}'."));

            return;
        }

        if (!entry.TryGetProperty(parts[1], out var _))
            errors.Add(Err($"{context} path '{path}': property '{parts[1]}' not found on '{entry.ClrType.Name}'."));
    }

    private static void ValidateOuterPathsFromAliasOnly(WhereClause clause, string fromAlias, string context, List<ApiError> errors)
    {
        switch (clause) {
            case ConditionClause c:
                if (!IsFromAliasPath(c.Field, fromAlias) && !string.Equals(c.Field.Trim(), fromAlias, StringComparison.OrdinalIgnoreCase)) {
                    // Allow bare property (no alias) as From-root shorthand
                    if (c.Field.Contains('.', StringComparison.Ordinal))
                        errors.Add(Err($"{context} field '{c.Field}' must use From alias '{fromAlias}' (join-alias filters belong in nested Join.Query)."));
                }

                if (c.SubClause != null)
                    ValidateOuterPathsFromAliasOnly(c.SubClause, fromAlias, context + ".SubClause", errors);

                break;
            case GroupClause g:
                foreach (var child in g.Children)
                    ValidateOuterPathsFromAliasOnly(child, fromAlias, context, errors);

                if (g.SubClause != null)
                    ValidateOuterPathsFromAliasOnly(g.SubClause, fromAlias, context + ".SubClause", errors);

                break;
        }
    }

    private static bool IsFromAliasPath(string path, string fromAlias)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var trimmed = path.Trim();
        if (!trimmed.Contains('.', StringComparison.Ordinal))
            return true; // bare property = From root

        return trimmed.StartsWith(fromAlias + ".", StringComparison.OrdinalIgnoreCase);
    }

    private static ApiError Err(string description) => new(ApiErrorCodes.InvalidQuery, description);
}