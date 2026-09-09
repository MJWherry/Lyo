using System.Collections;
using System.Reflection;
using Lyo.Api.Models.Error;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using ApiErrorCodes = Lyo.Api.Models.Constants.ApiErrorCodes;

namespace Lyo.Api.Services.Crud.Read.Query.Root;

/// <summary>Checks root <see cref="QueryReq" /> against an entity registry (allowlist, aliases, From-only outer where/sort, nested scope rules).</summary>
public static class RootQueryValidator
{
    /// <summary>Join ceiling. The executor nests one carrier type per join, so this is a shape limit rather than a policy setting.</summary>
    public const int MaxJoins = 7;

    /// <summary>Select ceiling. Rows are materialized as <c>ValueTuple</c> of the primary key plus one slot per selected field.</summary>
    public const int MaxSelectFields = 6;

    private const BindingFlags PropertyFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy;

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
        else if (request.Select.Count > MaxSelectFields)
            errors.Add(Err($"Select supports at most {MaxSelectFields} fields; {request.Select.Count} were requested."));

        if (request.Joins.Count > MaxJoins)
            errors.Add(Err($"Root /Query supports at most {MaxJoins} joins; {request.Joins.Count} were requested."));

        if (!string.IsNullOrWhiteSpace(request.From.EntityType) && !registry.TryGet(request.From.EntityType, out var _))
            errors.Add(Err($"Unknown or disallowed From.EntityType '{request.From.EntityType}'."));

        var aliases = new Dictionary<string, RootQueryEntityEntry>(StringComparer.OrdinalIgnoreCase);
        RootQueryEntityEntry? fromEntry = null;
        if (!string.IsNullOrWhiteSpace(request.From.EntityType) && registry.TryGet(request.From.EntityType, out var resolvedFrom))
            fromEntry = resolvedFrom;

        if (!string.IsNullOrWhiteSpace(request.From.Alias) && fromEntry is not null)
            aliases[request.From.Alias.Trim()] = fromEntry;

        ValidateSourceScope(request.From.Query, fromEntry, "From.Query", errors);
        for (var i = 0; i < request.Joins.Count; i++) {
            var join = request.Joins[i];
            var prefix = $"Joins[{i}]";
            if (join.Type is not (JoinType.Inner or JoinType.Left or JoinType.Right or JoinType.FullOuter))
                errors.Add(Err($"{prefix}.Type must be Inner, Left, Right, or FullOuter."));

            if (string.IsNullOrWhiteSpace(join.Alias))
                errors.Add(Err($"{prefix}.Alias is required."));
            else if (aliases.ContainsKey(join.Alias.Trim()))
                errors.Add(Err($"{prefix}.Alias '{join.Alias}' is duplicated."));

            RootQueryEntityEntry? joinEntry = null;
            if (string.IsNullOrWhiteSpace(join.EntityType))
                errors.Add(Err($"{prefix}.EntityType is required."));
            else if (!registry.TryGet(join.EntityType, out var resolvedJoin))
                errors.Add(Err($"{prefix}.EntityType '{join.EntityType}' is unknown or disallowed."));
            else {
                joinEntry = resolvedJoin;
                if (!string.IsNullOrWhiteSpace(join.Alias))
                    aliases[join.Alias.Trim()] = joinEntry;
            }

            if (join.On.Count == 0)
                errors.Add(Err($"{prefix}.On requires at least one clause."));

            ValidateSourceScope(join.Query, joinEntry, $"{prefix}.Query", errors);
            foreach (var on in join.On) {
                ValidateAliasPropertyPath(on.From, aliases, $"{prefix}.On.From", errors, true);
                ValidateAliasPropertyPath(on.To, aliases, $"{prefix}.On.To", errors, true);
            }
        }

        var fromAlias = request.From.Alias.Trim();
        foreach (var path in request.Select)
            ValidateSelectPath(path, aliases, errors);

        if (request.WhereClause != null)
            ValidateOuterPathsFromAliasOnly(request.WhereClause, fromAlias, fromEntry, "WhereClause", errors);

        for (var i = 0; i < request.SortBy.Count; i++) {
            var sortPath = request.SortBy[i].PropertyName;
            if (!IsFromAliasPath(sortPath, fromAlias)) {
                errors.Add(Err($"SortBy[{i}].PropertyName '{sortPath}' must use From alias '{fromAlias}' (join-alias sort is not supported in v1)."));
                continue;
            }

            // The sort path is fed straight to the queryable, so an unknown property is a query-time failure unless it is caught here first.
            var relative = StripAlias(sortPath, fromAlias);
            if (fromEntry is not null && !PathResolves(fromEntry.ClrType, relative))
                errors.Add(Err($"SortBy[{i}].PropertyName '{sortPath}' does not resolve on '{fromEntry.ClrType.Name}'."));
        }

