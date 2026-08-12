using System.Collections;
using System.Text.RegularExpressions;
using Lyo.Api.Models.Error;
using Lyo.Api.Services.Crud.Read.Project;
using Lyo.Formatter;
using Lyo.Query.Models.Common.Request;
using ApiErrorCodes = Lyo.Api.Models.Constants.ApiErrorCodes;

namespace Lyo.Api.Services.Crud.Read.Query.Root;

/// <summary>
/// Applies SmartFormat <see cref="ComputedField" /> templates to root <c>/Query</c> rows after fan-out collapse. From-only templates become a root scalar; any template that
/// references a join alias is written only onto each bag of the deepest join alias among those placeholders (From scalars are repeated per bag). Accepts Mustache-style
/// <c>{{token}}</c> as well as SmartFormat <c>{token}</c>.
/// </summary>
internal static partial class RootQueryComputedFields
{
    [GeneratedRegex(@"\{\{([^{}]+)\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex MustachePlaceholderRegex();

    public static List<ApiError> ValidateGuardrails(QueryReq request, QueryOptions options)
    {
        var errors = new List<ApiError>();
        if (request.Select.Count > options.MaxSelectFieldCount)
            errors.Add(new(ApiErrorCodes.InvalidQuery, $"Select field count ({request.Select.Count}) exceeds maximum allowed ({options.MaxSelectFieldCount})."));

        if (request.ComputedFields.Count > options.MaxComputedFieldCount)
            errors.Add(new(ApiErrorCodes.InvalidQuery, $"Computed field count ({request.ComputedFields.Count}) exceeds maximum allowed ({options.MaxComputedFieldCount})."));

        foreach (var computedField in request.ComputedFields) {
            if (computedField.Template?.Length > options.MaxComputedTemplateLength) {
                errors.Add(
                    new(
                        ApiErrorCodes.InvalidQuery,
                        $"Computed field '{computedField.Name}' template length ({computedField.Template.Length}) exceeds maximum allowed ({options.MaxComputedTemplateLength})."));
            }
        }

        return errors;
    }

    /// <summary>Normalizes Mustache braces and appends missing <c>alias.property</c> Select paths referenced by templates.</summary>
    public static IReadOnlyList<string> EnsureSelectIncludesComputedDependencies(QueryReq request, IProjectionService projectionService, IFormatterService? formatter)
    {
        if (request.ComputedFields.Count == 0)
            return [];

        NormalizeTemplatesInPlace(request.ComputedFields);
        var deps = projectionService.GetComputedFieldDependencies(request.ComputedFields);
        if (deps.Count == 0 && formatter is not null) {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var cf in request.ComputedFields) {
                if (string.IsNullOrWhiteSpace(cf.Template))
                    continue;

                foreach (var ph in formatter.GetPlaceholders(cf.Template))
                    set.Add(ph);
            }

            deps = [.. set];
        }

        var added = new List<string>();
        foreach (var dep in deps) {
            var path = dep.Trim();
            if (path.Length == 0 || !path.Contains('.', StringComparison.Ordinal))
                continue;

            if (SelectContains(request.Select, path))
                continue;

            request.Select.Add(path);
            added.Add(path);
        }

        return added;
    }

    public static IReadOnlyList<object?> Apply(
        IReadOnlyList<object?> items,
        IReadOnlyList<ComputedField> computedFields,
        RootQueryShapePlan plan,
        IProjectionService projectionService,
        IFormatterService? formatter)
    {
        if (computedFields.Count == 0 || items.Count == 0)
            return items;

        NormalizeTemplatesInPlace(computedFields);
        if (formatter is null) {
            var specs = ToProjectedSpecs(plan);
            var flattened = items.Select(i => i is IDictionary d ? FlattenRow(ToMutableDictionary(d), plan) : (object?)i).ToList();
            return projectionService.ApplyComputedFields(flattened, computedFields, specs);
        }

        var results = new List<object?>(items.Count);
        foreach (var item in items) {
            Dictionary<string, object?> root;
            if (item is IDictionary nested)
                root = ToMutableDictionary(nested);
            else if (plan.SelectSpecs.Count == 1 && plan.SelectSpecs[0].IsFromSide) {
                // Single From-side Select collapses to a scalar — promote so computed keys can attach.
                root = new(StringComparer.OrdinalIgnoreCase) { [plan.SelectSpecs[0].PropertyName] = item };
            }
            else {
                results.Add(item);
                continue;
            }

            var flat = FlattenRow(root, plan);
            foreach (var field in computedFields) {
                if (string.IsNullOrWhiteSpace(field.Name) || string.IsNullOrWhiteSpace(field.Template))
                    continue;

                ApplyOne(root, flat, field, plan, formatter);
            }

            results.Add(root);
        }

        return results;
    }

    private static void ApplyOne(Dictionary<string, object?> root, Dictionary<string, object?> flat, ComputedField field, RootQueryShapePlan plan, IFormatterService formatter)
    {
        var placeholders = formatter.GetPlaceholders(field.Template);
        if (placeholders.Count == 0) {
            root[field.Name] = field.Template;
            return;
        }

        if (!TryGetDeepestJoinAlias(placeholders, plan, out var joinAlias, out var resultName)) {
            // From-only (or constant placeholders that resolve as scalars): root scalar.
            root[field.Name] = FormatTemplate(formatter, field.Template, flat);
            return;
        }

        var values = new object?[placeholders.Count];
        for (var i = 0; i < placeholders.Count; i++)
            values[i] = ResolveFlat(flat, placeholders[i]);

        // Fan-out length follows the deepest join column when present; otherwise any join collection.
        var maxLen = 0;
        for (var i = 0; i < placeholders.Count; i++) {
            if (!IsJoinPlaceholder(placeholders[i], plan, out var phAlias))
                continue;

            if (!string.Equals(phAlias, joinAlias, StringComparison.OrdinalIgnoreCase))
                continue;

            if (IsCollection(values[i]))
                maxLen = Math.Max(maxLen, GetLength(values[i]!));
        }

        if (maxLen == 0) {
            foreach (var v in values) {
                if (IsCollection(v))
                    maxLen = Math.Max(maxLen, GetLength(v!));
            }
        }

        if (maxLen == 0)
            return;

        var formatted = new List<object?>(maxLen);
        for (var i = 0; i < maxLen; i++) {
            var mini = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var p = 0; p < placeholders.Count; p++) {
                var v = values[p];
                mini[placeholders[p]] = IsCollection(v) ? ElementAt(v!, i) : v;
            }

            formatted.Add(FormatTemplate(formatter, field.Template, mini));
        }

        // Join-scoped: only on deepest join bags — never root array / never "{alias}.{name}".
        ZipIntoJoinBags(root, resultName, joinAlias, plan, field.Name, formatted);
    }

