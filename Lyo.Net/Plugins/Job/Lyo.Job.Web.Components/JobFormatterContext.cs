using Lyo.Job.Models.Context;
using Lyo.Job.Models.Response;
using Lyo.Web.Components;
using Lyo.Web.Components.LyoType;
using Lyo.Web.Primitives;

namespace Lyo.Job.Web.Components;

/// <summary>
/// Builds the sample context a formatter-typed job parameter can use, so the template editor offers the same keys the scheduler and worker will resolve at run time.
/// </summary>
/// <remarks>
/// Two runtime stages contribute keys, and an author needs both: the scheduler formats definition, schedule, and trigger parameter values when it creates a run, and the
/// worker then re-formats string parameters against a <c>jobrun</c> bag plus anything it binds with <c>AddContext</c>.
/// <para>
/// All three stages build a <see cref="JobContextBag" /> from the same seeders, so the editor cannot drift from what resolves. <c>JobFormatterContextTests</c> asserts that.
/// </para>
/// </remarks>
public static class JobFormatterContext
{
    private static readonly Lazy<Dictionary<string, object?>> SampleRun = new(() => LyoSampleShape.FromType(typeof(JobRunRes)));

    /// <summary>
    /// Context for one definition. Pass <paramref name="latestRuns" /> (from <c>POST {DefinitionRoute}/LatestRuns</c>) so previews show real values; without it the
    /// keys still resolve, against a reflection-built sample of <see cref="JobRunRes" />.
    /// </summary>
    /// <param name="definition">Definition being edited. Parameters and schedules on it supply the flattened per-key entries.</param>
    /// <param name="latestRuns">Last, last successful, and last failed run, matching the snapshots the scheduler holds. Null when the definition has never run.</param>
    /// <param name="schedule">Selected schedule, when the caller is the schedule editor rather than the definition editor.</param>
    /// <returns>A case-insensitive bag suitable for <c>FormatterContext</c>.</returns>
    public static JobContextBag Build(JobDefinitionRes? definition, JobDefinitionLatestRunsRes? latestRuns = null, JobScheduleRes? schedule = null)
    {
        var lastRun = latestRuns?.LastRun;
        var bag = new JobContextBag().WithDefinition(definition)
            .WithLastRuns(lastRun, latestRuns?.LastSuccessfulRun, latestRuns?.LastFailedRun)
            .WithSchedule(schedule ?? definition?.JobSchedules?.FirstOrDefault());

        // A definition that has never run would seed null run entries and offer no autocomplete on run paths at all. The reflection-built placeholder graph keeps them visible.
        SubstituteSample(bag, "LastRun", lastRun);
        SubstituteSample(bag, "LastSuccessfulRun", latestRuns?.LastSuccessfulRun);
        SubstituteSample(bag, "LastFailedRun", latestRuns?.LastFailedRun);

        if (lastRun is not null)
            bag.WithRun(lastRun, p => p.Value);
        else
            bag.Set("jobrun", SampleJobRunMap());

        AddDefinitionParameterFallbacks(bag, definition);
        return bag;
    }

    private static void SubstituteSample(JobContextBag bag, string name, JobRunRes? run)
    {
        if (run is null)
            bag.Set(name, SampleRun.Value);
    }

    /// <summary>Definition keys with no run value still belong in the <c>jobrun.Parameters</c> map: a template may reference a parameter that has never been provided.</summary>
    private static void AddDefinitionParameterFallbacks(JobContextBag bag, JobDefinitionRes? definition)
    {
        if (bag["jobrun"] is not Dictionary<string, object?> jobRun || jobRun["Parameters"] is not Dictionary<string, object?> parameters)
            return;

        foreach (var p in definition?.JobParameters ?? [])
            parameters.TryAdd(p.Key, p.Value);
    }

    /// <summary>The <c>jobrun</c> shape for a definition that has never run: the <see cref="JobRunRes" /> sample graph, minus the parameter list, plus an empty parameter map.</summary>
    private static Dictionary<string, object?> SampleJobRunMap()
    {
        var map = new Dictionary<string, object?>(SampleRun.Value, StringComparer.OrdinalIgnoreCase);
        map.Remove(nameof(JobRunRes.JobRunParameters));
        map["Parameters"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        return map;
    }
}
