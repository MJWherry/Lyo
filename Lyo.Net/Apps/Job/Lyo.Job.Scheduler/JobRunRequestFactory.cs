using System.Text.RegularExpressions;
using Lyo.Common.Metadata.Records;
using Lyo.Formatter;
using Lyo.Job.Models;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Parameters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Lyo.Exceptions;

namespace Lyo.Job.Scheduler;

/// <summary>
/// Builds the <see cref="JobRunReq" /> the scheduler posts, including parameter merging and template expansion. Kept out of the scheduler because it touches no scheduler
/// state: given a <see cref="JobInfo" /> and the schedule or trigger that fired, it is a pure translation with the formatter as its only dependency.
/// </summary>
/// <param name="formatter">Formatter used for template values.</param>
/// <param name="createdBy">Value stamped on every run this factory builds.</param>
/// <param name="logger">Optional logger for unresolved templates and defaults.</param>
public sealed class JobRunRequestFactory(IFormatterService formatter, string createdBy, ILogger? logger = null)
{
    private static readonly Regex LegacyDoubleBracePlaceholderRegex = new(@"\{\{([^{}]+)\}\}", RegexOptions.Compiled);

    private readonly ILogger _logger = logger ?? NullLogger.Instance;

    /// <summary>
    /// Builds a run request for <paramref name="jobInfo" />. Definition parameters come first; schedule parameters replace same-key entries rather than appending, so the API never
    /// sees a duplicate key carrying an empty definition default next to the schedule value. Trigger parameters are appended.
    /// </summary>
    /// <param name="jobInfo">Definition and its recent runs, used as template data.</param>
    /// <param name="schedule">Schedule that fired, when this is a scheduled run.</param>
    /// <param name="trigger">Trigger that fired, when this is a triggered run.</param>
    /// <param name="triggeredBy">Completed run that fired the trigger.</param>
    public JobRunReq Build(JobInfo jobInfo, JobScheduleRes? schedule, JobTriggerRes? trigger, JobRunRes? triggeredBy)
    {
        ArgumentHelpers.ThrowIfNull(jobInfo);
        var req = new JobRunReq {
            JobDefinitionId = jobInfo.Definition.Id,
            JobScheduleId = schedule?.Id,
            JobTriggerId = trigger?.Id,
            TriggeredByJobRunId = triggeredBy?.Id,
            AllowTriggers = true,
            CreatedBy = createdBy,
            JobRunParameters = []
        };

        var templateData = BuildTemplateData(jobInfo, trigger, triggeredBy, schedule);
        foreach (var p in jobInfo.Definition.JobParameters ?? [])
            req.JobRunParameters.Add(CreateRunParameterFromDefinition(p, templateData));

        foreach (var sp in schedule?.Parameters ?? []) {
            if (!sp.Enabled)
                continue;

            var runParam = CreateRunParameterFromSchedule(sp, templateData);
            req.JobRunParameters.RemoveAll(existing => string.Equals(existing.Key, runParam.Key, StringComparison.OrdinalIgnoreCase));
            req.JobRunParameters.Add(runParam);
        }

        foreach (var tp in trigger?.TriggerParameters ?? [])
            req.JobRunParameters.Add(CreateRunParameterFromTrigger(tp, templateData));

        return req;
    }

    /// <summary>
    /// Formats String and Formatter parameter templates with <see cref="IFormatterService" />. Unresolved <c>{client...}</c> tokens stay in the value (<c>MaintainTokens</c>).
    /// Legacy <c>{{Definition.Name}}</c> is unwrapped to <c>{Definition.Name}</c> here only so existing schedules keep working. Json/Xml values are not formatted.
    /// </summary>
    /// <param name="template">Raw parameter value.</param>
    /// <param name="templateData">Data the template resolves against.</param>
    public string FormatTemplateValue(string template, Dictionary<string, object?> templateData)
    {
        if (string.IsNullOrEmpty(template) || template.IndexOf('{') < 0)
            return template;

        var normalized = UnwrapLegacyDoubleBracePlaceholders(template);
        try {
            return formatter.Format(normalized, templateData);
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Template format failed: {Template}", template);
            return template;
        }
    }