    private static string FormatTemplate(IFormatterService formatter, string template, Dictionary<string, object?> row)
    {
        var placeholders = formatter.GetPlaceholders(template);
        if (placeholders.Count == 0)
            return template;

        if (placeholders.All(p => !p.Contains('.')))
            return formatter.Format(template, row);

        var result = template;
        foreach (var ph in placeholders.OrderByDescending(p => p.Length)) {
            result = Regex.Replace(
                result, @"\{\s*" + Regex.Escape(ph) + @"(\:[^}]+)?\s*\}", m => {
                    if (!TryResolveKey(row, ph, out var val) || val is null)
                        return string.Empty;

                    var fmtSuffix = m.Groups[1].Success ? m.Groups[1].Value : "";
                    const string syntheticKey = "__p";
                    var miniTemplate = "{" + syntheticKey + fmtSuffix + "}";
                    var ctx = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { [syntheticKey] = val };
                    return formatter.Format(miniTemplate, ctx);
                }, RegexOptions.IgnoreCase);
        }

        return result;
    }

    private static void ZipIntoJoinBags(
        Dictionary<string, object?> root,
        string rootJoinResultName,
        string targetAlias,
        RootQueryShapePlan plan,
        string computedName,
        List<object?> formatted)
    {
        // Root-level join bags (target is a root join)
        if (string.Equals(
                rootJoinResultName, plan.Joins.FirstOrDefault(j => string.Equals(j.Alias, targetAlias, StringComparison.OrdinalIgnoreCase))?.ResultName,
                StringComparison.OrdinalIgnoreCase) && TryGetIgnoreCase(root, rootJoinResultName, out var bagsObj) && bagsObj is not null) {
            MutateBags(bagsObj, computedName, formatted, bags => root[rootJoinResultName] = bags);
            return;
        }

        // Nested: find root join that owns the chain, zip into nested bags matching targetAlias
        foreach (var candidate in plan.Joins.Where(j => string.Equals(j.On[0].LeftAlias, plan.FromAlias, StringComparison.OrdinalIgnoreCase))) {
            if (!TryGetIgnoreCase(root, candidate.ResultName, out var rootBags) || rootBags is null)
                continue;

            if (string.Equals(candidate.Alias, targetAlias, StringComparison.OrdinalIgnoreCase)) {
                MutateBags(rootBags, computedName, formatted, bags => root[candidate.ResultName] = bags);
                return;
            }

            if (ZipIntoNestedBags(rootBags, candidate.Alias, targetAlias, plan, computedName, formatted, out var updated)) {
                root[candidate.ResultName] = updated;
                return;
            }
        }
    }

