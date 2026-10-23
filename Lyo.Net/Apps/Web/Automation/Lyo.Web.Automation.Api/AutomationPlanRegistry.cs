using System.Text.Json;
using Lyo.Common.Core.Identifiers;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Plan;

namespace Lyo.Web.Automation.Api;

/// <summary>In-memory plans for the automation API. They disappear when the process stops.</summary>
public sealed class AutomationPlanRegistry : IAutomationPlanStore
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };

    private readonly Lock _gate = new();
    private readonly Dictionary<string, Stored> _plans = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public IReadOnlyList<AutomationPlanSummary> List()
    {
        lock (_gate)
            return _plans.Values.Select(item => new AutomationPlanSummary { Id = item.Id, Name = item.Plan.Name ?? "" })
                .OrderBy(item => item.Name, StringComparer.Ordinal)
                .ToArray();
    }

    /// <inheritdoc />
    public AutomationPlan? GetByName(string name)
    {
        lock (_gate)
            return _plans.TryGetValue(name, out var stored) ? stored.Plan : null;
    }

    /// <inheritdoc />
    public AutomationPlan? GetById(Guid id)
    {
        lock (_gate)
            return _plans.Values.FirstOrDefault(item => item.Id == id)?.Plan;
    }

    /// <inheritdoc />
    public AutomationPlan Create(string name)
    {
        lock (_gate) {
            if (_plans.TryGetValue(name, out var existing))
                return existing.Plan;

            var plan = new AutomationPlan(name, []);
            _plans[name] = new(LyoGuid.CreateV7(), plan);
            return plan;
        }
    }

    /// <inheritdoc />
    public bool TryReplace(string name, AutomationPlan plan)
    {
        lock (_gate) {
            if (!_plans.TryGetValue(name, out var existing))
                return false;

            _plans[name] = existing with { Plan = plan with { Name = name } };
            return true;
        }
    }

    /// <summary>The plan named <paramref name="name" />, or null when it was never created.</summary>
    public AutomationPlan? Get(string name) => GetByName(name);

    /// <summary>Parses plan JSON. Invalid JSON and a missing <c>steps</c> array return null.</summary>
    public static AutomationPlan? Parse(string json)
    {
        try {
            var plan = JsonSerializer.Deserialize<AutomationPlan>(json, Json);
            return plan?.Steps is null ? null : plan;
        }
        catch (JsonException) {
            return null;
        }
    }

    private sealed record Stored(Guid Id, AutomationPlan Plan);
}