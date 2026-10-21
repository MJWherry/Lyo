using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Lyo.Query.Services.WhereClause;

namespace Lyo.Query.Services.Path;

/// <summary>
/// A query-path window. <see cref="Start" /> is inclusive. <see cref="End" /> is exclusive, or null when the window runs through the end. <c>[n]</c> is the one-element
/// window <c>[n:n+1]</c>.
/// </summary>
public readonly struct QueryPathWindow
{
    /// <summary>Builds a window. <paramref name="text" /> is the bracket text kept on the normalized segment, including the brackets.</summary>
    public QueryPathWindow(int start, int? end, string text)
    {
        Start = start;
        End = end;
        Text = text;
    }

    /// <summary>Inclusive start. A missing start in the path is 0.</summary>
    public int Start { get; }

    /// <summary>Exclusive end. Null runs through the end of the value.</summary>
    public int? End { get; }

    /// <summary>Bracket text, including the brackets (<c>[0]</c>, <c>[:3]</c>, <c>[0:]</c>).</summary>
    public string Text { get; }

    /// <summary>True when the window is exactly one element.</summary>
    public bool IsSingle => End is int end && end == Start + 1;
}

/// <summary>Parses <c>[n]</c>, <c>[start:]</c>, <c>[:end]</c>, <c>[start:end]</c>, and <c>[:]</c> on a query path and builds the matching expression.</summary>
public static class QueryPath
{
    private static readonly MethodInfo SubstringStart = typeof(string).GetMethod(nameof(string.Substring), [typeof(int)])!;

    private static readonly MethodInfo SubstringLength = typeof(string).GetMethod(nameof(string.Substring), [typeof(int), typeof(int)])!;

    /// <summary>Splits one dot segment into a property name and an optional window. A segment with no brackets has a null window.</summary>
    public static bool TryParseSegment(string segment, out string name, out QueryPathWindow? window, out string? error)
    {
        name = segment;
        window = null;
        error = null;
        var open = segment.IndexOf('[');
        if (open < 0) {
            if (segment.IndexOf(']') >= 0) {
                error = "is not a valid range.";
                return false;
            }

            return true;
        }

        if (open == 0 || segment[^1] != ']' || segment.IndexOf('[', open + 1) >= 0) {
            error = "is not a valid range.";
            return false;
        }

        name = segment[..open];
        var body = segment[(open + 1)..^1];
        var text = segment[open..];
        if (body.Length == 0) {
            error = "is not a valid range.";
            return false;
        }

        var colon = body.IndexOf(':');
        if (colon < 0) {
            if (!TryNonNegative(body, out var index)) {
                error = "is not a valid range.";
                return false;
            }

            window = new QueryPathWindow(index, index + 1, text);
            return true;
        }

        var startText = body[..colon];
        var endText = body[(colon + 1)..];
        var start = 0;
        if (startText.Length > 0 && !TryNonNegative(startText, out start)) {
            error = "is not a valid range.";
            return false;
        }

        int? end = null;
        if (endText.Length > 0) {
            if (!TryNonNegative(endText, out var endValue) || endValue <= start) {
                error = "is not a valid range.";
                return false;
            }

            end = endValue;
        }

        window = new QueryPathWindow(start, end, text);
        return true;
    }

    /// <summary>Binds <paramref name="path" /> against <paramref name="rootType" />. When <paramref name="requireScalar" /> is true, a collection window longer than one element fails.</summary>
    public static bool TryBindFactory(Type rootType, string path, bool requireScalar, out Func<Expression, Expression>? bind, out Type? resultType, out string? error)
        => TryBindFactory(rootType, path, requireScalar, false, out bind, out resultType, out var _, out error);