        return errors;
    }

    /// <summary>
    /// A nested scope carries a where clause evaluated directly against its own entity, so its fields are entity-relative: an alias prefix or an unknown property means the
    /// caller is filtering something other than what they think. <see cref="SourceQueryScope.Keys" /> is rejected outright because execution ignores it, which would otherwise
    /// widen the scope silently.
    /// </summary>
    private static void ValidateSourceScope(SourceQueryScope? scope, RootQueryEntityEntry? entry, string prefix, List<ApiError> errors)
    {
        if (scope is null)
            return;

        if (scope.Keys.Count > 0)
            errors.Add(Err($"{prefix}.Keys is not supported on root /Query; express key filters as a WhereClause."));

        if (scope.WhereClause is not null && entry is not null)
            ValidateScopeClause(scope.WhereClause, entry, prefix, errors);
    }

    private static void ValidateScopeClause(WhereClause clause, RootQueryEntityEntry entry, string prefix, List<ApiError> errors)
    {
        switch (clause) {
            case ConditionClause c:
                if (string.IsNullOrWhiteSpace(c.Field))
                    errors.Add(Err($"{prefix} field must be non-empty."));
                else if (!PathResolves(entry.ClrType, c.Field.Trim()))
                    errors.Add(Err($"{prefix} field '{c.Field}' does not resolve on '{entry.ClrType.Name}'; nested scope fields are relative to that entity, not aliased."));

                if (c.SubClause != null)
                    ValidateScopeClause(c.SubClause, entry, prefix + ".SubClause", errors);

                break;
            case GroupClause g:
                foreach (var child in g.Children)
                    ValidateScopeClause(child, entry, prefix, errors);

                if (g.SubClause != null)
                    ValidateScopeClause(g.SubClause, entry, prefix + ".SubClause", errors);

                break;
        }
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

    private static void ValidateOuterPathsFromAliasOnly(WhereClause clause, string fromAlias, RootQueryEntityEntry? fromEntry, string context, List<ApiError> errors)
    {
        switch (clause) {
            case ConditionClause c:
                if (!IsFromAliasPath(c.Field, fromAlias) && !string.Equals(c.Field.Trim(), fromAlias, StringComparison.OrdinalIgnoreCase)) {
                    // Allow a bare property (no alias) as From-root shorthand.
                    if (c.Field.Contains('.', StringComparison.Ordinal))
                        errors.Add(Err($"{context} field '{c.Field}' must use From alias '{fromAlias}' (join-alias filters belong in nested Join.Query)."));
                }
                else if (fromEntry is not null && !string.IsNullOrWhiteSpace(c.Field) && !PathResolves(fromEntry.ClrType, StripAlias(c.Field.Trim(), fromAlias)))
                    errors.Add(Err($"{context} field '{c.Field}' does not resolve on '{fromEntry.ClrType.Name}'."));

                if (c.SubClause != null)
                    ValidateOuterPathsFromAliasOnly(c.SubClause, fromAlias, fromEntry, context + ".SubClause", errors);

                break;
            case GroupClause g:
                foreach (var child in g.Children)
                    ValidateOuterPathsFromAliasOnly(child, fromAlias, fromEntry, context, errors);

                if (g.SubClause != null)
                    ValidateOuterPathsFromAliasOnly(g.SubClause, fromAlias, fromEntry, context + ".SubClause", errors);

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

    private static string StripAlias(string path, string alias)
    {
        var trimmed = path.Trim();
        var prefix = alias + ".";
        return trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? trimmed[prefix.Length..] : trimmed;
    }

    /// <summary>
    /// Mirrors how the where-clause evaluator resolves a dotted path: each segment is a property on the current type, on its collection element type, or one navigation hop
    /// below it. Deliberately permissive — this exists to turn a query-time failure into a 400, not to narrow what the evaluator accepts.
    /// </summary>
    private static bool PathResolves(Type root, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var current = root;
        foreach (var segment in path.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)) {
            var resolved = FindProperty(current, segment) ?? FindImplicitHop(current, segment);
            if (resolved is null)
                return false;

            current = resolved.PropertyType;
        }

        return true;
    }

    private static PropertyInfo? FindProperty(Type type, string name)
    {
        var direct = SafeGetProperty(type, name);
        if (direct != null)
            return direct;

        var element = CollectionElementType(type);
        return element is null ? null : SafeGetProperty(element, name);
    }

    private static PropertyInfo? FindImplicitHop(Type type, string name)
    {
        var searchType = CollectionElementType(type) ?? type;
        foreach (var candidate in searchType.GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(p => p.Name, StringComparer.Ordinal)) {
            var nested = FindProperty(candidate.PropertyType, name);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private static PropertyInfo? SafeGetProperty(Type type, string name)
    {
        try {
            return type.GetProperty(name, PropertyFlags);
        }
        catch (AmbiguousMatchException) {
            return type.GetProperties(PropertyFlags).FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static Type? CollectionElementType(Type type)
    {
        if (type == typeof(string) || type == typeof(byte[]) || !typeof(IEnumerable).IsAssignableFrom(type))
            return null;

        if (type.IsArray)
            return type.GetElementType();

        foreach (var iface in new[] { type }.Concat(type.GetInterfaces())) {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return iface.GetGenericArguments()[0];
        }

        return null;
    }

    private static ApiError Err(string description) => new(ApiErrorCodes.InvalidQuery, description);
}
