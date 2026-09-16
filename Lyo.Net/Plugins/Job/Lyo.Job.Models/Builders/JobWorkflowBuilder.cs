using System.Text.Json;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Query.Models.Common;

namespace Lyo.Job.Models.Builders;

/// <summary>
/// Builds a whole workflow — steps, dependencies, conditions, parameters, and schedules — as one <see cref="JobWorkflowReq" /> that a single POST can create.
/// <para>
/// Step ids are minted client-side, so dependencies and conditions are written by <b>step name</b> and resolved to those ids in <see cref="Build" />. Without that a caller
/// has to create the workflow, read back the server-assigned ids, and PATCH each step's <c>DependsOnStepIds</c> — three round trips to express one graph.
/// </para>
/// <para>
/// <see cref="Build" /> is the validation gate: unique names, known condition and dependency names, a gate that only reads steps it waits for, at least one enabled step,
/// and an acyclic graph. Each of those would otherwise surface much later as a workflow run that fails or hangs, with nothing pointing back at the typo that caused it.
/// </para>
/// </summary>
/// <example>
/// <code>
/// var req = JobWorkflowBuilder.New("Nightly ETL")
///     .AddStep("Extract", extractDefinitionId)
///     .AddStep("Load", loadDefinitionId, dependsOn: ["Extract"], parameters: new Dictionary&lt;string, string&gt; { ["Rows"] = "{Step.Extract.Result.RowCount}" })
///     .AddStep("Notify", notifyDefinitionId, dependsOn: ["Load"], condition: JobWorkflowStepCondition.All(
///         JobWorkflowStepCondition.When("Load", "RowCount", ComparisonOperatorEnum.GreaterThan, "0"),
///         JobWorkflowStepCondition.WhenPath("Input.Env", ComparisonOperatorEnum.Equals, "prod")))
///     .WithSchedule(new() { Type = ScheduleType.Cron, CronExpression = "0 2 * * *" })
///     .Build();
/// </code>
/// </example>
public class JobWorkflowBuilder
{
    private readonly List<(JobWorkflowStepReq Step, string[] DependsOn, WhereClause? Condition)> _steps = [];
    private readonly JobWorkflowReq _workflow = new();

    public JobWorkflowBuilder(string name, string? description = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        _workflow.Name = name;
        _workflow.Description = description;
    }

    /// <summary>Starts a builder for a workflow named <paramref name="name" />.</summary>
    public static JobWorkflowBuilder New(string name, string? description = null) => new(name, description);

    /// <summary>Sets whether the workflow may be started at all. A disabled workflow rejects every start.</summary>
    public JobWorkflowBuilder WithEnabled(bool enabled = true)
    {
        _workflow.Enabled = enabled;
        return this;
    }

    /// <summary>
    /// Adds a step. The step's id is minted now so later steps can depend on it by name.
    /// </summary>
    /// <param name="stepName">Name other steps reference. Must be unique within the workflow.</param>
    /// <param name="jobDefinitionId">Definition this step runs.</param>
    /// <param name="dependsOn">Names of steps that must finish first. Resolved to ids in <see cref="Build" />.</param>
    /// <param name="condition">Gate that decides whether this step runs at all. Null runs it unconditionally.</param>
    /// <param name="parameters">Values that override the definition's defaults, serialized to the flat object map the scheduler merges.</param>
    /// <param name="failurePolicy">Whether a failure here stops the workflow or lets dependents continue.</param>
    /// <param name="stepOrder">Tie-break order among steps that are ready at the same time. Defaults to insertion order.</param>
    /// <param name="enabled">Whether the step is materialized into runs at all.</param>
    /// <exception cref="ValidationException">A step with this name was already added.</exception>
    public JobWorkflowBuilder AddStep(
        string stepName,
        Guid jobDefinitionId,
        string[]? dependsOn = null,
        WhereClause? condition = null,
        IReadOnlyDictionary<string, string>? parameters = null,
        JobWorkflowFailurePolicy failurePolicy = JobWorkflowFailurePolicy.Stop,
        int? stepOrder = null,
        bool enabled = true)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(stepName);
        if (_steps.Any(s => string.Equals(s.Step.StepName, stepName, StringComparison.OrdinalIgnoreCase)))
            throw new ValidationException($"Workflow '{_workflow.Name}' already has a step named '{stepName}'. Step names must be unique to be referenceable.");

        var step = new JobWorkflowStepReq {
            Id = LyoGuid.CreateV7(),
            JobDefinitionId = jobDefinitionId,
            StepName = stepName,
            StepOrder = stepOrder ?? _steps.Count,
            FailurePolicy = failurePolicy,
            ParametersJson = SerializeParameters(parameters),
            Enabled = enabled
        };

