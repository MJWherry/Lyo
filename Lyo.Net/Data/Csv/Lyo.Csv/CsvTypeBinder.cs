using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Lyo.Csv.Models;

namespace Lyo.Csv;

/// <summary>Cached property binders for typed CSV read/write.</summary>
internal static class CsvTypeBinder
{
    private static readonly ConcurrentDictionary<Type, TypeMap> Maps = new();
    private static readonly ConcurrentDictionary<Type, ICsvValueConverter> Converters = new();
    private static readonly ConcurrentDictionary<(Type Type, string HeaderKey), int[]> HeaderIndexCache = new();

    static CsvTypeBinder()
    {
        RegisterConverter(typeof(int), new Converters.Int32CsvConverter());
        RegisterConverter(typeof(int?), new Converters.Int32CsvConverter());
        RegisterConverter(typeof(long), new Converters.Int64CsvConverter());
        RegisterConverter(typeof(long?), new Converters.Int64CsvConverter());
        RegisterConverter(typeof(decimal), new Converters.DecimalCsvConverter());
        RegisterConverter(typeof(decimal?), new Converters.DecimalCsvConverter());
        RegisterConverter(typeof(bool), new Converters.YesNoBoolCsvConverter());
        RegisterConverter(typeof(bool?), new Converters.YesNoBoolCsvConverter());
    }

    /// <summary>Registers a converter for <paramref name="type" /> (overrides defaults).</summary>
    public static void RegisterConverter(Type type, ICsvValueConverter converter)
        => Converters[type] = converter;

    public static TypeMap GetMap(Type type) => Maps.GetOrAdd(type, BuildMap);

    public static TypeMap GetMap<T>() => GetMap(typeof(T));

    public static T CreateAndBind<T>(IReadOnlyList<string> headers, IReadOnlyList<string> fields, CultureInfo culture)
    {
        var map = GetMap<T>();
        var indices = GetHeaderIndices(map, headers);
        var instance = map.Factory();
        BindWithIndices(instance, map, indices, fields, culture);
        return (T)instance;
    }

    public static void Bind(object instance, TypeMap map, IReadOnlyList<string> headers, IReadOnlyList<string> fields, CultureInfo culture)
    {
        var indices = GetHeaderIndices(map, headers);
        BindWithIndices(instance, map, indices, fields, culture);
    }

    public static void BindByOrdinal(object instance, TypeMap map, IReadOnlyList<string> fields, CultureInfo culture)
    {
        var count = Math.Min(map.Columns.Length, fields.Count);
        for (var i = 0; i < count; i++) {
            var col = map.Columns[i];
            col.Setter(instance, fields[i], culture);
        }
    }

    public static string[] GetHeaders(TypeMap map)
    {
        var headers = new string[map.Columns.Length];
        for (var i = 0; i < map.Columns.Length; i++)
            headers[i] = map.Columns[i].HeaderName;

        return headers;
    }

    public static string?[] GetFieldValues(object? instance, TypeMap map, CultureInfo culture)
    {
        var values = new string?[map.Columns.Length];
        for (var i = 0; i < map.Columns.Length; i++) {
            var col = map.Columns[i];
            var raw = instance is null ? null : col.Getter(instance);
            values[i] = ConvertFrom(col.Property.PropertyType, raw, culture);
        }

        return values;
    }

    private static void BindWithIndices(object instance, TypeMap map, int[] indices, IReadOnlyList<string> fields, CultureInfo culture)
    {
        for (var i = 0; i < map.Columns.Length; i++) {
            var index = indices[i];
            if (index < 0 || index >= fields.Count)
                continue;

            map.Columns[i].Setter(instance, fields[index], culture);
        }
    }

    private static int[] GetHeaderIndices(TypeMap map, IReadOnlyList<string> headers)
    {
        var key = BuildHeaderKey(headers);
        return HeaderIndexCache.GetOrAdd((map.Type, key), static tuple => {
            // tuple can't capture map — resolve via Maps
            var typeMap = Maps[tuple.Type];
            var hdrs = tuple.HeaderKey.Split('\u001f');
            var indices = new int[typeMap.Columns.Length];
            for (var i = 0; i < typeMap.Columns.Length; i++)
                indices[i] = FindHeaderIndex(hdrs, typeMap.Columns[i].HeaderName);

            return indices;
        });
    }

    private static string BuildHeaderKey(IReadOnlyList<string> headers)
    {
        if (headers.Count == 0)
            return "";

        return string.Join("\u001f", headers.Select(h => h.Trim()));
    }