    /// <summary>
    /// Binds <paramref name="path" />. When <paramref name="countCollection" /> is true, a collection path binds as the count of its elements and
    /// <paramref name="collectionSize" /> is true for that count and for a <c>.count</c> suffix.
    /// </summary>
    public static bool TryBindFactory(
        Type rootType,
        string path,
        bool requireScalar,
        bool countCollection,
        out Func<Expression, Expression>? bind,
        out Type? resultType,
        out bool collectionSize,
        out string? error)
    {
        var parts = path.Split('.');
        if (parts.Any(part => part.Length == 0)) {
            bind = null;
            resultType = null;
            collectionSize = false;
            error = "is empty.";
            return false;
        }

        var scope = new BindScope { CountCollection = countCollection };
        var probe = Expression.Parameter(rootType, "row");
        if (!TryBind(rootType, parts, 0, probe, requireScalar, out var body, out error, requireScalar, scope)) {
            bind = null;
            resultType = null;
            collectionSize = false;
            return false;
        }

        resultType = body!.Type;
        collectionSize = scope.CollectionSize;
        bind = root => {
            TryBind(rootType, parts, 0, root, requireScalar, out var rebound, out var _, requireScalar, scope);
            return rebound!;
        };

        return true;
    }

    /// <summary>Binds <paramref name="parts" /> from <paramref name="partIndex" /> onto <paramref name="root" />.</summary>
    public static bool TryBind(Type rootType, IReadOnlyList<string> parts, int partIndex, Expression root, bool requireScalar, out Expression? body, out string? error)
        => TryBind(rootType, parts, partIndex, root, requireScalar, out body, out error, requireScalar);

    /// <summary>One character or a clamped slice. Null, or a start at or past the end, is null.</summary>
    public static string? SliceString(string? value, QueryPathWindow window)
    {
        if (value is null || window.Start >= value.Length)
            return null;

        if (window.End is not int end)
            return value.Substring(window.Start);

        var take = end - window.Start;
        var available = value.Length - window.Start;
        return value.Substring(window.Start, Math.Min(take, available));
    }

    /// <summary>Orders <paramref name="source" /> and returns the window. Scalar elements order by value. Entities order by one key.</summary>
    public static bool TryOrderedWindow(object source, Type elementType, QueryPathWindow window, out IEnumerable<object?>? ordered, out string? error)
    {
        if (!TryKey(elementType, out var key, out error)) {
            ordered = null;
            return false;
        }

        IEnumerable<object?> sequence = key is null
            ? ((IEnumerable)source).Cast<object?>().OrderBy(value => value)
            : ((IEnumerable)source).Cast<object?>().OrderBy(value => value is null ? null : key.GetValue(value));

        var skipped = sequence.Skip(window.Start);
        ordered = window.End is int end ? skipped.Take(end - window.Start) : skipped;
        return true;
    }

    /// <summary>True for the scalar types an aggregate group key or aggregate field may use.</summary>
    public static bool IsScalar(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying.IsPrimitive || underlying.IsEnum || underlying == typeof(string))
            return true;

        return underlying == typeof(decimal) || underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset) || underlying == typeof(Guid) ||
            underlying == typeof(TimeSpan)
#if NET6_0_OR_GREATER
            || underlying == typeof(DateOnly) || underlying == typeof(TimeOnly)
