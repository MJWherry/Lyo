using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Exceptions;

namespace Lyo.Common.Json;

/// <summary>Shared HTTP JSON defaults for Lyo APIs and first-party clients: Web preset, camelCase string enums, <see cref="ReferenceHandler.IgnoreCycles" /> for EF-like graphs.</summary>
public static class LyoJsonSerializerOptions
{
    /// <summary>Mints an options instance that is safe to register in DI or mutate further.</summary>
    public static JsonSerializerOptions Create()
        => new() {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

    /// <summary>Mints options and then runs <paramref name="configure" /> against them.</summary>
    public static JsonSerializerOptions Create(Action<JsonSerializerOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        var options = Create();
        configure(options);
        return options;
    }

    /// <summary>Copies Lyo HTTP JSON defaults onto <paramref name="target" />. Use with ASP.NET Core <c>ConfigureHttpJsonOptions</c>, which mutates the serializer in place.</summary>
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

/// <summary>Fluent composer for <see cref="LyoJsonSerializerOptions" /> plus extra converters and flags.</summary>
public sealed class LyoJsonSerializerOptionsBuilder
{
    private readonly JsonSerializerOptions _options;

    public LyoJsonSerializerOptionsBuilder() => _options = LyoJsonSerializerOptions.Create();

    /// <summary>Clones <paramref name="baseline" /> rather than starting from Lyo defaults.</summary>
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

    /// <summary>Returns a fresh <see cref="JsonSerializerOptions" /> copy of the built configuration.</summary>
    public JsonSerializerOptions Build() => new(_options);
}