    private static int FindHeaderIndex(IReadOnlyList<string> headers, string name)
    {
        for (var i = 0; i < headers.Count; i++) {
            if (string.Equals(headers[i], name, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    /// <summary>Used by compiled setters.</summary>
    public static object? ConvertToChecked(Type targetType, string? text, CultureInfo culture, string headerName)
    {
        var value = ConvertTo(targetType, text, culture);
        var isNonNullableValue = targetType.IsValueType && Nullable.GetUnderlyingType(targetType) is null;
        if (value is null) {
            if (!string.IsNullOrEmpty(text) && isNonNullableValue)
                throw new CsvBadDataException($"Cannot convert '{text}' to {targetType.Name} for column '{headerName}'.");
        }

        return value;
    }

    private static object? ConvertTo(Type targetType, string? text, CultureInfo culture)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (Converters.TryGetValue(targetType, out var conv) || Converters.TryGetValue(underlying, out conv))
            return conv.ConvertFromString(text, culture);

        if (string.IsNullOrEmpty(text))
            return targetType.IsValueType && Nullable.GetUnderlyingType(targetType) is null ? Activator.CreateInstance(targetType) : null;

        if (underlying == typeof(string))
            return text;

        if (underlying == typeof(Guid) && Guid.TryParse(text, out var guid))
            return guid;

        if (underlying.IsEnum) {
            try {
                return Enum.Parse(underlying, text, ignoreCase: true);
            }
            catch {
                return null;
            }
        }

        try {
            return Convert.ChangeType(text, underlying, culture);
        }
        catch {
            return null;
        }
    }

    private static string ConvertFrom(Type sourceType, object? value, CultureInfo culture)
    {
        var underlying = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
        if (Converters.TryGetValue(sourceType, out var conv) || Converters.TryGetValue(underlying, out conv))
            return conv.ConvertToString(value, culture);

        return value switch {
            null => "",
            IFormattable f => f.ToString(null, culture) ?? "",
            var o => o.ToString() ?? ""
        };
    }

    private static TypeMap BuildMap(Type type)
    {
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToArray();

        var columns = new List<ColumnMap>();
        foreach (var prop in props) {
            var attr = prop.GetCustomAttribute<CsvColumnAttribute>();
            if (attr?.Ignore == true)
                continue;

            if (!prop.CanWrite)
                continue;

            var name = !string.IsNullOrWhiteSpace(attr?.Name) ? attr!.Name! : prop.Name;
            columns.Add(new(prop, name, CompileSetter(type, prop, name), CompileGetter(type, prop)));
        }

        return new(type, columns.ToArray(), CompileFactory(type));
    }

    private static Func<object> CompileFactory(Type type)
    {
        var ctor = type.GetConstructor(Type.EmptyTypes);
        if (ctor != null)
            return Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(ctor), typeof(object))).Compile();

        return () => Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Type {type.FullName} cannot be constructed.");
    }

    private static Action<object, string?, CultureInfo> CompileSetter(Type declaringType, PropertyInfo prop, string headerName)
    {
        var objParam = Expression.Parameter(typeof(object), "obj");
        var textParam = Expression.Parameter(typeof(string), "text");
        var cultureParam = Expression.Parameter(typeof(CultureInfo), "culture");
        var typed = Expression.Convert(objParam, declaringType);
        var convert = Expression.Call(
            typeof(CsvTypeBinder).GetMethod(nameof(ConvertToChecked))!,
            Expression.Constant(prop.PropertyType, typeof(Type)),
            textParam,
            cultureParam,
            Expression.Constant(headerName));

        var isNonNullable = prop.PropertyType.IsValueType && Nullable.GetUnderlyingType(prop.PropertyType) is null;
        Expression body;
        if (isNonNullable) {
            // skip assign when convert returned null and text was empty (ConvertToChecked returns null without throw)
            var valueVar = Expression.Variable(typeof(object), "value");
            var assignVar = Expression.Assign(valueVar, convert);
            var setProp = Expression.Assign(
                Expression.Property(typed, prop),
                Expression.Convert(valueVar, prop.PropertyType));
            var ifNotNull = Expression.IfThen(Expression.ReferenceNotEqual(valueVar, Expression.Constant(null)), setProp);
            body = Expression.Block([valueVar], assignVar, ifNotNull);
        }
        else {
            var valueVar = Expression.Variable(typeof(object), "value");
            body = Expression.Block(
                [valueVar],
                Expression.Assign(valueVar, convert),
                Expression.Assign(
                    Expression.Property(typed, prop),
                    Expression.Convert(valueVar, prop.PropertyType)));
        }

        return Expression.Lambda<Action<object, string?, CultureInfo>>(body, objParam, textParam, cultureParam).Compile();
    }

    private static Func<object, object?> CompileGetter(Type declaringType, PropertyInfo prop)
    {
        var objParam = Expression.Parameter(typeof(object), "obj");
        var typed = Expression.Convert(objParam, declaringType);
        var access = Expression.Convert(Expression.Property(typed, prop), typeof(object));
        return Expression.Lambda<Func<object, object?>>(access, objParam).Compile();
    }

    internal sealed class TypeMap(Type type, ColumnMap[] columns, Func<object> factory)
    {
        public Type Type { get; } = type;
        public ColumnMap[] Columns { get; } = columns;
        public Func<object> Factory { get; } = factory;
    }

    internal sealed class ColumnMap(
        PropertyInfo property,
        string headerName,
        Action<object, string?, CultureInfo> setter,
        Func<object, object?> getter)
    {
        public PropertyInfo Property { get; } = property;
        public string HeaderName { get; } = headerName;
        public Action<object, string?, CultureInfo> Setter { get; } = setter;
        public Func<object, object?> Getter { get; } = getter;
    }
}