#endif
            ;
    }

    private static bool TryBind(
        Type currentType,
        IReadOnlyList<string> parts,
        int partIndex,
        Expression current,
        bool requireScalar,
        out Expression? body,
        out string? error,
        bool terminalScalar,
        BindScope? scope = null)
    {
        body = null;
        if (partIndex >= parts.Count) {
            if (terminalScalar && !IsScalar(current.Type)) {
                error = "is not a scalar.";
                return false;
            }

            body = current;
            error = null;
            return true;
        }

        var part = parts[partIndex];
        if (part == "*") {
            error = "cannot include a wildcard.";
            return false;
        }

        if (!TryParseSegment(part, out var name, out var window, out error))
            return false;

        if (SharedEntityMetadataCache.IsCollectionType(currentType))
            currentType = SharedEntityMetadataCache.GetCollectionElementType(currentType);

        var property = SharedEntityMetadataCache.ResolveProperty(currentType, name);
        if (property is null) {
            error = $"was not found on {currentType.Name}.";
            return false;
        }

        var access = Expression.Property(current, property);
        if (window is null) {
            if (!SharedEntityMetadataCache.IsCollectionType(property.PropertyType))
                return TryBind(property.PropertyType, parts, partIndex + 1, access, requireScalar, out body, out error, terminalScalar, scope);

            if (IsTerminalCount(parts, partIndex + 1) || (partIndex + 1 >= parts.Count && scope is { CountCollection: true })) {
                body = CollectionCount(access, property.PropertyType);
                if (scope is not null)
                    scope.CollectionSize = true;

                error = null;
                return true;
            }

            if (partIndex + 1 >= parts.Count || requireScalar) {
                error = scope is { CountCollection: true } && partIndex + 1 < parts.Count ? "is a list and cannot be counted." : "is a collection and cannot be grouped.";
                return false;
            }

            var elementType = SharedEntityMetadataCache.GetCollectionElementType(property.PropertyType);
            var element = Expression.Parameter(elementType, "e");
            if (!TryBind(elementType, parts, partIndex + 1, element, false, out var inner, out error, true, scope))
                return false;

            body = CallSelect(access, elementType, element, inner!);
            return true;
        }

        if (IsString(property.PropertyType)) {
            var sliced = StringSlice(access, window.Value);
            return TryBind(typeof(string), parts, partIndex + 1, sliced, requireScalar, out body, out error, terminalScalar, scope);
        }

        if (!SharedEntityMetadataCache.IsCollectionType(property.PropertyType)) {
            error = "is only valid on a string or a collection.";
            return false;
        }

        var elements = SharedEntityMetadataCache.GetCollectionElementType(property.PropertyType);
        if (!TryKey(elements, out var key, out error))
            return false;

        if (window.Value.IsSingle) {
            if (partIndex + 1 >= parts.Count) {
                if (!IsScalar(elements)) {
                    error = "is not a scalar.";
                    return false;
                }

                body = First(access, elements, key, window.Value.Start);
                error = null;
                return true;
            }

            var element = Expression.Parameter(elements, "e");
            if (!TryBind(elements, parts, partIndex + 1, element, requireScalar, out var inner, out error, terminalScalar, scope))
                return false;

            var selected = CallSelect(Skip(Order(access, elements, key), elements, window.Value.Start), elements, element, inner!);
            body = CallFirstOrDefault(selected, inner!.Type);
            return true;
        }

        if (requireScalar) {
            error = scope is { CountCollection: true } ? "is a list and cannot be counted." : "is a collection and cannot be grouped.";
            return false;
        }

        var item = Expression.Parameter(elements, "e");
        Expression? projected = null;
        if (partIndex + 1 < parts.Count) {
            if (!TryBind(elements, parts, partIndex + 1, item, false, out projected, out error, true, scope))
                return false;
        }
        else if (!IsScalar(elements)) {
            error = "is not a scalar.";
            return false;
        }

        var windowed = Take(Skip(Order(access, elements, key), elements, window.Value.Start), elements, window.Value);
        body = projected is null ? windowed : CallSelect(windowed, elements, item, projected);
        error = null;
        return true;
    }

    /// <summary>Orders, skips, and takes <paramref name="collection" />. Used when a merged select projects several leaves of one window.</summary>
    public static bool TryWindowSequence(Expression collection, Type elementType, QueryPathWindow window, out Expression? body, out string? error)
    {
        if (window.IsSingle) {
            body = null;
            error = "is a collection and cannot be grouped.";
            return false;
        }

        if (!TryKey(elementType, out var key, out error)) {
            body = null;
            return false;
        }

        body = Take(Skip(Order(collection, elementType, key), elementType, window.Start), elementType, window);
        return true;
    }

    private static bool TryNonNegative(string text, out int value)
    {
        value = 0;
        if (text.Length == 0 || text[0] == '-' || text[0] == '+')
            return false;

        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }

    private static bool IsString(Type type) => (Nullable.GetUnderlyingType(type) ?? type) == typeof(string);

    private static bool TryKey(Type elementType, out PropertyInfo? key, out string? error)
    {
        if (IsScalar(elementType)) {
            key = null;
            error = null;
            return true;
        }

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
        var keyed = elementType.GetProperties(flags).Where(IsKey).ToArray();
        if (keyed.Length == 1) {
            key = keyed[0];
            error = null;
            return true;
        }

        if (keyed.Length > 1) {
            key = null;
            error = "has no key to order by.";
            return false;
        }

        key = elementType.GetProperty("Id", flags);
        if (key is not null) {
            error = null;
            return true;
        }

        error = "has no key to order by.";
        return false;
    }

    private static bool IsKey(PropertyInfo property) => property.GetCustomAttributes(true).Any(attribute => attribute.GetType().Name == "KeyAttribute");

    private static Expression StringSlice(Expression value, QueryPathWindow window)
    {
        var text = value.Type == typeof(string) ? value : Expression.Convert(value, typeof(string));
        var length = Expression.Property(text, nameof(string.Length));
        var start = Expression.Constant(window.Start);
        Expression slice;
        if (window.End is not int end)
            slice = Expression.Call(text, SubstringStart, start);
        else {
            var take = end - window.Start;
            var full = Expression.Call(text, SubstringLength, start, Expression.Constant(take));
            var partial = Expression.Call(text, SubstringLength, start, Expression.Subtract(length, start));
            var enough = Expression.GreaterThanOrEqual(length, Expression.Constant(window.Start + take));
            slice = Expression.Condition(enough, full, partial);
        }

        var missing = Expression.OrElse(Expression.Equal(text, Expression.Constant(null, typeof(string))), Expression.GreaterThanOrEqual(start, length));
        return Expression.Condition(missing, Expression.Constant(null, typeof(string)), slice);
    }

    private static bool IsTerminalCount(IReadOnlyList<string> parts, int index)
    {
        if (index != parts.Count - 1)
            return false;

        return TryParseSegment(parts[index], out var name, out var window, out var _) && window is null && name.Equals("count", StringComparison.OrdinalIgnoreCase);
    }

    private static Expression CollectionCount(Expression collection, Type collectionType)
    {
        var elementType = SharedEntityMetadataCache.GetCollectionElementType(collectionType);
        var queryable = Expression.Call(SharedEntityMetadataCache.GetProjectionAsQueryableMethod(elementType), collection);
        return Expression.Call(SharedEntityMetadataCache.GetProjectionQueryableCountMethod(elementType), queryable);
    }

    private static Expression Order(Expression collection, Type elementType, PropertyInfo? key)
    {
        var element = Expression.Parameter(elementType, "e");
        Expression keyBody = key is null ? element : Expression.Property(element, key);
        var lambda = Expression.Lambda(keyBody, element);
        var method = EnumerableMethod(nameof(Enumerable.OrderBy), 2, method => FuncArity(method) == 2).MakeGenericMethod(elementType, keyBody.Type);
        return Expression.Call(method, collection, lambda);
    }

    private static Expression Skip(Expression source, Type elementType, int start)
    {
        var method = EnumerableCountArg(nameof(Enumerable.Skip)).MakeGenericMethod(elementType);
        return Expression.Call(method, source, Expression.Constant(start));
    }

    private static Expression Take(Expression source, Type elementType, QueryPathWindow window)
    {
        if (window.End is not int end)
            return source;

        var method = EnumerableCountArg(nameof(Enumerable.Take)).MakeGenericMethod(elementType);
        return Expression.Call(method, source, Expression.Constant(end - window.Start));
    }

    private static MethodInfo EnumerableCountArg(string name)
        => typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == name && method.IsGenericMethodDefinition && method.GetParameters() is [var _, var count] && count.ParameterType == typeof(int));

    private static Expression First(Expression collection, Type elementType, PropertyInfo? key, int start)
        => CallFirstOrDefault(Skip(Order(collection, elementType, key), elementType, start), elementType);

    private static Expression CallSelect(Expression source, Type elementType, ParameterExpression element, Expression body)
    {
        var lambda = Expression.Lambda(body, element);
        var method = EnumerableMethod(nameof(Enumerable.Select), 2, method => FuncArity(method) == 2).MakeGenericMethod(elementType, body.Type);
        return Expression.Call(method, source, lambda);
    }

    private static Expression CallFirstOrDefault(Expression source, Type elementType)
    {
        var method = EnumerableMethod(nameof(Enumerable.FirstOrDefault), 1, null).MakeGenericMethod(elementType);
        return Expression.Call(method, source);
    }

    private static MethodInfo EnumerableMethod(string name, int parameters, Func<MethodInfo, bool>? match)
    {
        var methods = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == name && method.IsGenericMethodDefinition && method.GetParameters().Length == parameters);

        if (match is not null)
            methods = methods.Where(match);

        return methods.Single();
    }

    private static int FuncArity(MethodInfo method) => method.GetParameters()[1].ParameterType.GetGenericArguments().Length;

    private sealed class BindScope
    {
        public bool CountCollection { get; init; }

        public bool CollectionSize { get; set; }
    }
}