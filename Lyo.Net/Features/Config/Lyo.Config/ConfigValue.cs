using System.Text.Json;
using Lyo.Exceptions;

namespace Lyo.Config;

/// <summary>Represents a typed configuration value persisted as JSON.</summary>
public sealed class ConfigValue
{
    /// <summary>
    /// Gets or sets the CLR type name for JSON, same convention as <see cref="ConfigDefinitionRecord.ForEntityType" /> / <see cref="ConfigDefinitionRecord.ForValueType" />:
    /// <see cref="Type.FullName" /> (see <see cref="GetTypeName(System.Type)" />).
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the serialized JSON payload.</summary>
    public string Json { get; set; } = "null";

    /// <summary>Creates a typed config value from the supplied value.</summary>
    public static ConfigValue From<T>(T value, JsonSerializerOptions? options = null) => From(typeof(T), value, options);

    /// <summary>Creates a typed config value from the supplied value and explicit type.</summary>
    public static ConfigValue From(Type type, object? value, JsonSerializerOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(type, nameof(type));
        options ??= ConfigJsonSerializerOptions.Default;
        return new() { TypeName = GetTypeName(type), Json = JsonSerializer.Serialize(value, type, options) };
    }

    /// <summary>Deserializes the value to the given type.</summary>
    public T? GetValue<T>(JsonSerializerOptions? options = null) => JsonSerializer.Deserialize<T>(Json, options ?? ConfigJsonSerializerOptions.Default);

    /// <summary>Deserializes the value to the given runtime type.</summary>
    public object? GetValue(Type type, JsonSerializerOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(type, nameof(type));
        return JsonSerializer.Deserialize(Json, type, options ?? ConfigJsonSerializerOptions.Default);
    }

    /// <summary>Resolves the serialized CLR type name to a runtime type.</summary>
    public Type ResolveType()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(TypeName, nameof(TypeName));
        var type = TryResolveType(TypeName);
        OperationHelpers.ThrowIfNull(type, $"Unable to resolve config value type '{TypeName}'.");
        return type;
    }

    /// <summary>Resolves a type name stored as <see cref="Type.FullName" /> or assembly-qualified name (for example legacy rows).</summary>
    public static Type? TryResolveType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        var t = Type.GetType(typeName, false);
        if (t != null)
            return t;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            t = assembly.GetType(typeName);
            if (t != null)
                return t;
        }

        return null;
    }

    /// <summary>Returns true when the supplied type name resolves to the same CLR type.</summary>
    public bool MatchesType(string expectedTypeName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(expectedTypeName, nameof(expectedTypeName));
        return TypeNameComparer.Equals(TypeName, expectedTypeName);
    }

    /// <inheritdoc cref="GetTypeName(Type)" />
    public static string GetTypeName<T>() => GetTypeName(typeof(T));

    /// <summary>
    /// Returns the CLR type name for storage and for <see cref="ConfigDefinitionRecord.ForValueType" />, matching <see cref="ConfigDefinitionRecord.ForEntityType" />:
    /// <see cref="Type.FullName" />, then assembly-qualified name, then <see cref="Type.Name" />.
    /// </summary>
    public static string GetTypeName(Type type)
    {
        ArgumentHelpers.ThrowIfNull(type, nameof(type));
        return type.FullName ?? type.AssemblyQualifiedName ?? type.Name;
    }

    internal static class TypeNameComparer
    {
        public static bool Equals(string left, string right)
        {
            if (string.Equals(left, right, StringComparison.Ordinal))
                return true;

            var leftType = TryResolveType(left);
            var rightType = TryResolveType(right);
            if (leftType != null && rightType != null)
                return leftType == rightType;

            return false;
        }
    }
}