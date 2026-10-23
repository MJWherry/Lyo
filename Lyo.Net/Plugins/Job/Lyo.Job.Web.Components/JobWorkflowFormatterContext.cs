using Lyo.Exceptions;
using Lyo.Formatter.Web.Components;
using Lyo.Job.Models.Context;
using Lyo.Web.Components.LyoType;

namespace Lyo.Job.Web.Components;

/// <summary>
/// Builds the sample context one workflow step resolves against, so the editor offers the same paths the engine will actually seed — both for parameter templates and for the
/// condition gate's field picker.
/// </summary>
/// <remarks>
/// The runtime side is <c>JobWorkflowEngine.BuildStepScope</c>, and both go through <see cref="JobContextBag.WithWorkflowStep" />. <c>JobWorkflowFormatterContextTests</c>
/// asserts the editor covers every key the engine seeds, so a change to one cannot leave the other advertising paths that resolve to nothing.
/// </remarks>
public static class JobWorkflowFormatterContext
{
    /// <summary>
    /// One level only, unlike the definition editor's three. A gate or template reads an upstream step's own fields (<c>State</c>, <c>Result</c>, timestamps), never the job
    /// definition behind it or that definition's schedules.
    /// </summary>
    private const int UpstreamSampleDepth = 1;

    /// <summary>Paths a step yields before any dependency: the clock rows, the workflow, the step itself, and its job definition.</summary>
    private const int PathBudgetBase = 96;

    /// <summary>Added per dependency. A <see cref="JobRunRes" /> placeholder walks to roughly this many paths once timestamps expand into their calendar components.</summary>
    private const int PathBudgetPerDependency = 96;

    private const string StepPrefix = "Step.";

    private static readonly Lazy<Dictionary<string, object?>> SampleRun = new(() => LyoSampleShape.FromType(typeof(JobRunRes), UpstreamSampleDepth));

    /// <summary>Context for one step of a workflow being authored.</summary>
    /// <param name="step">Step being edited.</param>
    /// <param name="definition">Job definition the step runs, or null when none is picked yet.</param>
    /// <param name="workflow">Workflow the step belongs to, supplying the <c>Workflow.*</c> paths.</param>
    /// <param name="inputKeys">Run input keys the workbench declares, seeded as <c>Input.{key}</c>.</param>
    /// <param name="dependencies">Each dependency's step name with the definition it runs. Both are needed to offer that step's <c>Step.{name}.*</c> paths before it has run.</param>
    /// <returns>A case-insensitive bag suitable for <c>FormatterContext</c> and for path autocomplete.</returns>
    public static JobContextBag Build(
        JobWorkflowStepRes step,
        JobDefinitionRes? definition,
        JobWorkflowRes? workflow,
        IEnumerable<string> inputKeys,
        IEnumerable<(string StepName, JobDefinitionRes? Definition)> dependencies)
    {
        ArgumentHelpers.ThrowIfNull(inputKeys);
        ArgumentHelpers.ThrowIfNull(dependencies);
        var runScope = new JobContextBag().WithWorkflow(workflow, null);
        foreach (var key in inputKeys.Where(k => !string.IsNullOrWhiteSpace(k)))
            runScope.Set($"Input.{key}", "");

        var upstream = dependencies.Where(d => !string.IsNullOrWhiteSpace(d.StepName)).ToList();
        var scope = runScope.WithWorkflowStep(step, definition, upstream.Select(d => (d.StepName, (JobRunRes?)null)));
        foreach (var (stepName, dependencyDefinition) in upstream)
            SeedUpstreamSample(scope, stepName, dependencyDefinition);

        return scope;
    }

    /// <summary>
    /// Authoring happens before anything has run, so a null upstream would offer no autocomplete on its run paths at all. The reflection-built placeholder keeps them visible
    /// (matching what <c>JobFormatterContext</c> does for a definition that has never run), and the dependency's declared parameter keys fill in the one nested section that is knowable.
    /// Result keys are not: only the worker decides those.
    /// </summary>
    private static void SeedUpstreamSample(JobContextBag scope, string stepName, JobDefinitionRes? definition)
    {
        scope.Set($"Step.{stepName}", SampleRun.Value);
        foreach (var parameter in definition?.JobParameters ?? [])
            scope.Set($"Step.{stepName}.Parameter.{parameter.Key}", parameter.Value ?? "");
    }

    /// <summary>
    /// Selectable leaf paths from a context built here, for the condition gate's field list. Sized to the number of dependencies so the walk is not cut off partway through the
    /// last one: <see cref="LyoFormatterContextCatalog.MaxItems" /> is tuned for a session context of a few keys, and a step with two upstreams is well past it.
    /// </summary>
    public static List<string> Paths(JobContextBag context)
    {
        ArgumentHelpers.ThrowIfNull(context);
        var budget = PathBudgetBase + DependencyCount(context) * PathBudgetPerDependency;
        return [.. LyoFormatterContextCatalog.Build(context, budget).Where(e => !e.HasChildren).Select(e => e.Path)];
    }

    /// <summary>Counts <c>Step.{name}</c> roots, ignoring the flat <c>Step.{name}.Parameter.{key}</c> entries seeded alongside them.</summary>
    private static int DependencyCount(JobContextBag context)
        => context.Keys.Count(k => k.StartsWith(StepPrefix, StringComparison.OrdinalIgnoreCase) && k.IndexOf('.', StepPrefix.Length) < 0);
}