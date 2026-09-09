using System.Reflection;
using System.Text.Json;
using Lyo.Common.Core;
using Lyo.Common.Metadata.Records;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;
using Microsoft.AspNetCore.Components;

namespace Lyo.Reporting.Web;

/// <summary>Resolves composition component FullNames and builds <c>DynamicComponent</c> parameter dictionaries.</summary>
public static class ReportEmbedBinder
{
    /// <summary>Host-supplied component FullNames offered in the workbench picker. Any loaded <see cref="IComponent" /> FullName still resolves. Defaults empty.</summary>
    public static IReadOnlyList<string> KnownComponentTypes { get; set; } = [];

    /// <summary>Resolves <paramref name="typeFullName" /> to an <see cref="IComponent" /> type, or null with <paramref name="error" /> set.</summary>
    public static Type? TryResolveComponent(string? typeFullName, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(typeFullName)) {
            error = "Component type is empty.";
            return null;
        }

        var type = LyoTypeInfo.TryResolveClrType(typeFullName.Trim());
        if (type is null) {
            error = $"Could not resolve component type '{typeFullName}'.";
            return null;
        }

        if (!typeof(IComponent).IsAssignableFrom(type)) {
            error = $"Type '{type.FullName}' is not an IComponent.";
            return null;
        }

        return type;
    }

    /// <summary>Public writable <c>[Parameter]</c> properties on <paramref name="componentType" />.</summary>
    public static IReadOnlyList<PropertyInfo> ParameterProperties(Type componentType)
    {
        return componentType.WritableProperties().Where(p => p.HasAttribute<ParameterAttribute>()).ToArray();
    }

    /// <summary>Builds a parameter dictionary for <c>DynamicComponent</c> from block bindings. Missing types yield an empty dictionary.</summary>
    public static Dictionary<string, object>? BindBlock(Block block, IReadOnlyDictionary<string, string?>? reportParams, out string? error)
    {
        var type = TryResolveComponent(block.ComponentType, out error);
        if (type is null)
            return null;

        return Bind(type, block.ParameterBindings, reportParams, out error);
    }

    /// <summary>Binds report params onto a root component by explicit bindings, then by matching parameter names.</summary>
    public static Dictionary<string, object> Bind(Type componentType, Dictionary<string, ComponentBinding>? bindings, IReadOnlyDictionary<string, string?>? reportParams, out string? error)
    {
        error = null;
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        var props = ParameterProperties(componentType);
        reportParams ??= new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in props) {
            if (bindings is not null && bindings.TryGetValue(prop.Name, out var binding) && !string.IsNullOrWhiteSpace(binding.Value)) {
                var raw = binding.Kind == ComponentBindingKind.Param ? Lookup(reportParams, binding.Value!) : binding.Value;
                var converted = ConvertValue(raw, prop.PropertyType, out error);
                if (error is not null)
                    return result;

                if (converted is not null)
                    result[prop.Name] = converted;
                continue;
            }

            var byName = Lookup(reportParams, prop.Name);
            if (byName is null)
                continue;

            var value = ConvertValue(byName, prop.PropertyType, out error);
            if (error is not null)
                return result;

            if (value is not null)
                result[prop.Name] = value;
        }

        return result;
    }

    private static string? Lookup(IReadOnlyDictionary<string, string?> parameters, string key)
        => parameters.TryGetValue(key, out var value) ? value : null;

    private static object? ConvertValue(string? raw, Type target, out string? error)
    {
        error = null;
        if (raw is null)
            return null;

        if (target == typeof(string))
            return raw;

        try {
            if (target.IsPrimitive || target == typeof(decimal) || target == typeof(Guid) || target == typeof(DateTime) || target == typeof(DateOnly) || target == typeof(TimeOnly) ||
                target == typeof(DateTimeOffset))
                return JsonSerializer.Deserialize(WrapScalar(raw, target), target);

            return JsonSerializer.Deserialize(raw, target);
        }
        catch (JsonException ex) {
            error = $"Could not bind JSON to {target.FullName}: {ex.Message}";
            return null;
        }
    }

    private static string WrapScalar(string raw, Type target)
    {
        var trimmed = raw.Trim();
        if (target == typeof(string))
            return JsonSerializer.Serialize(raw);

        if (trimmed.StartsWith('{') || trimmed.StartsWith('[') || trimmed.StartsWith('"') || trimmed is "true" or "false" or "null")
            return trimmed;

        if (target == typeof(Guid) || target == typeof(DateTime) || target == typeof(DateOnly) || target == typeof(TimeOnly) || target == typeof(DateTimeOffset))
            return JsonSerializer.Serialize(raw);

        return trimmed;
    }
}