    private static bool ZipIntoNestedBags(
        object bagsObj,
        string currentAlias,
        string targetAlias,
        RootQueryShapePlan plan,
        string computedName,
        List<object?> formatted,
        out List<object?> updatedBags)
    {
        var bags = ToBagList(bagsObj);
        updatedBags = bags;
        var childJoins = plan.Joins.Where(j => string.Equals(j.On[0].LeftAlias, currentAlias, StringComparison.OrdinalIgnoreCase)).ToList();
        if (childJoins.Count == 0)
            return false;

        var index = 0;
        var changed = false;
        for (var bi = 0; bi < bags.Count; bi++) {
            if (bags[bi] is not IDictionary bagDict)
                continue;

            var bag = ToMutableDictionary(bagDict);
            foreach (var child in childJoins) {
                if (!TryGetIgnoreCase(bag, child.ResultName, out var childObj) || childObj is null)
                    continue;

                if (string.Equals(child.Alias, targetAlias, StringComparison.OrdinalIgnoreCase)) {
                    var childBags = ToBagList(childObj);
                    for (var ci = 0; ci < childBags.Count && index < formatted.Count; ci++, index++) {
                        if (childBags[ci] is not IDictionary cd)
                            continue;

                        var mutable = ToMutableDictionary(cd);
                        mutable[computedName] = formatted[index];
                        childBags[ci] = mutable;
                        changed = true;
                    }

                    bag[child.ResultName] = childBags.Count == 1 ? childBags[0] : childBags;
                }
                else if (ZipIntoNestedBags(childObj, child.Alias, targetAlias, plan, computedName, formatted.Skip(index).ToList(), out var nestedUpdated)) {
                    // Re-zip with global index tracking is awkward; flatten path for nested depth>1 is rare for v1.
                    bag[child.ResultName] = nestedUpdated.Count == 1 ? nestedUpdated[0] : nestedUpdated;
                    index += nestedUpdated.Count;
                    changed = true;
                }
            }

            bags[bi] = bag;
        }

        updatedBags = bags;
        return changed;
    }

    private static void MutateBags(object bagsObj, string computedName, List<object?> formatted, Action<List<object?>> replaceRoot)
    {
        var bags = ToBagList(bagsObj);
        for (var i = 0; i < bags.Count && i < formatted.Count; i++) {
            if (bags[i] is not IDictionary bag)
                continue;

            var mutable = ToMutableDictionary(bag);
            mutable[computedName] = formatted[i];
            bags[i] = mutable;
        }

        replaceRoot(bags);
    }

    /// <summary>
    /// Among join aliases referenced by placeholders, picks the furthest from <see cref="RootQueryShapePlan.FromAlias" />. Returns false when no join alias is referenced
    /// (From-only / constant).
    /// </summary>
    private static bool TryGetDeepestJoinAlias(IReadOnlyList<string> placeholders, RootQueryShapePlan plan, out string joinAlias, out string resultName)
    {
        joinAlias = "";
        resultName = "";
        string? deepest = null;
        var deepestDepth = -1;
        foreach (var ph in placeholders) {
            if (!IsJoinPlaceholder(ph, plan, out var alias))
                continue;

            var depth = JoinDepth(alias, plan);
            if (depth < 0)
                continue;

            if (depth > deepestDepth) {
                deepestDepth = depth;
                deepest = alias;
            }
        }

        if (deepest is null)
            return false;

        var planJoin = plan.Joins.First(j => string.Equals(j.Alias, deepest, StringComparison.OrdinalIgnoreCase));
        joinAlias = planJoin.Alias;
        resultName = planJoin.ResultName;
        return true;
    }

