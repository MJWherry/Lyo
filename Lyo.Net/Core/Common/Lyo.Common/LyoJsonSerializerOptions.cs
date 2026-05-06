using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Exceptions;

namespace Lyo.Common;

/// <summary>HTTP JSON defaults shared across Lyo APIs and first-party clients: Web preset, camelCase string enums, <see cref="ReferenceHandler.IgnoreCycles"/> for EF-like graphs.</summary>
public static class LyoJsonSerializerOptions
{
    /// <summary>Creates a new options instance (safe for DI or further mutation).</summary>
    public static JsonSerializerOptions Create() 
        => new() {
            PropertyNamingPolicy =  JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

    /// <summary>Creates options then applies <paramref name="configure"/>.</summary>
    public static JsonSerializerOptions Create(Action<JsonSerializerOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        var options = Create();
        configure(options);
        return options;
    }

    /// <summary>
    /// Copies Lyo HTTP JSON defaults onto <paramref name="target"/> for use with ASP.NET Core
    /// <c>ConfigureHttpJsonOptions</c>, where the serializer must be mutated in place.
    /// </summary>
    public static void ApplyTo(JsonSerializerOptions target)
    {
        ArgumentHelpers.ThrowIfNull(target);
        var source = Create();
        target.PropertyNamingPolicy = source.PropertyNamingPolicy;
        target.PropertyNameCaseInsensitive = source.PropertyNameCaseInsensitive;
        target.NumberHandling = source.NumberHandling;
        target.AllowTrailingCommas = source.AllowTrailingCommas;
        target.ReadCommentHandling = source.ReadCommentHandling;
        target.Encoder = source.Encoder;
        target.ReferenceHandler = source.ReferenceHandler;
        foreach (var c in source.Converters) {
            if (target.Converters.All(e => e.GetType() != c.GetType()))
                target.Converters.Add(c);
        }
    }
}

/// <summary>Fluent helper to compose <see cref="LyoJsonSerializerOptions"/> with extra converters and flags.</summary>
public sealed class LyoJsonSerializerOptionsBuilder
{
    private readonly JsonSerializerOptions _options;

    public LyoJsonSerializerOptionsBuilder()
    {
        _options = LyoJsonSerializerOptions.Create();
    }

    /// <summary>Starts from a copy of <paramref name="baseline"/> instead of Lyo defaults.</summary>
    public LyoJsonSerializerOptionsBuilder(JsonSerializerOptions baseline)
    {
        ArgumentHelpers.ThrowIfNull(baseline);
        _options = new(baseline);
    }

    public LyoJsonSerializerOptionsBuilder AddConverter(JsonConverter converter)
    {
        ArgumentHelpers.ThrowIfNull(converter);
        _options.Converters.Add(converter);
        return this;
    }

    public LyoJsonSerializerOptionsBuilder WithWriteIndented(bool writeIndented = true)
    {
        _options.WriteIndented = writeIndented;
        return this;
    }

    public LyoJsonSerializerOptionsBuilder WithDefaultIgnoreCondition(JsonIgnoreCondition condition)
    {
        _options.DefaultIgnoreCondition = condition;
        return this;
    }

    /// <summary>Returns a new <see cref="JsonSerializerOptions"/> copy of the built configuration.</summary>
    public JsonSerializerOptions Build() => new(_options);
}
