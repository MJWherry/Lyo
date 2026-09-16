using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Job.Models.Builders;
using Lyo.Query.Models.Common;

namespace Lyo.Job.Web.Components;

/// <summary>
/// Keeps a workflow draft's links pointed at step ids. The editor renames steps constantly; a dependency stored as a name would dangle, and a condition or a parameter
/// template that reads <c>Step.{name}.*</c> has to be rewritten in the same edit because that is the path the engine seeds.
/// </summary>
public static class JobWorkflowDraftLinks
{
    /// <summary>
    /// Renames <paramref name="step"/> and rewrites every <c>Step.{old}.</c> path in the draft. Dependency lists are ids, so they are left alone.
    /// </summary>
    public static void Rename(IReadOnlyList<JobWorkflowStepDraft> steps, JobWorkflowStepDraft step, string newName)
    {
        ArgumentHelpers.ThrowIfNull(steps);
        ArgumentHelpers.ThrowIfNull(step);
        ArgumentHelpers.ThrowIfNull(newName);

        var oldName = step.StepName;
        step.StepName = newName;
        if (string.IsNullOrEmpty(oldName) || string.Equals(oldName, newName, StringComparison.Ordinal))
            return;

        var from = $"{JobWorkflowStepCondition.StepPathPrefix}{oldName}.";
        var to = $"{JobWorkflowStepCondition.StepPathPrefix}{newName}.";
        foreach (var other in steps) {
            other.Condition = Rewrite(other.Condition, from, to);
            foreach (var key in other.Parameters.Keys.ToList())
                other.Parameters[key] = Replace(other.Parameters[key], from, to);
        }
    }

    /// <summary>Step names for <paramref name="step"/>'s dependencies, in list order, for <see cref="JobWorkflowBuilder.AddStep"/>.</summary>
    public static string[] DependencyNames(IReadOnlyList<JobWorkflowStepDraft> steps, JobWorkflowStepDraft step)
    {
        ArgumentHelpers.ThrowIfNull(steps);
        ArgumentHelpers.ThrowIfNull(step);
        var byId = steps.ToDictionary(s => s.Id);
        return [.. step.DependsOn.Where(byId.ContainsKey).Select(id => byId[id].StepName)];
    }

    /// <summary>
    /// Reads a persisted workbench draft. <c>DependsOn</c> may be draft ids or, for a draft saved before links were ids, step names. Names are resolved once.
    /// </summary>
    public static JobWorkflowWorkbenchState Load(string json, JsonSerializerOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(json);
        var opted = new JsonSerializerOptions(options ?? JsonSerializerOptions.Default);
        opted.Converters.Add(new JobWorkflowStepDraftConverter());
        var state = JsonSerializer.Deserialize<JobWorkflowWorkbenchState>(json, opted) ?? new JobWorkflowWorkbenchState();
        Repair(state);
        return state;
    }

    /// <summary>Assigns an id to any step missing one and turns pending dependency names into those ids.</summary>
    public static void Repair(JobWorkflowWorkbenchState state)
    {
        ArgumentHelpers.ThrowIfNull(state);
        foreach (var step in state.Steps) {
            if (step.Id == Guid.Empty)
                step.Id = LyoGuid.CreateV7();
        }

        var byName = state.Steps.Where(s => !string.IsNullOrWhiteSpace(s.StepName))
            .GroupBy(s => s.StepName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var step in state.Steps) {
            foreach (var raw in step.PendingDependsOn) {
                if (Guid.TryParse(raw, out var id) && id != Guid.Empty)
                    step.DependsOn.Add(id);
                else if (byName.TryGetValue(raw, out var named))
                    step.DependsOn.Add(named.Id);
            }

            step.PendingDependsOn.Clear();
        }

        if (state.SelectedStepId == Guid.Empty && state.SelectedStepName is { Length: > 0 } selected && byName.TryGetValue(selected, out var match))
            state.SelectedStepId = match.Id;
    }

    private static WhereClause? Rewrite(WhereClause? clause, string from, string to)
    {
        switch (clause) {
            case null:
                return null;
            case GroupClause group:
                group.Children = [.. group.Children.Select(child => Rewrite(child, from, to)).OfType<WhereClause>()];
                break;
            case ConditionClause condition:
                condition.Field = Replace(condition.Field, from, to);
                break;
        }

        clause.SubClause = Rewrite(clause.SubClause, from, to);
        return clause;
    }

    /// <summary>Replaces every case-insensitive occurrence of <paramref name="from"/>. A step rename must catch <c>Step.extract.</c> as well as <c>Step.Extract.</c>.</summary>
    private static string Replace(string? text, string from, string to)
    {
        if (string.IsNullOrEmpty(text) || from.Length == 0)
            return text ?? "";

        var result = new StringBuilder(text.Length);
        var index = 0;
        while (index < text.Length) {
            var found = text.IndexOf(from, index, StringComparison.OrdinalIgnoreCase);
            if (found < 0) {
                result.Append(text, index, text.Length - index);
                break;
            }

            result.Append(text, index, found - index);
            result.Append(to);
            index = found + from.Length;
        }

        return result.ToString();
    }

    /// <summary>
    /// Reads one step while holding <c>DependsOn</c> aside. A guid entry is a current link; anything else is a step name from a draft saved before links were ids, and
    /// <see cref="Repair"/> resolves it once the whole list is loaded.
    /// </summary>
    private sealed class JobWorkflowStepDraftConverter : JsonConverter<JobWorkflowStepDraft>
    {
        public override JobWorkflowStepDraft Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            List<string> pending = [];
            if ((TryProperty(root, "dependsOn", out var depends) || TryProperty(root, "DependsOn", out depends)) && depends.ValueKind == JsonValueKind.Array) {
                foreach (var item in depends.EnumerateArray()) {
                    if (item.ValueKind == JsonValueKind.String && item.GetString() is { Length: > 0 } text)
                        pending.Add(text);
                }
            }

            var step = JsonSerializer.Deserialize<JobWorkflowStepDraft>(Strip(root), WithoutThis(options)) ?? new JobWorkflowStepDraft();
            step.PendingDependsOn = pending;
            return step;
        }

        public override void Write(Utf8JsonWriter writer, JobWorkflowStepDraft value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, value, WithoutThis(options));

        private static bool TryProperty(JsonElement root, string name, out JsonElement value)
        {
            foreach (var property in root.EnumerateObject()) {
                if (property.NameEquals(name)) {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static string Strip(JsonElement root)
        {
            using var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer)) {
                writer.WriteStartObject();
                foreach (var property in root.EnumerateObject()) {
                    if (property.NameEquals("dependsOn") || property.NameEquals("DependsOn"))
                        continue;

                    property.WriteTo(writer);
                }

                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(buffer.ToArray());
        }

        private static JsonSerializerOptions WithoutThis(JsonSerializerOptions options)
        {
            var copy = new JsonSerializerOptions(options);
            for (var i = copy.Converters.Count - 1; i >= 0; i--) {
                if (copy.Converters[i] is JobWorkflowStepDraftConverter)
                    copy.Converters.RemoveAt(i);
            }

            return copy;
        }
    }
}
