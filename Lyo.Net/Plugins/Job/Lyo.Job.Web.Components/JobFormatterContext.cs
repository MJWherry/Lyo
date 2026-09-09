using System.Reflection;
using Lyo.Job.Models.Response;
using Lyo.Web.Components;
using Lyo.Web.Components.LyoType;
using Lyo.Web.Primitives;

namespace Lyo.Job.Web.Components;

/// <summary>
/// Builds the sample context a formatter-typed job parameter can use, so the template editor offers the same keys the scheduler and worker will resolve at run time.
/// </summary>
/// <remarks>
/// Two runtime stages contribute keys, and an author needs both. The scheduler formats definition, schedule, and trigger parameter values when it creates a run,
/// against the keys in <c>JobScheduler.BuildTemplateData</c>. The worker then re-formats string parameters against a <c>jobrun</c> bag seeded in
/// <c>JobWorkerParameterFormatter.SeedJobRun</c>, plus anything it binds with <c>AddContext</c>, which is what the host's example context covers.
/// <para>
/// This mirrors both, so it has to stay in step with them. <c>JobFormatterContextTests</c> asserts the key parity to catch drift.
/// </para>
/// </remarks>
public static class JobFormatterContext
{
    private static readonly Lazy<Dictionary<string, object?>> SampleRun = new(() => LyoSampleShape.FromType(typeof(JobRunRes)));

    /// <summary>
    /// Context for one definition. Pass <paramref name="latestRuns" /> (from <c>POST {DefinitionRoute}/LatestRuns</c>) so previews show real values; without it the
    /// keys still resolve, against a reflection-built sample of <see cref="JobRunRes" />.
    /// </summary>
    /// <param name="definition">Definition being edited. Parameters, schedules, and triggers on it supply the flattened per-key entries.</param>
    /// <param name="latestRuns">Last, last successful, and last failed run, matching the snapshots the scheduler holds. Null when the definition has never run.</param>
    /// <param name="schedule">Selected schedule, when the caller is the schedule editor rather than the definition editor.</param>
    /// <returns>A case-insensitive dictionary suitable for <c>FormatterContext</c>.</returns>
    public static Dictionary<string, object?> Build(JobDefinitionRes? definition, JobDefinitionLatestRunsRes? latestRuns = null, JobScheduleRes? schedule = null)
    {
        var trigger = definition?.JobTriggers?.FirstOrDefault();
        var effectiveSchedule = schedule ?? definition?.JobSchedules?.FirstOrDefault();
        var lastRun = latestRuns?.LastRun;

        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
            ["Definition"] = definition,
            ["LastRun"] = RunOrSample(lastRun),
            ["LastSuccessfulRun"] = RunOrSample(latestRuns?.LastSuccessfulRun),
            ["LastFailedRun"] = RunOrSample(latestRuns?.LastFailedRun),
            ["Trigger"] = trigger,
            // The scheduler only fills TriggeredByRun for trigger-created runs, which the editor cannot know ahead of time; the last run stands in so the paths resolve.
            ["TriggeredByRun"] = RunOrSample(lastRun),
            ["Schedule"] = effectiveSchedule
        };

        AddRunEntries(data, "LastRun", lastRun);
        AddRunEntries(data, "LastSuccessfulRun", latestRuns?.LastSuccessfulRun);
        AddRunEntries(data, "LastFailedRun", latestRuns?.LastFailedRun);
        AddRunEntries(data, "TriggeredByRun", lastRun);

        foreach (var p in effectiveSchedule?.Parameters ?? []) {
            if (p.Enabled)
                data[$"Schedule_Parameter_{p.Key}"] = p.Value;
        }

        foreach (var p in trigger?.TriggerParameters ?? [])
            data[$"Trigger_Parameter_{p.Key}"] = p.Value;

        data["jobrun"] = BuildJobRunBag(definition, lastRun);
        return data;
    }

    /// <summary>A real run when one exists, otherwise the placeholder graph so run paths still appear in autocomplete.</summary>
    private static object RunOrSample(JobRunRes? run) => run ?? (object)SampleRun.Value;

    /// <summary>Mirrors <c>JobScheduler.AddRunTemplateData</c>: each run result and parameter flattened as <c>{prefix}_Result_{key}</c> /
    /// <c>{prefix}_Parameter_{key}</c>.</summary>
    private static void AddRunEntries(Dictionary<string, object?> data, string prefix, JobRunRes? run)
    {
        if (run is null)
            return;

        foreach (var (key, value) in run.GetResultDictionary())
            data[$"{prefix}_Result_{key}"] = value;

        foreach (var (key, value) in run.GetParameterDictionary())
            data[$"{prefix}_Parameter_{key}"] = value;
    }

    /// <summary>
    /// Mirrors <c>JobWorkerParameterFormatter.SeedJobRun</c>: every <see cref="JobRunRes" /> property except the parameter list, plus a <c>Parameters</c> map keyed by
    /// parameter name. That map is what <c>{jobrun.parameters.emailTo}</c> resolves against.
    /// </summary>
    private static Dictionary<string, object?> BuildJobRunBag(JobDefinitionRes? definition, JobRunRes? lastRun)
    {
        var bag = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in typeof(JobRunRes).GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0 || string.Equals(prop.Name, nameof(JobRunRes.JobRunParameters), StringComparison.Ordinal))
                continue;

            bag[prop.Name] = ReadProperty(prop, lastRun);
        }

        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in lastRun?.JobRunParameters ?? [])
            parameters[p.Key] = p.Value;

        // Definition keys with no run value still belong in the map: a template may reference a parameter that has never been provided.
        foreach (var p in definition?.JobParameters ?? [])
            parameters.TryAdd(p.Key, p.Value);

        bag["Parameters"] = parameters;
        return bag;
    }

    private static object? ReadProperty(PropertyInfo prop, JobRunRes? run)
    {
        if (run is null)
            return SampleRun.Value.GetValueOrDefault(prop.Name);

        try {
            return prop.GetValue(run);
        }
        catch {
            return null;
        }
    }
}
