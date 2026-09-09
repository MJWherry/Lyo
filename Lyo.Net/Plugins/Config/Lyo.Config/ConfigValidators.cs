using System.Text.Json;
using Lyo.Common.Core.Conversion;
using Lyo.Validation;
using Lyo.Validation.Models;

namespace Lyo.Config;

/// <summary>Config definition and binding writes fluent validators.</summary>
public static class ConfigValidators
{
    /// <summary>Common definition validator (required fields, resolvable type, default JSON matches type).</summary>
    public static IValidator<ConfigDefinitionRecord> Definition { get; } = BuildDefinition();

    /// <summary>Common binding validator (required fields, value JSON matches definition type when both are set).</summary>
    public static IValidator<ConfigBindingRecord> Binding { get; } = BuildBinding();

    private static IValidator<ConfigDefinitionRecord> BuildDefinition()
        => ValidatorBuilder<ConfigDefinitionRecord>.Create()
            .RuleFor(x => x.SubjectEntityType).NotWhiteSpace()
            .RuleFor(x => x.Key).NotWhiteSpace()
            .RuleFor(x => x.ForValueType).NotWhiteSpace()
            .Must(x => ConfigValue.TryResolveType(x.ForValueType) != null, "config.type.unresolved", "ForValueType does not resolve to a loaded CLR type.")
            .Must(DefaultMatchesType, "config.default.type", "Default value type does not match ForValueType.")
            .Must(DefaultJsonConverts, "config.default.convert", "Default JSON cannot be converted to ForValueType.")
            .Build();

    private static IValidator<ConfigBindingRecord> BuildBinding()
        => ValidatorBuilder<ConfigBindingRecord>.Create()
            .RuleFor(x => x.Key).NotWhiteSpace()
            .RuleFor(x => x.SubjectEntityType).NotWhiteSpace()
            .RuleFor(x => x.SubjectEntityId).NotWhiteSpace()
            .Must(x => x.Value != null, "config.binding.value", "Binding value is required.")
            .Must(x => x.Value == null || !string.IsNullOrWhiteSpace(x.Value.TypeName), "config.binding.type", "Binding value type is required.")
            .Must(x => x.Value == null || ConfigValue.TryResolveType(x.Value.TypeName) != null, "config.binding.type.unresolved", "Binding value type does not resolve to a loaded CLR type.")
            .Must(BindingJsonConverts, "config.binding.convert", "Binding JSON cannot be converted to the value type.")
            .Build();

    private static bool DefaultMatchesType(ConfigDefinitionRecord definition)
        => definition.DefaultValue == null || definition.Accepts(definition.DefaultValue);

    private static bool DefaultJsonConverts(ConfigDefinitionRecord definition)
    {
        if (definition.DefaultValue == null)
            return true;

        var type = ConfigValue.TryResolveType(definition.ForValueType);
        if (type == null)
            return true;

        return ConfigValueConversion.TryConvertJson(definition.DefaultValue.Json, type);
    }

    private static bool BindingJsonConverts(ConfigBindingRecord binding)
    {
        if (binding.Value == null)
            return true;

        var type = ConfigValue.TryResolveType(binding.Value.TypeName);
        if (type == null)
            return true;

        return ConfigValueConversion.TryConvertJson(binding.Value.Json, type);
    }
}

/// <summary>Turns stored JSON into a CLR value using JSON deserialize and <see cref="TypeConversion" /> for scalars.</summary>
public static class ConfigValueConversion
{
    /// <summary>True when <paramref name="json" /> can be read as <paramref name="targetType" />.</summary>
    public static bool TryConvertJson(string? json, Type targetType)
    {
        if (targetType == null)
            return false;

        if (string.IsNullOrWhiteSpace(json) || json == "null")
            return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;

        try {
            var deserialized = JsonSerializer.Deserialize(json, targetType, ConfigJsonSerializerOptions.Default);
            if (deserialized != null)
                return true;
        }
        catch (JsonException) {
            // Otherwise TypeConversion handles quoted scalars and lenient bools.
        }

        try {
            object? raw = json;
            try {
                raw = JsonSerializer.Deserialize<object>(json, ConfigJsonSerializerOptions.Default) ?? json;
            }
            catch (JsonException) {
                raw = json.Trim().Trim('"');
            }

            return TypeConversion.TryConvertTo(raw, targetType, out _);
        }
        catch (TypeConversionException) {
            return false;
        }
    }
}