        _steps.Add((step, dependsOn ?? [], condition));
        return this;
    }

    /// <summary>Attaches a workflow-owned schedule. Its <c>JobWorkflowId</c> is filled in server-side from the workflow it is nested under.</summary>
    public JobWorkflowBuilder WithSchedule(JobScheduleReq schedule)
    {
        ArgumentHelpers.ThrowIfNull(schedule);
        _workflow.CreateSchedules.Add(schedule);
        return this;
    }

    /// <summary>
    /// Resolves dependency and condition names to step ids and validates the result.
    /// </summary>
    /// <exception cref="ValidationException">
    /// A dependency or condition names a step that does not exist, no step is enabled, or the dependencies form a cycle.
    /// </exception>
    public JobWorkflowReq Build()
    {
        var byName = _steps.ToDictionary(s => s.Step.StepName, s => s.Step, StringComparer.OrdinalIgnoreCase);
        foreach (var (step, dependsOn, condition) in _steps) {
            step.DependsOnStepIds = dependsOn.Length == 0 ? null : string.Join(",", dependsOn.Select(name => Resolve(byName, step.StepName, name, "depends on")));
            if (condition is null)
                continue;

            ValidateConditionStepReferences(step.StepName, condition, byName, dependsOn);
            step.ConditionJson = JsonSerializer.Serialize(condition);
        }

        // A workflow with nothing enabled can never run: StartWorkflowAsync rejects it, so persisting it only defers the error.
        if (!_steps.Any(s => s.Step.Enabled))
            throw new ValidationException($"Workflow '{_workflow.Name}' has no enabled steps.");

        var cycle = JobWorkflowGraph.FindDependencyCycle(_steps.Select(s => new KeyValuePair<Guid, string?>(s.Step.Id, s.Step.DependsOnStepIds)));
        if (cycle.Count > 0) {
            var names = _steps.Where(s => cycle.Contains(s.Step.Id)).Select(s => s.Step.StepName);
            throw new ValidationException($"Workflow '{_workflow.Name}' has a circular step dependency involving: {string.Join(", ", names)}.");
        }

        _workflow.CreateSteps = [.. _steps.Select(s => s.Step)];
        return _workflow;
    }

    private Guid Resolve(IReadOnlyDictionary<string, JobWorkflowStepReq> byName, string stepName, string referencedName, string relation)
    {
        if (byName.TryGetValue(referencedName, out var referenced))
            return referenced.Id;

        throw new ValidationException($"Step '{stepName}' {relation} '{referencedName}', which is not a step of workflow '{_workflow.Name}'.");
    }

    /// <summary>
    /// Every <c>Step.{name}</c> path in a gate must name a known step that the gated step also waits for. The engine evaluates a gate as soon as the step's own dependencies
    /// are satisfied, so a gate reading a step it does not depend on would test a run that has not happened yet and skip the step for the wrong reason.
    /// </summary>
    /// <exception cref="ValidationException">A referenced step is unknown, or is not a declared dependency of the gated step.</exception>
    private void ValidateConditionStepReferences(string stepName, WhereClause condition, IReadOnlyDictionary<string, JobWorkflowStepReq> byName, string[] dependsOn)
    {
        foreach (var referencedName in CollectStepReferences(condition)) {
            Resolve(byName, stepName, referencedName, "is conditional on");
            if (!dependsOn.Contains(referencedName, StringComparer.OrdinalIgnoreCase))
                throw new ValidationException($"Step '{stepName}' is conditional on '{referencedName}' but does not depend on it, so the gate would be read before it runs.");
        }
    }

    /// <summary>Walks a clause tree and yields the distinct step names its <c>Step.{name}.</c> paths reference.</summary>
    private static IEnumerable<string> CollectStepReferences(WhereClause clause)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Collect(clause, names);
        return names;
    }

    private static void Collect(WhereClause? clause, HashSet<string> names)
    {
        switch (clause) {
            case null:
                return;
            case GroupClause group:
                foreach (var child in group.Children)
                    Collect(child, names);

                break;
            case ConditionClause condition when TryReadStepName(condition.Field, out var name):
                names.Add(name);
                break;
        }

        Collect(clause.SubClause, names);
    }

    private static bool TryReadStepName(string? field, out string name)
    {
        name = "";
        if (field is null || !field.StartsWith(JobWorkflowStepCondition.StepPathPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var rest = field.Substring(JobWorkflowStepCondition.StepPathPrefix.Length);
        var dot = rest.IndexOf('.');
        name = dot < 0 ? rest : rest.Substring(0, dot);
        return name.Length > 0;
    }

    /// <summary>Emits the flat object map the scheduler's step-parameter merge reads. Any other shape is ignored there, so the shape is part of the contract.</summary>
    private static string? SerializeParameters(IReadOnlyDictionary<string, string>? parameters)
        => parameters is null || parameters.Count == 0 ? null : JsonSerializer.Serialize(parameters);
}
