using System.Reflection;
using System.Text.Json;
using Lyo.Common.Core;
using Lyo.Exceptions;
using Lyo.Job.Models.Response;

namespace Lyo.Job.Models.Context;

/// <summary>
/// The template context every job stage resolves against: the scheduler when it builds a run request, the worker when it re-formats string parameters, the workflow engine
/// when it starts a step, and the parameter editor when it advertises tokens. One type with one set of seeders, so the four cannot drift apart.
/// <para>
/// Nested data is stored under dotted keys (<c>LastRun.Result.RowCount</c>), which <c>IFormatterService</c> resolves both as a whole key and segment by segment, so
/// <c>{LastRun.Result.RowCount}</c> works.
/// </para>
/// <para>
/// Inherited from <see cref="ContextBag" />: not thread-safe, and intended for one execution. Build a fresh bag per run creation and per step start rather than caching one
/// on a scheduler or engine, both of which process messages concurrently.
/// </para>
/// </summary>
public sealed class JobContextBag : ContextBag
{
    /// <summary>Key holding the step a scope was created for, so a step template can read its own name and definition.</summary>
    public const string CurrentStepKey = "CurrentStep";

    private static readonly BindingFlags RunPropertyFlags = BindingFlags.Public | BindingFlags.Instance;

    /// <summary>Seeds <c>Definition</c>.</summary>
    /// <returns>This bag.</returns>
    public JobContextBag WithDefinition(JobDefinitionRes? definition)
    {
        Set("Definition", definition);
        return this;
    }

    /// <summary>Seeds <c>LastRun</c>, <c>LastSuccessfulRun</c>, and <c>LastFailedRun</c>, each with its results and parameters as dotted entries.</summary>
    /// <returns>This bag.</returns>
    public JobContextBag WithLastRuns(JobRunRes? last, JobRunRes? lastSuccessful, JobRunRes? lastFailed)
    {
        SetRun("LastRun", last);
        SetRun("LastSuccessfulRun", lastSuccessful);
        SetRun("LastFailedRun", lastFailed);
        return this;
    }

    /// <summary>Seeds <c>Schedule</c> plus <c>Schedule.Parameter.{key}</c> for each enabled schedule parameter. Disabled parameters are left out, matching what the run gets.</summary>
    /// <returns>This bag.</returns>
    public JobContextBag WithSchedule(JobScheduleRes? schedule)
    {
        Set("Schedule", schedule);
        foreach (var parameter in schedule?.Parameters ?? []) {
            if (parameter.Enabled)
                SetNested("Schedule", "Parameter", parameter.Key, parameter.Value);
        }

        return this;
    }

    /// <summary>Seeds <c>Workflow</c> and <c>WorkflowRun</c> so a step template can read the workflow it belongs to.</summary>
    /// <returns>This bag.</returns>
    public JobContextBag WithWorkflow(JobWorkflowRes? workflow, JobWorkflowRunRes? workflowRun)
    {
        Set("Workflow", workflow);
        Set("WorkflowRun", workflowRun);
        return this;
    }

    /// <summary>
    /// Seeds <c>Input.{key}</c> from a workflow run's stored inputs, the flat string map the start call resolved from the caller's inputs over the schedule's enabled
    /// parameters. This is what a step condition reads to branch on something the run was started with rather than on an upstream result.
    /// </summary>
    /// <param name="inputsJson">Serialized <c>Dictionary&lt;string, string&gt;</c>, or null for a run with no inputs.</param>
    /// <returns>This bag.</returns>
    /// <remarks>Unreadable JSON seeds nothing rather than throwing: a hand-edited column must not stop the engine from starting the run's steps.</remarks>
    public JobContextBag WithInputs(string? inputsJson)
    {
        foreach (var input in ParseInputs(inputsJson))
            Set($"Input.{input.Key}", input.Value);

        return this;
    }

    /// <summary>
    /// Seeds the worker's <c>jobrun</c> bag: every readable <see cref="JobRunRes" /> property except the parameter list, plus a <c>Parameters</c> map keyed by parameter name.
    /// That map is what <c>{jobrun.Parameters.EmailTo}</c> reads.
    /// </summary>
    /// <param name="run">Run being executed.</param>
    /// <param name="coerce">Converts a stored parameter to the value templates should see. Null stores the raw string.</param>
    /// <returns>This bag.</returns>
    public JobContextBag WithRun(JobRunRes run, Func<JobRunParameterRes, object?>? coerce = null)
    {
        ArgumentHelpers.ThrowIfNull(run);
        Set("jobrun", BuildJobRunMap(run, coerce));
        return this;
    }

    /// <summary>Replaces the <c>Parameters</c> map inside an already-seeded <c>jobrun</c> entry. Used by the worker after a formatting pass rewrites parameter values.</summary>
    public void RefreshRunParameters(JobRunRes run, Func<JobRunParameterRes, object?>? coerce = null)
    {
        ArgumentHelpers.ThrowIfNull(run);
        if (TryGetValue("jobrun", out var existing) && existing is Dictionary<string, object?> map)
            map["Parameters"] = BuildParameterMap(run, coerce);
    }

