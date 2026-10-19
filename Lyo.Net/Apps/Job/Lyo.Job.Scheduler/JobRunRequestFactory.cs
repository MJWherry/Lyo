using System.Text.Json;
using Lyo.Common.Metadata.Records;
using Lyo.Formatter;
using Lyo.Job.Models;
using Lyo.Job.Models.Context;
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
    private readonly ILogger _logger = logger ?? NullLogger.Instance;

    /// <summary>
    /// Builds a run request for <paramref name="jobInfo" />. Definition parameters come first; schedule parameters replace same-key entries rather than appending, so the API never
    /// sees a duplicate key carrying an empty definition default next to the schedule value.
    /// </summary>
    /// <param name="jobInfo">Definition and its recent runs, used as template data.</param>
    /// <param name="schedule">Schedule that fired, when this is a scheduled run.</param>
    public JobRunReq Build(JobInfo jobInfo, JobScheduleRes? schedule)
    {
        ArgumentHelpers.ThrowIfNull(jobInfo);
        var req = new JobRunReq {
            JobDefinitionId = jobInfo.Definition.Id,
            JobScheduleId = schedule?.Id,
            CreatedBy = createdBy,
            JobRunParameters = []
        };

        var templateData = BuildTemplateData(jobInfo, schedule);
        foreach (var p in jobInfo.Definition.JobParameters ?? [])
            req.JobRunParameters.Add(CreateRunParameterFromDefinition(p, templateData));

        foreach (var sp in schedule?.Parameters ?? []) {
            if (!sp.Enabled)
                continue;

            var runParam = CreateRunParameterFromSchedule(sp, templateData);
            req.JobRunParameters.RemoveAll(existing => string.Equals(existing.Key, runParam.Key, StringComparison.OrdinalIgnoreCase));
            req.JobRunParameters.Add(runParam);
        }

        return req;
    }

    /// <summary>
    /// Builds the run request for one workflow step. Definition parameters come first, then the step's own <c>ParametersJson</c> values <b>replace</b> same-key entries — the
    /// same merge rule schedules use — so one shared definition can serve many workflows with different inputs.
    /// <para>
    /// <c>ParametersJson</c> is a flat object map (<c>{"Path":"/tmp/out"}</c>). The parameter type comes from the matching definition parameter; a key the definition does not
    /// declare is treated as a string. Unparseable JSON is logged and ignored rather than failing the step: a bad step configuration should still produce a runnable request
    /// from the definition defaults, and the workflow's own failure policy then decides what happens.
    /// </para>
    /// </summary>
    /// <param name="definition">Definition the step runs.</param>
    /// <param name="step">Step being started, supplying the parameter overrides.</param>
    /// <param name="stepScope">Context the step's templates resolve against, including any upstream runs seeded with <c>WithUpstreamRun</c>.</param>
    public JobRunReq BuildForWorkflowStep(JobDefinitionRes definition, JobWorkflowStepRes step, JobContextBag stepScope)
    {
        ArgumentHelpers.ThrowIfNull(definition);
        ArgumentHelpers.ThrowIfNull(step);
        ArgumentHelpers.ThrowIfNull(stepScope);
        var req = new JobRunReq {
            JobDefinitionId = definition.Id,
            CreatedBy = createdBy,
            JobRunParameters = []
        };

        foreach (var p in definition.JobParameters ?? [])
            req.JobRunParameters.Add(CreateRunParameterFromDefinition(p, stepScope));

        foreach (var (key, value) in ParseStepParameters(step)) {
            var declared = (definition.JobParameters ?? []).FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
            var type = declared?.Type ?? LyoTypeInfo.String.FullName;
            var formatted = FormatterLyoType.IsFormattable(LyoTypeInfo.FromName(type)) ? FormatTemplateValue(value ?? "", stepScope) : value;
            req.JobRunParameters.RemoveAll(existing => string.Equals(existing.Key, key, StringComparison.OrdinalIgnoreCase));
            req.JobRunParameters.Add(
                new JobRunParameterReq {
                    Key = declared?.Key ?? key,
                    Description = declared?.Description,
                    Type = LyoTypeInfo.NormalizeFullName(type),
                    Value = formatted
                });
        }

        return req;
    }

    /// <summary>Reads a step's <c>ParametersJson</c> object map. A JSON string becomes its text; anything else keeps its raw JSON so a structured value survives intact.</summary>
    private IReadOnlyList<KeyValuePair<string, string?>> ParseStepParameters(JobWorkflowStepRes step)
    {
        if (string.IsNullOrWhiteSpace(step.ParametersJson))
            return [];

        try {
            using var document = JsonDocument.Parse(step.ParametersJson!);
            if (document.RootElement.ValueKind != JsonValueKind.Object) {
                _logger.LogWarning("Workflow step {StepName} has a ParametersJson that is not an object map; the step runs on definition defaults", step.StepName);
                return [];
            }

            return [
                .. document.RootElement.EnumerateObject()
                    .Select(p => new KeyValuePair<string, string?>(p.Name, p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : p.Value.GetRawText()))
            ];
        }
        catch (JsonException ex) {
            _logger.LogWarning(ex, "Workflow step {StepName} has malformed ParametersJson; the step runs on definition defaults", step.StepName);
            return [];
        }
    }

    /// <summary>
    /// Formats String and Formatter parameter templates with <see cref="IFormatterService" />. Unresolved <c>{client...}</c> tokens stay in the value (<c>MaintainTokens</c>).
    /// Json/Xml values are not formatted.
    /// </summary>
    /// <param name="template">Raw parameter value.</param>
    /// <param name="templateData">Data the template resolves against.</param>
    public string FormatTemplateValue(string template, JobContextBag templateData)
    {
        if (string.IsNullOrEmpty(template) || template.IndexOf('{') < 0)
            return template;

        try {
            return formatter.Format(template, templateData);
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Template format failed: {Template}", template);
            return template;
        }
    }

    /// <summary>
    /// The context every parameter template resolves against. Internal so tests can assert the template editor's advertised token list against what actually resolves —
    /// the editor builds its own bag from the same <see cref="JobContextBag" /> seeders.
    /// </summary>
    /// <param name="jobInfo">Definition and its recent runs.</param>
    /// <param name="schedule">Schedule that fired, if any.</param>
    internal static JobContextBag BuildTemplateData(JobInfo jobInfo, JobScheduleRes? schedule)
        => new JobContextBag().WithDefinition(jobInfo.Definition)
            .WithLastRuns(jobInfo.LastRun, jobInfo.LastSuccessfulRun, jobInfo.LastFailedRun)
            .WithSchedule(schedule);

    private JobRunParameterReq CreateRunParameterFromDefinition(JobParameterRes p, JobContextBag templateData)
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

    private JobRunParameterReq CreateRunParameterFromSchedule(JobScheduleParameterRes p, JobContextBag templateData)
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
}
