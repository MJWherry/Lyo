using System.Collections;
using System.Globalization;
using System.Reflection;
using Lyo.Exceptions;

namespace Lyo.Formatter.Web.Components;

/// <summary>One SmartFormat-selectable path from the live session context, shown in the template editor dropdown.</summary>
/// <param name="Path">Selector as it appears inside <c>{...}</c>, e.g. <c>User.First</c>.</param>
/// <param name="Preview">Short display of the current value, for scanning the list.</param>
/// <param name="HasChildren">True when the value has nested keys or properties the user can continue into.</param>
public sealed record LyoFormatterContextEntry(string Path, string Preview, bool HasChildren);

/// <summary>The <c>{...}</c> token that contains the caret, used to insert or replace a context path.</summary>
/// <param name="BraceIndex">Index of the opening <c>{</c>.</param>
/// <param name="EndIndex">Exclusive end of the token. Includes the closing <c>}</c> when the placeholder is already closed.</param>
/// <param name="Prefix">Selector text after <c>{</c> up to the caret (format specifiers omitted).</param>
/// <param name="Key">Full selector inside the token, ignoring <c>:format</c>.</param>
/// <param name="Closed">True when a matching <c>}</c> exists after the caret.</param>
public readonly record struct LyoFormatterPlaceholderSpan(int BraceIndex, int EndIndex, string Prefix, string Key, bool Closed);

/// <summary>
/// Walks a SmartFormat context (dictionary or POCO) into dotted paths for the template editor autocomplete.
/// Strings and primitives are leaves; nested dictionaries and public readable properties become child paths.
/// </summary>
public static class LyoFormatterContextCatalog
{
    /// <summary>How many nested property/key levels to walk, counting the root key as depth 0.</summary>
    public const int MaxDepth = 3;

    /// <summary>Hard cap so a large graph cannot flood the dropdown.</summary>
    public const int MaxItems = 80;

    /// <summary>Default number of rows shown after filtering.</summary>
    public const int DefaultSuggestLimit = 16;

    private static readonly HashSet<string> DateTimePropertyNames = new(StringComparer.Ordinal) {
        "Year", "Month", "Day", "Hour", "Minute", "Second"
    };

    /// <summary>Builds the full path list for <paramref name="context" />. Empty when context is null or a leaf.</summary>
    public static IReadOnlyList<LyoFormatterContextEntry> Build(object? context)
    {
        var items = new List<LyoFormatterContextEntry>();
        if (context is null || IsLeaf(context))
            return items;

        Walk(context, path: string.Empty, depth: 0, items, new HashSet<object>(ReferenceEqualityComparer.Instance));
        return items;
    }