    private static bool IsJoinPlaceholder(string placeholder, RootQueryShapePlan plan, out string joinAlias)
    {
        joinAlias = "";
        var parts = placeholder.Split('.', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return false;

        if (string.Equals(parts[0], plan.FromAlias, StringComparison.OrdinalIgnoreCase))
            return false;

        var join = plan.Joins.FirstOrDefault(j => string.Equals(j.Alias, parts[0], StringComparison.OrdinalIgnoreCase));
        if (join is null)
            return false;

        joinAlias = join.Alias;
        return true;
    }

    private static int JoinDepth(string alias, RootQueryShapePlan plan)
    {
        var depth = 0;
        var current = alias;
        var guard = 0;
        while (guard++ < 16) {
            var join = plan.Joins.FirstOrDefault(j => string.Equals(j.Alias, current, StringComparison.OrdinalIgnoreCase));
            if (join is null)
                return -1;

            depth++;
            var parent = join.On[0].LeftAlias;
            if (string.Equals(parent, plan.FromAlias, StringComparison.OrdinalIgnoreCase))
                return depth;

            current = parent;
        }

        return -1;
    }

    private static Dictionary<string, object?> FlattenRow(Dictionary<string, object?> root, RootQueryShapePlan plan)
    {
        var flat = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in root)
            flat[kv.Key] = kv.Value;

        foreach (var spec in plan.SelectSpecs) {
            if (spec.IsFromSide) {
                if (TryGetIgnoreCase(root, spec.PropertyName, out var fromVal)) {
                    flat[spec.PropertyName] = fromVal;
                    flat[spec.RequestedPath] = fromVal;
                    flat[$"{plan.FromAlias}.{spec.PropertyName}"] = fromVal;
                }

                continue;
            }

            var column = ExtractColumnAlongJoinTree(root, plan, spec.Alias, spec.PropertyName);
            flat[spec.RequestedPath] = column;
            flat[$"{spec.Alias}.{spec.PropertyName}"] = column;
        }

        return flat;
    }

