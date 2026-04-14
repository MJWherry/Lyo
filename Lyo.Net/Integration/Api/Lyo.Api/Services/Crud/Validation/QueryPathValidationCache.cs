using Lyo.Api.Services.Crud;
using Lyo.Query.Services.WhereClause;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.Crud.Validation;

/// <summary>
/// Per-request cache so the same dotted path is not re-validated for sort, where, select, and include.
/// Complements process-wide <see cref="SharedEntityMetadataCache" /> and filter metadata caches by deduplicating work within one query.
/// </summary>
public sealed class QueryPathValidationCache
{
    private readonly Dictionary<string, (bool Ok, string? Message)> _filterPropertyPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (bool Ok, string? Normalized, string? Message)> _selectNormalize = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (bool Ok, string? Message)> _includePaths = new(StringComparer.OrdinalIgnoreCase);

    private static string EntityKey<T>() => typeof(T).FullName ?? typeof(T).Name;

    private static string PathKey<T>(string path) => $"{EntityKey<T>()}\u001f{path.Trim()}";

    /// <summary>CLR property path validation (same rules as where/sort).</summary>
    public bool TryValidateFilterPropertyPath<TDbModel>(IWhereClauseService filter, string path, out string? errorMessage)
        where TDbModel : class
    {
        var key = PathKey<TDbModel>(path);
        if (_filterPropertyPaths.TryGetValue(key, out var hit)) {
            errorMessage = hit.Message;
            return hit.Ok;
        }

        var ok = filter.TryValidatePropertyPath<TDbModel>(path, out errorMessage);
        _filterPropertyPaths[key] = (ok, errorMessage);
        if (!ok)
            return ok;

        try {
            var normalized = SharedEntityMetadataCache.NormalizeFieldPath(typeof(TDbModel), path);
            var nk = PathKey<TDbModel>(path);
            _selectNormalize[nk] = (true, normalized, null);
            if (!string.Equals(normalized, path.Trim(), StringComparison.Ordinal))
                _selectNormalize[PathKey<TDbModel>(normalized)] = (true, normalized, null);
        }
        catch {
            // Filter path accepted but projection normalization differs (e.g. edge cases); omit select warm.
        }

        return ok;
    }

    /// <summary>Select path normalization (projection); reuses session entries warmed from successful filter validation when paths match.</summary>
    public bool TryNormalizeSelectPath<TDbModel>(string field, out string? normalized, out string? errorMessage)
        where TDbModel : class
    {
        var key = PathKey<TDbModel>(field);
        if (_selectNormalize.TryGetValue(key, out var hit)) {
            normalized = hit.Normalized;
            errorMessage = hit.Message;
            return hit.Ok;
        }

        if (SharedEntityMetadataCache.TryNormalizeFieldPath(typeof(TDbModel), field, out normalized, out errorMessage)) {
            _selectNormalize[key] = (true, normalized!, null);
            return true;
        }

        _selectNormalize[key] = (false, null, errorMessage);
        return false;
    }

    /// <summary>EF navigation include path (same rules as <see cref="EntityLoaderService.CollectIncludePathErrors" /> per path).</summary>
    public bool TryValidateInclude<TContext, TDbModel>(IEntityLoaderService loader, TContext db, string include, out string? errorMessage)
        where TContext : DbContext
        where TDbModel : class
    {
        var key = PathKey<TDbModel>(include);
        if (_includePaths.TryGetValue(key, out var hit)) {
            errorMessage = hit.Message;
            return hit.Ok;
        }

        var errs = loader.CollectIncludePathErrors<TContext, TDbModel>(db, [include]);
        if (errs.Count == 0) {
            _includePaths[key] = (true, null);
            errorMessage = null;
            return true;
        }

        errorMessage = errs[0].Message;
        _includePaths[key] = (false, errorMessage);
        return false;
    }
}