    /// <summary>
    /// Seeds a completed upstream run under <c>Step.{stepName}</c>, with its results and parameters as dotted entries, so a downstream step can template against
    /// <c>{Step.Extract.Result.RowCount}</c>.
    /// </summary>
    /// <returns>This bag.</returns>
    public JobContextBag WithUpstreamRun(string stepName, JobRunRes? run)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(stepName);
        SetRun($"Step.{stepName}", run);
        return this;
    }

    /// <summary>
    /// An isolated copy for one workflow step, naming the step under <see cref="CurrentStepKey" />. Sibling steps of a DAG run concurrently and must not see each other's
    /// upstream seeds, so each gets its own bag.
    /// </summary>
    /// <remarks>
    /// A copy rather than <see cref="ContextBag{TKey}.CreateScope" />: the formatter imports only a bag's own entries, so handing it a child scope would lose everything the
    /// run-level bag seeded.
    /// </remarks>
    public JobContextBag CreateStepScope(JobWorkflowStepRes step)
    {
        ArgumentHelpers.ThrowIfNull(step);
        var scope = Copy();
        scope.Set(CurrentStepKey, step);
        return scope;
    }

    /// <summary>
    /// An isolated copy holding everything one workflow step resolves against: the run-level seeds already on this bag, the step itself, its job definition, and each
    /// dependency's completed run keyed by that dependency's step name. Both the engine (when it starts the step) and the editor (when it advertises tokens) call this,
    /// so the paths a template or a condition gate can read are defined in one place.
    /// </summary>
    /// <param name="step">Step being scoped. Readable from the scope as <see cref="CurrentStepKey" />.</param>
    /// <param name="definition">Job definition the step runs, or null when it could not be loaded.</param>
    /// <param name="upstream">Each dependency's step name with its completed run, or a null run for one that has not finished.</param>
    /// <returns>The new scope. This bag is left untouched.</returns>
    public JobContextBag WithWorkflowStep(JobWorkflowStepRes step, JobDefinitionRes? definition, IEnumerable<(string StepName, JobRunRes? Run)> upstream)
    {
        ArgumentHelpers.ThrowIfNull(upstream);
        var scope = CreateStepScope(step);
        scope.WithDefinition(definition);
        foreach (var (stepName, run) in upstream)
            scope.WithUpstreamRun(stepName, run);

        return scope;
    }

    /// <inheritdoc cref="ContextBag{TKey}.Copy" />
    public new JobContextBag Copy() => (JobContextBag)base.Copy();

    /// <inheritdoc cref="ContextBag{TKey}.Freeze" />
    public new JobContextBag Freeze() => (JobContextBag)base.Freeze();

    /// <inheritdoc />
    protected override ContextBag<string> CreateShell() => new JobContextBag();

    /// <summary>Stores <paramref name="run" /> under <paramref name="name" />, then its results and parameters as <c>{name}.Result.{key}</c> and <c>{name}.Parameter.{key}</c>.</summary>
    private void SetRun(string name, JobRunRes? run)
    {
        Set(name, run);
        if (run is null)
            return;

        foreach (var result in run.GetResultDictionary())
            SetNested(name, "Result", result.Key, result.Value);

        foreach (var parameter in run.GetParameterDictionary())
            SetNested(name, "Parameter", parameter.Key, parameter.Value);
    }

    /// <summary>Stores <c>{prefix}.{section}.{key}</c>.</summary>
    private void SetNested(string prefix, string section, string key, object? value)
        => Set($"{prefix}.{section}.{key}", value);

    private static Dictionary<string, string?> ParseInputs(string? inputsJson)
    {
        if (string.IsNullOrWhiteSpace(inputsJson))
            return [];

        try {
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(inputsJson!) ?? [];
        }
        catch (JsonException) {
            return [];
        }
    }

    private static Dictionary<string, object?> BuildJobRunMap(JobRunRes run, Func<JobRunParameterRes, object?>? coerce)
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in typeof(JobRunRes).GetProperties(RunPropertyFlags)) {
            if (!property.CanRead || property.GetIndexParameters().Length > 0 || string.Equals(property.Name, nameof(JobRunRes.JobRunParameters), StringComparison.Ordinal))
                continue;

            try {
                map[property.Name] = property.GetValue(run);
            }
            catch (TargetInvocationException) {
                // A getter that throws contributes no token rather than failing the whole seed.
            }
        }

        map["Parameters"] = BuildParameterMap(run, coerce);
        return map;
    }

    private static Dictionary<string, object?> BuildParameterMap(JobRunRes run, Func<JobRunParameterRes, object?>? coerce)
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in run.JobRunParameters ?? [])
            map[parameter.Key] = coerce is null ? parameter.Value : coerce(parameter);

        return map;
    }
}
