using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Lyo.Common.Core.Conversion;
using Lyo.Exceptions;

namespace Lyo.Common.Core;

/// <summary>
/// Shared CLR member lookup, dotted-path get/set, expression path extraction, attribute reads, catalog static-field discovery, and scalar classification.
/// </summary>
/// <remarks>
/// Call sites use extension members (<c>typeof(T).FindProperty("Id")</c>, <c>row.GetPropertyValue("Address.City")</c>, <c>expr.TryGetMemberPath()</c>) after
/// <c>using Lyo.Common.Core</c>. JSON/dictionary projected rows stay in the web grid helper; QueryProject name attributes and <c>Count()</c> suffixes stay in Query.
/// </remarks>
public static class LyoReflection
{
    private const BindingFlags PublicInstanceIgnoreCase = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
    private const BindingFlags PublicDeclaredIgnoreCase = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase;
    private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;

    private static readonly HashSet<Type> ScalarTypes = CreateScalarTypes();

    private static HashSet<Type> CreateScalarTypes()
    {
        HashSet<Type> set = [
            typeof(string), typeof(decimal), typeof(Guid), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan), typeof(Uri)
        ];
#if NET6_0_OR_GREATER
        set.Add(typeof(DateOnly));
        set.Add(typeof(TimeOnly));
#endif
        return set;
    }

    private static readonly ConcurrentDictionary<string, PropertyInfo?> PropertyCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, FieldInfo?> FieldCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, MethodInfo?> MethodCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, ConstructorInfo?> ConstructorCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<Type, IReadOnlyList<PropertyInfo>> ReadableCache = new();
    private static readonly ConcurrentDictionary<Type, IReadOnlyList<PropertyInfo>> WritableCache = new();
    private static readonly ConcurrentDictionary<string, object> StaticFieldsCache = new(StringComparer.Ordinal);

    extension(Type type)
    {
        /// <summary>Public instance property by name (ignore-case), skipping indexers. Cached per type and name.</summary>
        public PropertyInfo? FindProperty(string name)
        {
            ArgumentHelpers.ThrowIfNull(type);
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return PropertyCache.GetOrAdd(MemberKey(type, name), _ => {
                try {
                    var prop = type.GetProperty(name.Trim(), PublicInstanceIgnoreCase);
                    return prop is null || prop.IsIndexer() ? null : prop;
                }
                catch (AmbiguousMatchException) {
                    return type.GetProperties(PublicInstanceIgnoreCase)
                        .FirstOrDefault(p => !p.IsIndexer() && string.Equals(p.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
                }
            });
        }

        /// <summary>Public instance or static field by name (ignore-case). Cached per type and name.</summary>
        public FieldInfo? FindField(string name)
        {
            ArgumentHelpers.ThrowIfNull(type);
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return FieldCache.GetOrAdd(MemberKey(type, name), _ => type.GetField(name.Trim(), PublicDeclaredIgnoreCase));
        }

        /// <summary>Public instance or static method by name (ignore-case) and parameter types. Cached per type, name, and signature.</summary>
        public MethodInfo? FindMethod(string name, params Type[] parameterTypes)
        {
            ArgumentHelpers.ThrowIfNull(type);
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var types = parameterTypes ?? [];
            return MethodCache.GetOrAdd(MethodKey(type, name, types), _ => FindMethodCore(type, name.Trim(), types));
        }

        /// <summary>Public constructor matching <paramref name="parameterTypes" />. Cached per type and signature.</summary>
        public ConstructorInfo? FindConstructor(params Type[] parameterTypes)
        {
            ArgumentHelpers.ThrowIfNull(type);
            var types = parameterTypes ?? [];
            return ConstructorCache.GetOrAdd(MethodKey(type, ".ctor", types), _ => type.GetConstructor(types));
        }

        /// <summary>Public instance properties that can be read and are not indexers.</summary>
        public IReadOnlyList<PropertyInfo> ReadableProperties()
        {
            ArgumentHelpers.ThrowIfNull(type);
            return ReadableCache.GetOrAdd(type, static t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(static p => p.IsReadable()).ToArray());
        }

        /// <summary>Public instance properties that can be written and are not indexers.</summary>
        public IReadOnlyList<PropertyInfo> WritableProperties()
        {
            ArgumentHelpers.ThrowIfNull(type);
            return WritableCache.GetOrAdd(type, static t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(static p => p.IsWritable()).ToArray());
        }

        /// <summary>Readable public instance properties whose type is <see cref="IsScalar" /> (Select-style scalars, not collections or nested objects).</summary>
        public IReadOnlyList<PropertyInfo> ScalarProperties()
        {
            ArgumentHelpers.ThrowIfNull(type);
            return type.ReadableProperties().Where(static p => p.IsScalarProperty()).ToArray();
        }

        /// <summary>Values of public static fields whose <see cref="FieldInfo.FieldType" /> is <typeparamref name="T" /> (Metadata catalog discovery).</summary>
        public IReadOnlyList<T> PublicStaticFields<T>()
        {
            ArgumentHelpers.ThrowIfNull(type);
            var key = $"{type.FullName}\u001f{typeof(T).FullName}";
            return (IReadOnlyList<T>)StaticFieldsCache.GetOrAdd(key, _ => type.GetFields(PublicStatic)
                .Where(static f => f.FieldType == typeof(T) && f.IsPublicStatic())
                .Select(static f => (T)f.GetValue(null)!)
                .ToArray());
        }

        /// <summary>True when every dotted segment of <paramref name="path" /> resolves to a public instance property on this type.</summary>
        public bool HasPath(string path) => type.ResolvePath(path) is not null;

        /// <summary>Property chain for a dotted path, or <see langword="null" /> when any segment is missing.</summary>
        public IReadOnlyList<PropertyInfo>? ResolvePath(string path)
        {
            ArgumentHelpers.ThrowIfNull(type);
            var parts = SplitPath(path);
            if (parts.Length == 0)
                return null;

            var chain = new List<PropertyInfo>(parts.Length);
            var current = type;
            foreach (var part in parts) {
                var prop = current.FindProperty(part);
                if (prop is null)
                    return null;

                chain.Add(prop);
                current = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            }

            return chain;
        }

        /// <summary>CLR type of the leaf property on a dotted path, or <see langword="null" /> when the path does not resolve.</summary>
        public Type? GetPathType(string path)
        {
            var chain = type.ResolvePath(path);
            return chain is { Count: > 0 } ? chain[^1].PropertyType : null;
        }

        /// <summary>
        /// True for primitives, enums, <see cref="string" />, <see cref="decimal" />, <see cref="Guid" />, dates, <see cref="TimeSpan" />, and <see cref="Uri" /> (nullable wrappers
        /// unwrapped). Not collections, not POCOs, not <c>byte[]</c>.
        /// </summary>
        public bool IsScalar()
        {
            ArgumentHelpers.ThrowIfNull(type);
            var t = Nullable.GetUnderlyingType(type) ?? type;
            return t.IsPrimitive || t.IsEnum || ScalarTypes.Contains(t);
        }

        /// <summary>True when the type is <see cref="IsScalar" /> or <c>byte[]</c> (EF/projection “not a navigation”).</summary>
        public bool IsSimple()
        {
            ArgumentHelpers.ThrowIfNull(type);
            var t = Nullable.GetUnderlyingType(type) ?? type;
            return t.IsScalar() || t == typeof(byte[]);
        }

        /// <summary>Tries to construct a public parameterless instance (value types included). Returns <see langword="false" /> when no such constructor exists.</summary>
        public bool TryCreateInstance([NotNullWhen(true)] out object? instance)
        {
            ArgumentHelpers.ThrowIfNull(type);
            instance = null;
            try {
                if (type.IsValueType) {
                    instance = Activator.CreateInstance(type);
                    return instance is not null;
                }

                var ctor = type.FindConstructor();
                if (ctor is null)
                    return false;

                instance = ctor.Invoke(null);
                return instance is not null;
            }
            catch {
                instance = null;
                return false;
            }
        }

        /// <summary>Zero value for value types; <see langword="null" /> for reference types.</summary>
        public object? DefaultValue()
        {
            ArgumentHelpers.ThrowIfNull(type);
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }

    extension(object? instance)
    {
        /// <summary>Reads a dotted public instance property path (ignore-case). Returns <see langword="null" /> when the instance, a segment, or the leaf is null or missing.</summary>
        public object? GetPropertyValue(string path)
        {
            return TryWalk(instance, path, set: false, value: null, convertThrowing: false, out var current, out _) ? current : null;
        }

        /// <summary>True when the path resolves to a non-null value.</summary>
        public bool TryGetPropertyValue(string path, [NotNullWhen(true)] out object? value)
        {
            if (!TryWalk(instance, path, set: false, value: null, convertThrowing: false, out var current, out _) || current is null) {
                value = null;
                return false;
            }

            value = current;
            return true;
        }

        /// <summary>Reads a dotted path and converts the leaf with <see cref="TypeConversion.TryConvertTo{T}(object?, out T?, bool)" />. Missing or failed conversion yields default.</summary>
        public T? GetPropertyValue<T>(string path)
        {
            var raw = instance.GetPropertyValue(path);
            if (raw is T typed)
                return typed;

            return TypeConversion.TryConvertTo<T>(raw, out var converted) ? converted : default;
        }

        /// <summary>Writes a dotted path after converting <paramref name="value" /> to the leaf property type. Returns <see langword="false" /> on miss or conversion failure.</summary>
        public bool TrySetPropertyValue(string path, object? value)
        {
            if (instance is null || !TryWalk(instance, path, set: true, value, convertThrowing: false, out _, out var written))
                return false;

            return written;
        }

        /// <summary>Writes a dotted path, converting <paramref name="value" /> to the leaf type.</summary>
        /// <exception cref="ArgumentException">The instance is null or the path does not resolve to a writable property.</exception>
        /// <exception cref="TypeConversionException">The value cannot become the leaf property type.</exception>
        public void SetPropertyValue(string path, object? value)
        {
            ArgumentHelpers.ThrowIfNull(instance);
            if (!TryWalk(instance, path, set: true, value, convertThrowing: true, out _, out var written) || !written)
                ArgumentHelpers.ThrowIf(true, "Path did not resolve to a writable public instance property.");
        }

        /// <summary>Public field value by name (ignore-case), or <see langword="null" /> when the instance or field is missing.</summary>
        public object? GetFieldValue(string name)
        {
            if (instance is null || string.IsNullOrWhiteSpace(name))
                return null;

            var field = instance.GetType().FindField(name);
            return field?.GetValue(instance);
        }

        /// <summary>Invokes a public method resolved by name and parameter types. Returns <see langword="false" /> when the method is missing or invocation throws.</summary>
        public bool TryInvoke(string methodName, Type[] parameterTypes, object?[] args, out object? result)
        {
            result = null;
            if (instance is null || string.IsNullOrWhiteSpace(methodName))
                return false;

            var method = instance.GetType().FindMethod(methodName, parameterTypes ?? []);
            if (method is null)
                return false;

            try {
                result = method.Invoke(method.IsStatic ? null : instance, args);
                return true;
            }
            catch {
                result = null;
                return false;
            }
        }
    }

    extension(Expression? expr)
    {
        /// <summary>Dotted member path from a member-access chain (properties and fields), unwrapping <c>Convert</c>/<c>ConvertChecked</c>. Null when the expression is not a chain.</summary>
        public string? TryGetMemberPath()
        {
            if (expr is null)
                return null;

            var body = UnwrapConvert(expr is LambdaExpression lambda ? lambda.Body : expr);
            var parts = new List<string>();
            while (body is MemberExpression member) {
                parts.Insert(0, member.Member.Name);
                if (member.Expression is null)
                    break;

                body = UnwrapConvert(member.Expression);
            }

            return parts.Count == 0 ? null : string.Join(".", parts);
        }

        /// <summary>Leaf member name from a member-access chain, or <see langword="null" /> when the expression is not a chain.</summary>
        public string? TryGetMemberName()
        {
            var path = expr.TryGetMemberPath();
            if (path is null)
                return null;

            var dot = path.LastIndexOf('.');
            return dot < 0 ? path : path[(dot + 1)..];
        }
    }

    extension(LambdaExpression expr)
    {
        /// <summary>Dotted member path from a lambda. Throws when the body is not a member-access chain.</summary>
        /// <exception cref="ArgumentException">The lambda is not a chain of member accesses.</exception>
        public string GetMemberPath()
        {
            ArgumentHelpers.ThrowIfNull(expr);
            var path = ((Expression)expr).TryGetMemberPath();
            ArgumentHelpers.ThrowIf(path is null, "Invalid expression. Must be a property or field access expression.");
            return path!;
        }

        /// <summary>Leaf member name from a lambda. Throws when the body is not a member-access chain.</summary>
        /// <exception cref="ArgumentException">The lambda is not a chain of member accesses.</exception>
        public string GetMemberName()
        {
            ArgumentHelpers.ThrowIfNull(expr);
            var name = ((Expression)expr).TryGetMemberName();
            ArgumentHelpers.ThrowIf(name is null, "Invalid expression. Must be a property or field access expression.");
            return name!;
        }
    }

    extension<T, TProp>(Expression<Func<T, TProp>> expr)
    {
        /// <inheritdoc cref="GetMemberPath" />
        public string GetMemberPath() => ((LambdaExpression)expr).GetMemberPath();

        /// <inheritdoc cref="GetMemberName" />
        public string GetMemberName() => ((LambdaExpression)expr).GetMemberName();
    }

    extension(MemberInfo member)
    {
        /// <summary>First custom attribute of type <typeparamref name="TAttr" />, or <see langword="null" />.</summary>
        public TAttr? GetAttribute<TAttr>(bool inherit = true) where TAttr : Attribute
        {
            ArgumentHelpers.ThrowIfNull(member);
            return member.GetCustomAttribute<TAttr>(inherit);
        }

        /// <summary>True when <typeparamref name="TAttr" /> is present.</summary>
        public bool HasAttribute<TAttr>(bool inherit = true) where TAttr : Attribute => member.GetAttribute<TAttr>(inherit) is not null;

        /// <summary>All custom attributes of type <typeparamref name="TAttr" />.</summary>
        public IReadOnlyList<TAttr> GetAttributes<TAttr>(bool inherit = true) where TAttr : Attribute
        {
            ArgumentHelpers.ThrowIfNull(member);
            return member.GetCustomAttributes<TAttr>(inherit).ToArray();
        }
    }

    extension(PropertyInfo property)
    {
        /// <summary>True when the property has index parameters.</summary>
        public bool IsIndexer()
        {
            ArgumentHelpers.ThrowIfNull(property);
            return property.GetIndexParameters().Length > 0;
        }

        /// <summary>True when the property can be read and is not an indexer.</summary>
        public bool IsReadable()
        {
            ArgumentHelpers.ThrowIfNull(property);
            return property.CanRead && !property.IsIndexer();
        }

        /// <summary>True when the property can be written and is not an indexer.</summary>
        public bool IsWritable()
        {
            ArgumentHelpers.ThrowIfNull(property);
            return property.CanWrite && !property.IsIndexer();
        }

        /// <summary>True when <see cref="PropertyInfo.PropertyType" /> is <see cref="IsScalar" />.</summary>
        public bool IsScalarProperty()
        {
            ArgumentHelpers.ThrowIfNull(property);
            return property.PropertyType.IsScalar();
        }
    }

    extension(FieldInfo field)
    {
        /// <summary>True when the field is public and static.</summary>
        public bool IsPublicStatic()
        {
            ArgumentHelpers.ThrowIfNull(field);
            return field.IsPublic && field.IsStatic;
        }
    }

    private static string[] SplitPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return [];

        var raw = path!.Split('.');
        var parts = new List<string>(raw.Length);
        foreach (var segment in raw) {
            var trimmed = segment.Trim();
            if (trimmed.Length > 0)
                parts.Add(trimmed);
        }

        return parts.Count == 0 ? [] : [.. parts];
    }

    private static string MemberKey(Type type, string name) => $"{type.FullName}\u001f{name.Trim().ToLowerInvariant()}";

    private static string MethodKey(Type type, string name, Type[] parameterTypes)
        => $"{type.FullName}\u001f{name.Trim().ToLowerInvariant()}\u001f{string.Join(",", parameterTypes.Select(static t => t.FullName))}";

    private static MethodInfo? FindMethodCore(Type type, string name, Type[] parameterTypes)
    {
        foreach (var method in type.GetMethods(PublicDeclaredIgnoreCase)) {
            if (!string.Equals(method.Name, name, StringComparison.OrdinalIgnoreCase))
                continue;

            var parameters = method.GetParameters();
            if (parameters.Length != parameterTypes.Length)
                continue;

            var match = true;
            for (var i = 0; i < parameters.Length; i++) {
                if (parameters[i].ParameterType != parameterTypes[i]) {
                    match = false;
                    break;
                }
            }

            if (match)
                return method;
        }

        return null;
    }

    private static Expression UnwrapConvert(Expression expr)
    {
        while (expr is UnaryExpression unary && unary.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
            expr = unary.Operand;

        return expr;
    }

    private static bool TryWalk(object? instance, string path, bool set, object? value, bool convertThrowing, out object? current, out bool written)
    {
        written = false;
        current = instance;
        var parts = SplitPath(path);
        if (current is null || parts.Length == 0)
            return false;

        var last = parts.Length - 1;
        for (var i = 0; i < parts.Length; i++) {
            if (current is null)
                return false;

            var prop = current.GetType().FindProperty(parts[i]);
            if (prop is null)
                return false;

            if (i == last && set) {
                if (!prop.CanWrite)
                    return false;

                object? converted = value;
                if (value is not null && !prop.PropertyType.IsInstanceOfType(value)) {
                    if (convertThrowing)
                        converted = TypeConversion.ConvertTo(value, prop.PropertyType);
                    else if (!TypeConversion.TryConvertTo(value, prop.PropertyType, out converted))
                        return false;
                }

                prop.SetValue(current, converted);
                written = true;
                return true;
            }

            current = prop.GetValue(current);
        }

        return true;
    }
}