    /// <summary>
    /// Filters <paramref name="catalog" /> to paths that match <paramref name="prefix" /> (ordinal ignore-case): the full path or any dotted segment starts with the prefix.
    /// Empty prefix keeps top-level keys plus one nested level unless <paramref name="listAllWhenEmpty" /> is true (click-to-replace on a closed <c>{key}</c> with nothing typed yet).
    /// </summary>
    public static IReadOnlyList<LyoFormatterContextEntry> Filter(IReadOnlyList<LyoFormatterContextEntry> catalog, string prefix, int limit = DefaultSuggestLimit, bool listAllWhenEmpty = false)
    {
        ArgumentHelpers.ThrowIfNull(catalog);
        prefix ??= string.Empty;
        if (limit <= 0)
            return [];

        IEnumerable<LyoFormatterContextEntry> query = catalog;
        if (prefix.Length == 0) {
            if (!listAllWhenEmpty)
                query = query.Where(e => DotCount(e.Path) <= 1);
        }
        else {
            query = query.Where(e => PathMatchesPrefix(e.Path, prefix));
        }

        return query
            .OrderBy(e => DotCount(e.Path))
            .ThenBy(e => e.Path, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// Dropdown rows for the placeholder at the caret. Always filters by <see cref="LyoFormatterPlaceholderSpan.Prefix"/> — including inside an already-closed <c>{key}</c>.
    /// An empty prefix on a closed token lists all paths so the user can pick a replacement without typing.
    /// </summary>
    public static IReadOnlyList<LyoFormatterContextEntry> Suggest(IReadOnlyList<LyoFormatterContextEntry> catalog, in LyoFormatterPlaceholderSpan span, int limit = DefaultSuggestLimit)
        => Filter(catalog, span.Prefix, limit, listAllWhenEmpty: span.Closed && span.Prefix.Length == 0);

    /// <summary>
    /// Caret index after inserting <c>{path}</c> at <paramref name="braceIndex"/>.
    /// Nested values (list, dictionary, object) leave the caret before <c>}</c> so the user can type <c>.</c> and continue into a child.
    /// Leaves place the caret after <c>}</c>.
    /// </summary>
    public static int CaretAfterInsert(int braceIndex, string path, bool hasChildren)
    {
        ArgumentHelpers.ThrowIfNull(path);
        var afterClose = braceIndex + path.Length + 2;
        return hasChildren ? afterClose - 1 : afterClose;
    }

    /// <summary>
    /// True when <paramref name="caret" /> sits inside a <c>{...}</c> token (open or already closed).
    /// <paramref name="prefix" /> is the selector text after <c>{</c> up to the caret (format specifiers are stripped).
    /// </summary>
    public static bool TryGetOpenPlaceholder(string template, int caret, out int braceIndex, out string prefix)
    {
        if (TryGetPlaceholderAtCaret(template, caret, out var span)) {
            braceIndex = span.BraceIndex;
            prefix = span.Prefix;
            return true;
        }

        braceIndex = -1;
        prefix = string.Empty;
        return false;
    }

    /// <summary>
    /// Locates the <c>{...}</c> token that contains <paramref name="caret" />, including a closed token such as <c>{Name}</c>.
    /// <see cref="LyoFormatterPlaceholderSpan.EndIndex" /> is exclusive and includes the closing <c>}</c> when present, so insert/replace can swap the whole token.
    /// </summary>
    public static bool TryGetPlaceholderAtCaret(string template, int caret, out LyoFormatterPlaceholderSpan span)
    {
        template ??= string.Empty;
        span = default;
        if (caret < 0 || caret > template.Length)
            return false;

        var depth = 0;
        var lastOpen = -1;
        for (var i = 0; i < caret; i++) {
            var c = template[i];
            if (c == '{') {
                if (i + 1 < template.Length && template[i + 1] == '{') {
                    i++;
                    continue;
                }

                depth++;
                lastOpen = i;
                continue;
            }

            if (c != '}')
                continue;

            if (i + 1 < caret && template[i + 1] == '}') {
                i++;
                continue;
            }

            if (depth > 0)
                depth--;
        }

        if (depth <= 0 || lastOpen < 0 || caret <= lastOpen)
            return false;

        var closed = false;
        var endIndex = caret;
        for (var i = lastOpen + 1; i < template.Length; i++) {
            var c = template[i];
            if (c == '{') {
                if (i + 1 < template.Length && template[i + 1] == '{') {
                    i++;
                    continue;
                }

                break;
            }

            if (c != '}')
                continue;

            if (i + 1 < template.Length && template[i + 1] == '}') {
                i++;
                continue;
            }

            closed = true;
            endIndex = i + 1;
            break;
        }

        if (!closed)
            endIndex = caret;

        var keyLimit = closed ? endIndex - 1 : caret;
        var inner = template[(lastOpen + 1)..keyLimit];
        var spec = inner.IndexOfAny([':', ',', '(', ')']);
        var key = spec >= 0 ? inner[..spec] : inner;

        var prefixLimit = Math.Min(caret, keyLimit) - (lastOpen + 1);
        if (prefixLimit < 0)
            prefixLimit = 0;
        if (prefixLimit > inner.Length)
            prefixLimit = inner.Length;

        var prefix = inner[..prefixLimit];
        var prefixSpec = prefix.IndexOfAny([':', ',', '(', ')']);
        if (prefixSpec >= 0)
            prefix = prefix[..prefixSpec];

        span = new(lastOpen, endIndex, prefix, key, closed);
        return true;
    }

    private static void Walk(object? value, string path, int depth, List<LyoFormatterContextEntry> items, HashSet<object> seen)
    {
        if (items.Count >= MaxItems)
            return;

        if (path.Length > 0) {
            items.Add(new(path, Preview(value), HasChildren(value, depth)));
            if (items.Count >= MaxItems)
                return;
        }

        if (depth >= MaxDepth || value is null || IsLeaf(value) || !seen.Add(value))
            return;

        if (TryWalkDictionary(value, path, depth, items, seen))
            return;

        if (value is IEnumerable and not IDictionary) {
            if (value is ICollection collection) {
                var countPath = Combine(path, "Count");
                if (countPath.Length > 0 && items.Count < MaxItems)
                    items.Add(new(countPath, collection.Count.ToString(CultureInfo.InvariantCulture), HasChildren: false));
            }

            return;
        }

        foreach (var prop in GetReadableProperties(value.GetType(), value)) {
            if (items.Count >= MaxItems)
                return;

            object? child;
            try {
                child = prop.GetValue(value);
            }
            catch {
                continue;
            }

            Walk(child, Combine(path, prop.Name), depth + 1, items, seen);
        }
    }

    private static bool TryWalkDictionary(object value, string path, int depth, List<LyoFormatterContextEntry> items, HashSet<object> seen)
    {
        if (value is IReadOnlyDictionary<string, object?> typed) {
            foreach (var entry in typed) {
                if (items.Count >= MaxItems)
                    return true;
                if (string.IsNullOrEmpty(entry.Key))
                    continue;

                Walk(entry.Value, Combine(path, entry.Key), depth + 1, items, seen);
            }

            return true;
        }

        if (value is IDictionary dictionary) {
            foreach (DictionaryEntry entry in dictionary) {
                if (items.Count >= MaxItems)
                    return true;

                var key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture);
                if (string.IsNullOrEmpty(key))
                    continue;

                Walk(entry.Value, Combine(path, key), depth + 1, items, seen);
            }

            return true;
        }

        return false;
    }

    private static bool HasChildren(object? value, int depth)
    {
        if (depth >= MaxDepth || value is null || IsLeaf(value))
            return false;
        if (value is IDictionary { Count: > 0 })
            return true;
        if (value is IEnumerable and not IDictionary)
            return value is ICollection;
        return GetReadableProperties(value.GetType(), value).Count > 0;
    }

    private static bool IsLeaf(object? value)
    {
        if (value is null or string or Enum or decimal or Guid or TimeSpan or Uri or Version or byte[])
            return true;

        var type = value.GetType();
        return type.IsPrimitive || type == typeof(DateOnly) || type == typeof(TimeOnly);
    }

    private static List<PropertyInfo> GetReadableProperties(Type type, object instance)
    {
        var flags = BindingFlags.Public | BindingFlags.Instance;
        IEnumerable<PropertyInfo> props = type.GetProperties(flags).Where(p => p.CanRead && p.GetIndexParameters().Length == 0);
        if (instance is DateTime or DateTimeOffset)
            props = props.Where(p => DateTimePropertyNames.Contains(p.Name));

        return props.ToList();
    }

    private static string Combine(string path, string segment) => path.Length == 0 ? segment : path + "." + segment;

    private static bool PathMatchesPrefix(string path, string prefix)
    {
        if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return true;

        var start = 0;
        for (var i = 0; i < path.Length; i++) {
            if (path[i] != '.')
                continue;
            if (start < i && path.AsSpan(start, i - start).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
            start = i + 1;
        }

        return start < path.Length && path.AsSpan(start).StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static int DotCount(string path)
    {
        var n = 0;
        foreach (var c in path) {
            if (c == '.')
                n++;
        }

        return n;
    }

    private static string Preview(object? value)
    {
        if (value is null)
            return "null";
        if (value is string text)
            return Truncate(text);
        if (value is IDictionary dictionary)
            return dictionary.Count == 0 ? "{}" : dictionary.Count.ToString(CultureInfo.InvariantCulture) + " keys";
        if (value is DateTime date)
            return date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        if (value is DateTimeOffset offset)
            return offset.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        if (value is IFormattable formattable)
            return Truncate(formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty);

        var rendered = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        var typeName = value.GetType().FullName;
        if (typeName is not null && rendered.StartsWith(typeName, StringComparison.Ordinal))
            return value.GetType().Name;

        return Truncate(rendered);
    }

    private static string Truncate(string text)
    {
        text = text.Replace('\r', ' ').Replace('\n', ' ');
        return text.Length <= 48 ? text : text[..45] + "...";
    }
}
