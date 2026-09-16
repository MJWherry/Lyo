using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Exceptions;
using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Playwright.Record;

/// <summary>Camel-case JSON for an <see cref="AutomationPlan" />, including the <c>type</c> step discriminator.</summary>
public static class AutomationPlanJson
{
    /// <summary>Serializer settings the recorder writes and the panel Apply action reads.</summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    /// <summary>Writes <paramref name="plan" /> indented, with camel-case names and string enums.</summary>
    public static string Serialize(AutomationPlan plan)
    {
        ArgumentHelpers.ThrowIfNull(plan);
        return JsonSerializer.Serialize(plan, Options);
    }

    /// <summary>Reads a plan produced by <see cref="Serialize" /> or typed into the panel.</summary>
    public static AutomationPlan Deserialize(string json)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(json);
        var plan = JsonSerializer.Deserialize<AutomationPlan>(json, Options);
        if (plan?.Steps == null)
            throw new JsonException("Plan has no steps.");

        return plan;
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