    /// <summary>
    /// Walks root → root-join bags → nested child bags to collect <paramref name="propertyName" /> for <paramref name="targetAlias" />. Length matches the fan-out of the target
    /// join (one value per bag).
    /// </summary>
    private static List<object?> ExtractColumnAlongJoinTree(Dictionary<string, object?> root, RootQueryShapePlan plan, string targetAlias, string propertyName)
    {
        var column = new List<object?>();
        var rootJoins = plan.Joins.Where(j => string.Equals(j.On[0].LeftAlias, plan.FromAlias, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var rj in rootJoins) {
            if (!TryGetIgnoreCase(root, rj.ResultName, out var bagsObj) || bagsObj is null)
                continue;

            CollectProperty(ToBagList(bagsObj), rj.Alias, targetAlias, propertyName, plan, column);
        }

        return column;
    }

    private static void CollectProperty(List<object?> bags, string currentAlias, string targetAlias, string propertyName, RootQueryShapePlan plan, List<object?> column)
    {
        if (string.Equals(currentAlias, targetAlias, StringComparison.OrdinalIgnoreCase)) {
            foreach (var bag in bags) {
                if (bag is IDictionary d && TryGetIgnoreCase(d, propertyName, out var v))
                    column.Add(v);
                else
                    column.Add(null);
            }

            return;
        }

        var childJoins = plan.Joins.Where(j => string.Equals(j.On[0].LeftAlias, currentAlias, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var bag in bags) {
            if (bag is not IDictionary d) {
                // Still need to fan-out nulls if target is deeper — skip for empty bag
                continue;
            }

            foreach (var child in childJoins) {
                if (!TryGetIgnoreCase(d, child.ResultName, out var childObj) || childObj is null) {
                    if (string.Equals(child.Alias, targetAlias, StringComparison.OrdinalIgnoreCase) || IsAncestorOf(child.Alias, targetAlias, plan))
                        column.Add(null);

                    continue;
                }

                CollectProperty(ToBagList(childObj), child.Alias, targetAlias, propertyName, plan, column);
            }
        }
    }

    private static bool IsAncestorOf(string ancestorAlias, string targetAlias, RootQueryShapePlan plan)
    {
        var current = targetAlias;
        var guard = 0;
        while (guard++ < 16) {
            var join = plan.Joins.FirstOrDefault(j => string.Equals(j.Alias, current, StringComparison.OrdinalIgnoreCase));
            if (join is null)
                return false;

            var parent = join.On[0].LeftAlias;
            if (string.Equals(parent, ancestorAlias, StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(parent, plan.FromAlias, StringComparison.OrdinalIgnoreCase))
                return false;

            current = parent;
        }

        return false;
    }

    private static List<object?> ToBagList(object bagsObj)
        => bagsObj switch {
            IList list => list.Cast<object?>().ToList(),
            IDictionary d => [d],
            var _ => []
        };

    private static object? ResolveFlat(Dictionary<string, object?> flat, string token) => TryResolveKey(flat, token, out var val) ? val : null;

    private static bool TryResolveKey(Dictionary<string, object?> row, string token, out object? val)
    {
        if (row.TryGetValue(token, out val))
            return true;

        var tokenNorm = CompactAlphanumericLower(token);
        if (tokenNorm.Length == 0) {
            val = null;
            return false;
        }

        foreach (var kv in row) {
            if (CompactAlphanumericLower(kv.Key) == tokenNorm) {
                val = kv.Value;
                return true;
            }
        }

        val = null;
        return false;
    }

    private static string CompactAlphanumericLower(string key)
    {
        if (string.IsNullOrEmpty(key))
            return "";

        Span<char> buffer = stackalloc char[key.Length];
        var j = 0;
        foreach (var c in key) {
            if (char.IsLetterOrDigit(c))
                buffer[j++] = char.ToLowerInvariant(c);
        }

        return j == 0 ? "" : new(buffer[..j]);
    }

    private static bool IsCollection(object? value) => value is not null and not string and not byte[] and IEnumerable;

    private static int GetLength(object collection)
    {
        if (collection is ICollection c)
            return c.Count;

        var n = 0;
        foreach (var _ in (IEnumerable)collection)
            n++;

        return n;
    }

    private static object? ElementAt(object collection, int index)
    {
        if (collection is IList list)
            return index < list.Count ? list[index] : null;

        var i = 0;
        foreach (var item in (IEnumerable)collection) {
            if (i++ == index)
                return item;
        }

        return null;
    }

    private static Dictionary<string, object?> ToMutableDictionary(IDictionary source)
    {
        if (source is Dictionary<string, object?> typed)
            return typed;

        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (DictionaryEntry e in source) {
            if (e.Key is string key)
                dict[key] = e.Value;
        }

        return dict;
    }

    private static bool TryGetIgnoreCase(IDictionary dict, string key, out object? value)
    {
        foreach (DictionaryEntry e in dict) {
            if (e.Key is string s && string.Equals(s, key, StringComparison.OrdinalIgnoreCase)) {
                value = e.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static bool TryGetIgnoreCase(Dictionary<string, object?> dict, string key, out object? value) => dict.TryGetValue(key, out value);

    private static bool SelectContains(IEnumerable<string> select, string path) => select.Any(s => string.Equals(s.Trim(), path, StringComparison.OrdinalIgnoreCase));

    private static void NormalizeTemplatesInPlace(IReadOnlyList<ComputedField> computedFields)
    {
        foreach (var cf in computedFields) {
            if (string.IsNullOrWhiteSpace(cf.Template))
                continue;

            cf.Template = NormalizeMustache(cf.Template);
        }
    }

    internal static string NormalizeMustache(string template) => MustachePlaceholderRegex().Replace(template, "{$1}");

    private static IReadOnlyList<ProjectedFieldSpec> ToProjectedSpecs(RootQueryShapePlan plan)
        => plan.SelectSpecs.Select(s => new ProjectedFieldSpec(s.RequestedPath, s.RequestedPath, s.RequestedPath.Split('.'))).ToArray();
}