    /// <summary>
    /// The data every parameter template resolves against. Internal so tests can assert the template editor's advertised token list against
    /// what actually resolves.
    /// </summary>
    /// <param name="jobInfo">Definition and its recent runs.</param>
    /// <param name="trigger">Trigger that fired, if any.</param>
    /// <param name="triggeredBy">Run that fired the trigger, if any.</param>
    /// <param name="schedule">Schedule that fired, if any.</param>
    internal static Dictionary<string, object?> BuildTemplateData(JobInfo jobInfo, JobTriggerRes? trigger, JobRunRes? triggeredBy, JobScheduleRes? schedule)
    {
        var data = new Dictionary<string, object?> {
            ["Definition"] = jobInfo.Definition,
            ["LastRun"] = jobInfo.LastRun,
            ["LastSuccessfulRun"] = jobInfo.LastSuccessfulRun,
            ["LastFailedRun"] = jobInfo.LastFailedRun,
            ["Trigger"] = trigger,
            ["TriggeredByRun"] = triggeredBy,
            ["Schedule"] = schedule
        };

        AddRunTemplateData(data, "LastRun", jobInfo.LastRun);
        AddRunTemplateData(data, "LastSuccessfulRun", jobInfo.LastSuccessfulRun);
        AddRunTemplateData(data, "LastFailedRun", jobInfo.LastFailedRun);
        AddRunTemplateData(data, "TriggeredByRun", triggeredBy);
        foreach (var sp in schedule?.Parameters ?? []) {
            if (sp.Enabled)
                data[$"Schedule_Parameter_{sp.Key}"] = sp.Value;
        }

        if (trigger?.TriggerParameters == null)
            return data;

        foreach (var tp in trigger.TriggerParameters)
            data[$"Trigger_Parameter_{tp.Key}"] = tp.Value;

        return data;
    }

    private static void AddRunTemplateData(IDictionary<string, object?> data, string prefix, JobRunRes? run)
    {
        if (run == null)
            return;

        var results = run.GetResultDictionary();
        var parameters = run.GetParameterDictionary();
        foreach (var kvp in results)
            data[$"{prefix}_Result_{kvp.Key}"] = kvp.Value;

        foreach (var kvp in parameters)
            data[$"{prefix}_Parameter_{kvp.Key}"] = kvp.Value;
    }

    private JobRunParameterReq CreateRunParameterFromDefinition(JobParameterRes p, Dictionary<string, object?> templateData)
    {
        var known = LyoTypeInfo.FromName(p.Type);
        var req = new JobRunParameterReq { Key = p.Key, Description = p.Description, Type = LyoTypeInfo.NormalizeFullName(p.Type) };

        // An expression default carries the template in DefaultTemplate, so the declared type stays whatever the worker consumes (a DateTime, an int) rather than a template.
        var spec = LyoParameterSpec.From(p);
        if (LyoParameterDefaults.IsExpression(spec)) {
            if (LyoParameterDefaults.TryResolve(spec, p.Value, template => FormatTemplateValue(template, templateData), out var resolved, out var error))
                req.Value = resolved;
            else {
                _logger.LogWarning("Parameter default expression did not resolve for {Key}: {Error}", p.Key, error);
                req.Value = p.Value;
            }

            return req;
        }

        req.Value = FormatterLyoType.IsFormattable(known) ? FormatTemplateValue(p.Value ?? "", templateData) : p.Value;
        return req;
    }

    private JobRunParameterReq CreateRunParameterFromSchedule(JobScheduleParameterRes p, Dictionary<string, object?> templateData)
    {
        var known = LyoTypeInfo.FromName(p.Type);
        var req = new JobRunParameterReq {
            Key = p.Key,
            Description = p.Description,
            Type = LyoTypeInfo.NormalizeFullName(p.Type),
            EncryptedValue = p.EncryptedValue
        };

        req.Value = FormatterLyoType.IsFormattable(known) ? FormatTemplateValue(p.Value ?? "", templateData) : p.Value;
        return req;
    }

    private JobRunParameterReq CreateRunParameterFromTrigger(JobTriggerParameterRes p, Dictionary<string, object?> templateData)
    {
        var known = LyoTypeInfo.FromName(p.Type);
        var req = new JobRunParameterReq { Key = p.Key, Description = p.Description, Type = LyoTypeInfo.NormalizeFullName(p.Type) };
        req.Value = FormatterLyoType.IsFormattable(known) ? FormatTemplateValue(p.Value ?? "", templateData) : p.Value;
        return req;
    }

    /// <summary>
    /// Scheduler-only: <c>{{selector}}</c> → <c>{selector}</c>. Does not run inside <see cref="IFormatterService" />.
    /// SmartFormat uses <c>{{</c> as a literal brace.
    /// </summary>
    private static string UnwrapLegacyDoubleBracePlaceholders(string template)
        => LegacyDoubleBracePlaceholderRegex.Replace(template, static m => "{" + m.Groups[1].Value.Trim() + "}");
}
