using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.Export;
using Lyo.Api.Models.Builders;
using Lyo.Common.Enums;
using Lyo.Common.Identifiers;
using Lyo.Encryption;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Models.Security;
using Lyo.Job.Postgres.Database;
using Lyo.Job.Postgres.Events;
using Lyo.MessageQueue;
using Lyo.Postgres;
using Lyo.Query.Models.Enums;
using Lyo.Schedule.Models;
using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Constants = Lyo.Job.Models.Constants;
using JobRunResult = Lyo.Job.Postgres.Database.JobRunResult;

namespace Lyo.Job.Postgres;

/// <summary>Extension methods for PostgreSQL job management database context registration.</summary>
public static class Extensions
{
    /// <summary>Maps job API endpoints. Call after AddPostgresJobManagement.</summary>
    public static WebApplication BuildJobGroup(this WebApplication app)
    {
        app.CreateBuilder<JobContext, JobDefinition, JobDefinitionReq, JobDefinitionRes, Guid>(Constants.Rest.Job.Definitions, "Job")
            .WithCrud(
                ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance, new() {
                    BeforeCreate = ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres(),
                    AfterCreate = ctx => JobAuditHelper.RecordCreated(ctx.Services, nameof(JobDefinition), ctx.Entity.Id),
                    BeforeUpdate = ctx => {
                        ctx.Entity.DefinitionVersion++;
                        ctx.Entity.UpdatedTimestamp = DateTime.UtcNow;
                    },
                    AfterUpdate = ctx => {
                        JobAuditHelper.RecordUpdated(ctx.Services, nameof(JobDefinition), ctx.Entity.Id);
                        app.Services.GetRequiredService<IJobEventPublisher>().PublishDefinitionUpdatedAsync(ctx.Entity.Id).GetAwaiter().GetResult();
                    },
                    BeforeDelete = ctx => {
                        var i = ctx.Entity;
                        var db = ctx.DbContext;
                        foreach (var jobRun in i.JobRuns) {
                            foreach (var x in jobRun.InverseReRanFromJobRun)
                                x.ReRanFromJobRunId = null;

                            db.JobRunLogs.RemoveRange(jobRun.JobRunLogs);
                            db.JobRunParameters.RemoveRange(jobRun.JobRunParameters);
                            db.JobRunResults.RemoveRange(jobRun.JobRunResults);
                        }

                        db.JobRuns.RemoveRange(i.JobRuns);
                        foreach (var schedule in i.JobSchedules)
                            db.JobScheduleParameters.RemoveRange(schedule.JobScheduleParameters);

                        db.JobSchedules.RemoveRange(i.JobSchedules);
                        db.JobParameters.RemoveRange(i.JobParameters);
                    }
                })
            .Build();

        app.CreateBuilder<JobContext, JobParameter, JobParameterReq, JobParameterRes, Guid>($"{Constants.Rest.Job.DefinitionParameters}", "Job")
            .WithCrud(
                ApiFeatureSet.DefaultCrud,
                new() {
                    BeforeCreate = ctx => {
                        ctx.Entity.Id = LyoGuid.CreateCombPostgres();
                        EncryptJobParameterEntity(ctx.Services, ctx.Entity);
                    },
                    AfterCreate = ctx => {
                        JobAuditHelper.RecordCreated(ctx.Services, nameof(JobParameter), ctx.Entity.Id);
                        app.Services.GetRequiredService<IJobEventPublisher>().PublishDefinitionUpdatedAsync(ctx.Entity.JobDefinitionId).GetAwaiter().GetResult();
                    },
                    BeforeUpdate = ctx => EncryptJobParameterEntity(ctx.Services, ctx.Entity),
                    AfterUpdate = ctx => {
                        JobAuditHelper.RecordUpdated(ctx.Services, nameof(JobParameter), ctx.Entity.Id);
                        app.Services.GetRequiredService<IJobEventPublisher>().PublishDefinitionUpdatedAsync(ctx.Entity.JobDefinitionId).GetAwaiter().GetResult();
                    }
                })
            .Build();

        app.CreateBuilder<JobContext, JobSchedule, JobScheduleReq, JobScheduleRes, Guid>($"{Constants.Rest.Job.Schedules}", "Job")
            .WithCrud(
                ApiFeatureSet.DefaultCrud, new() {
                    BeforeCreate = ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres(),
                    AfterCreate = ctx => {
                        JobAuditHelper.RecordCreated(ctx.Services, nameof(JobSchedule), ctx.Entity.Id);
                        app.Services.GetRequiredService<IJobEventPublisher>().PublishDefinitionUpdatedAsync(ctx.Entity.JobDefinitionId).GetAwaiter().GetResult();
                    },
                    AfterUpdate = ctx => {
                        JobAuditHelper.RecordUpdated(ctx.Services, nameof(JobSchedule), ctx.Entity.Id);
                        app.Services.GetRequiredService<IJobEventPublisher>().PublishDefinitionUpdatedAsync(ctx.Entity.JobDefinitionId).GetAwaiter().GetResult();
                    }
                })
            .Build();

        app.CreateBuilder<JobContext, JobTrigger, JobTriggerReq, JobTriggerRes, Guid>($"{Constants.Rest.Job.Triggers}", "Job")
            .WithCrud(
                ApiFeatureSet.DefaultCrud, new() {
                    BeforeCreate = ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres(),
                    AfterCreate = ctx => {
                        JobAuditHelper.RecordCreated(ctx.Services, nameof(JobTrigger), ctx.Entity.Id);
                        var publisher = app.Services.GetRequiredService<IJobEventPublisher>();
                        publisher.PublishDefinitionUpdatedAsync(ctx.Entity.TriggersJobDefinitionId).GetAwaiter().GetResult();
                        publisher.PublishDefinitionUpdatedAsync(ctx.Entity.JobDefinitionId).GetAwaiter().GetResult();
                    },
                    AfterUpdate = ctx => {
                        JobAuditHelper.RecordUpdated(ctx.Services, nameof(JobTrigger), ctx.Entity.Id);
                        var publisher = app.Services.GetRequiredService<IJobEventPublisher>();
                        publisher.PublishDefinitionUpdatedAsync(ctx.Entity.TriggersJobDefinitionId).GetAwaiter().GetResult();
                        publisher.PublishDefinitionUpdatedAsync(ctx.Entity.JobDefinitionId).GetAwaiter().GetResult();
                    }
                })
            .Build();

        app.CreateBuilder<JobContext, JobRun, JobRunReq, JobRunRes, Guid>(Constants.Rest.Job.Runs, "Job")
            .WithQuery()
            .WithGet()
            .WithDelete(
                ctx => {
                    var jobRun = ctx.Entity;
                    var db = ctx.DbContext;
                    foreach (var i in jobRun.InverseReRanFromJobRun)
                        i.ReRanFromJobRunId = null;

                    db.JobRunLogs.RemoveRange(jobRun.JobRunLogs);
                    db.JobRunParameters.RemoveRange(jobRun.JobRunParameters);
                    db.JobRunResults.RemoveRange(jobRun.JobRunResults);
                }, null, ["JobRunLogs", "JobRunParameters", "JobRunResults", "InverseReRanFromJobRun"])
            .WithDeleteBulk(
                ctx => {
                    var jobRun = ctx.Entity;
                    var db = ctx.DbContext;
                    db.JobRunLogs.RemoveRange(jobRun.JobRunLogs);
                    db.JobRunParameters.RemoveRange(jobRun.JobRunParameters);
                    db.JobRunResults.RemoveRange(jobRun.JobRunResults);
                }, null, ["JobRunLogs", "JobRunParameters", "JobRunResults", "InverseReRanFromJobRun"])
            .WithExport()
            .Build();

        app.CreateBuilder<JobContext, JobRunParameter, JobRunParameterReq, JobRunParameterRes, Guid>(Constants.Rest.Job.RunParameters, "Job")
            .WithQuery()
            .WithGet()
            .WithCreate(ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres())
            .Build();

        app.CreateBuilder<JobContext, JobRunResult, JobRunResultRes, JobRunResultRes, Guid>(Constants.Rest.Job.RunResults, "Job")
            .WithQuery()
            .WithGet()
            .WithCreate(ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres())
            .Build();

        app.CreateBuilder<JobContext, JobRunLog, JobRunLogReq, JobRunLogRes, Guid>(Constants.Rest.Job.RunLogs, "Job")
            .WithQuery()
            .WithGet()
            .WithCreate(ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres())
            .Build();

        app.CreateBuilder<JobContext, JobWorkerInstance, JobWorkerInstanceReq, JobWorkerInstanceRes, Guid>(Constants.Rest.Job.WorkerInstances, "Job")
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres() })
            .Build();

        app.CreateBuilder<JobContext, JobBlackoutCalendar, JobBlackoutCalendarReq, JobBlackoutCalendarRes, Guid>(Constants.Rest.Job.BlackoutCalendars, "Job")
            .WithCrud(
                ApiFeatureSet.DefaultCrud, new() {
                    BeforeCreate = ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres(),
                    BeforeDelete = ctx => ctx.DbContext.JobBlackoutWindows.RemoveRange(ctx.Entity.JobBlackoutWindows),
                    DeleteIncludes = ["JobBlackoutWindows"]
                })
            .Build();

        app.CreateBuilder<JobContext, JobBlackoutWindow, JobBlackoutWindowReq, JobBlackoutWindowRes, Guid>(Constants.Rest.Job.BlackoutWindows, "Job")
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres() })
            .Build();

        app.CreateBuilder<JobContext, JobWorkflow, JobWorkflowReq, JobWorkflowRes, Guid>(Constants.Rest.Job.Workflows, "Job")
            .WithCrud(
                ApiFeatureSet.DefaultCrud, new() {
                    BeforeCreate = ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres(),
                    BeforeDelete = ctx => {
                        var workflow = ctx.Entity;
                        var db = ctx.DbContext;
                        foreach (var run in workflow.JobWorkflowRuns)
                            db.JobWorkflowRunSteps.RemoveRange(run.JobWorkflowRunSteps);

                        db.JobWorkflowRuns.RemoveRange(workflow.JobWorkflowRuns);
                        db.JobWorkflowSteps.RemoveRange(workflow.JobWorkflowSteps);
                    },
                    DeleteIncludes = ["JobWorkflowRuns", "JobWorkflowRuns.JobWorkflowRunSteps", "JobWorkflowSteps"]
                })
            .Build();

        app.CreateBuilder<JobContext, JobWorkflowStep, JobWorkflowStepReq, JobWorkflowStepRes, Guid>(Constants.Rest.Job.WorkflowSteps, "Job")
            .WithCrud(
                ApiFeatureSet.DefaultCrud, new() {
                    BeforeCreate = ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres(),
                    BeforeDelete = ctx => ctx.DbContext.JobWorkflowRunSteps.RemoveRange(ctx.Entity.JobWorkflowRunSteps),
                    DeleteIncludes = ["JobWorkflowRunSteps"]
                })
            .Build();

        app.CreateBuilder<JobContext, JobWorkflowRun, JobWorkflowRunReq, JobWorkflowRunRes, Guid>(Constants.Rest.Job.WorkflowRuns, "Job")
            .WithCrud(
                ApiFeatureSet.DefaultCrud, new() {
                    BeforeCreate = ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres(),
                    BeforeDelete = ctx => ctx.DbContext.JobWorkflowRunSteps.RemoveRange(ctx.Entity.JobWorkflowRunSteps),
                    DeleteIncludes = ["JobWorkflowRunSteps"]
                })
            .Build();

        app.CreateBuilder<JobContext, JobWorkflowRunStep, JobWorkflowRunStepReq, JobWorkflowRunStepRes, Guid>(Constants.Rest.Job.WorkflowRunSteps, "Job")
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres() })
            .Build();

        MapStatsEndpoint(app);
        MapNextRunsEndpoint(app);
        MapLifecycleEndpoints(app);
        return app;
    }

    /// <summary>Maps the <c>GET /Job/Definition/{id}/Stats</c> endpoint. Called by <see cref="BuildJobGroup" />.</summary>
    private static void MapStatsEndpoint(WebApplication app)
        => app.MapGet(
                $"/{Constants.Rest.Job.Definitions}/{{id:guid}}/Stats", async (Guid id, int days, JobService jobService, CancellationToken ct) => {
                    days = days > 0 ? days : 30;
                    var stats = await jobService.GetDefinitionStats(id, days, ct).ConfigureAwait(false);
                    return stats is null ? Results.NotFound() : Results.Ok(stats);
                })
            .WithTags("Job")
            .WithName("GetJobDefinitionStats");

    /// <summary>Maps the <c>GET /Job/Definition/{id}/NextRuns</c> endpoint. Called by <see cref="BuildJobGroup" />.</summary>
    private static void MapNextRunsEndpoint(WebApplication app)
        => app.MapGet(
                $"/{Constants.Rest.Job.Definitions}/{{id:guid}}/NextRuns", async (Guid id, int count, JobService jobService, CancellationToken ct) => {
                    count = count > 0 ? count : 20;
                    var nextRuns = await jobService.GetNextRuns(id, count, ct).ConfigureAwait(false);
                    return Results.Ok(nextRuns);
                })
            .WithTags("Job")
            .WithName("GetJobDefinitionNextRuns");

    /// <summary>
    /// Maps the run lifecycle endpoints that delegate to <see cref="JobService" /> (Create, Started, Finished, Cancel, Rerun, Log). These are required by
    /// <c>Lyo.Job.Scheduler</c> and <c>Lyo.Job.Worker</c> and are mapped automatically by <see cref="BuildJobGroup" />.
    /// </summary>
    private static void MapLifecycleEndpoints(WebApplication app)
    {
        app.MapPost(
                $"/{Constants.Rest.Job.Runs}/Create", async (JobRunReq req, JobService jobService, CancellationToken ct) => {
                    var result = await jobService.CreateJobRun(req, ct).ConfigureAwait(false);
                    return result.IsSuccess ? Results.Created($"/{Constants.Rest.Job.Runs}/{result.Data!.Id}", result.Data) : Results.BadRequest(result.Error);
                })
            .WithTags("Job")
            .WithName("CreateJobRun");

        app.MapPost(
                $"/{Constants.Rest.Job.Runs}/{{id:guid}}/Started", async (Guid id, JobService jobService) => {
                    var (run, error) = await jobService.StartedJobRun(id).ConfigureAwait(false);
                    return error is null ? Results.Ok(run) : Results.BadRequest(error);
                })
            .WithTags("Job")
            .WithName("StartedJobRun");

        app.MapPost(
                $"/{Constants.Rest.Job.Runs}/{{id:guid}}/Finished", async (Guid id, IReadOnlyList<JobRunResultReq> results, JobService jobService) => {
                    var (run, error) = await jobService.FinishedJobRun(id, results).ConfigureAwait(false);
                    return error is null ? Results.Ok(run) : Results.BadRequest(error);
                })
            .WithTags("Job")
            .WithName("FinishedJobRun");

        app.MapPost(
                $"/{Constants.Rest.Job.Runs}/{{id:guid}}/Cancel", async (Guid id, JobService jobService) => {
                    var (run, error) = await jobService.CancelJobRun(id).ConfigureAwait(false);
                    return error is null ? Results.Ok(run) : Results.BadRequest(error);
                })
            .WithTags("Job")
            .WithName("CancelJobRun");

        app.MapPost(
                $"/{Constants.Rest.Job.Runs}/{{id:guid}}/Rerun", async (Guid id, JobService jobService) => {
                    var result = await jobService.RerunJob(id).ConfigureAwait(false);
                    return result is { IsSuccess: true } ? Results.Ok(result.Data) : Results.BadRequest(result?.Error);
                })
            .WithTags("Job")
            .WithName("RerunJob");

        app.MapPost(
                $"/{Constants.Rest.Job.Runs}/{{id:guid}}/Log", async (Guid id, JobRunLogReq req, JobService jobService) => {
                    var result = await jobService.Log(id, req).ConfigureAwait(false);
                    return result.IsSuccess ? Results.Created($"/{Constants.Rest.Job.RunLogs}/{result.Data!.Id}", result.Data) : Results.BadRequest(result.Error);
                })
            .WithTags("Job")
            .WithName("LogJobRun");

        app.MapPatch(
                $"/{Constants.Rest.Job.Runs}/{{id:guid}}/Heartbeat", async (Guid id, JobRunHeartbeatReq? req, JobService jobService, CancellationToken ct) => {
                    var (run, error) = await jobService.HeartbeatJobRun(id, req, ct).ConfigureAwait(false);
                    return error is null ? Results.Ok(run) : Results.BadRequest(error);
                })
            .WithTags("Job")
            .WithName("HeartbeatJobRun");

        app.MapPost(
                $"/{Constants.Rest.Job.Runs}/{{id:guid}}/Children", async (Guid id, JobCreateChildRunsReq req, JobService jobService, CancellationToken ct) => {
                    try {
                        var children = await jobService.CreateChildRunsAsync(id, req, ct).ConfigureAwait(false);
                        return Results.Ok(children);
                    }
                    catch (InvalidOperationException ex) {
                        return Results.BadRequest(LyoProblemDetailsBuilder.Create().WithMessage(ex.Message).Build());
                    }
                })
            .WithTags("Job")
            .WithName("CreateChildJobRuns");
    }

    /// <summary>Configures Mapster job entity mappings. Call when configuring Mapster (e.g. config.Apply(ConfigureJobMappings)).</summary>
    /// <remarks>
    /// All entity→Res mappings for positional record types use <c>MapWith()</c> to bypass <c>RecordTypeAdapter.CreateBlockExpression</c>, which throws "Collection was modified"
    /// during eager <c>Compile()</c> when sub-mapping compilation adds entries to Mapster's internal list while that list is being enumerated (a Mapster bug). Req→entity mappings are
    /// unaffected and use the normal fluent API since they target mutable class types.
    /// </remarks>
    public static TypeAdapterConfig ConfigureJobMappings(this TypeAdapterConfig config)
    {
        config.NewConfig<JobBlackoutCalendarReq, JobBlackoutCalendar>().Map(to => to.JobBlackoutWindows, from => from.CreateBlackoutWindows);

        config.NewConfig<JobBlackoutWindowReq, JobBlackoutWindow>()
            .Map(dest => dest.DayFlags, src => src.DayFlags.ToString())
            .Map(dest => dest.StartTime, src => src.StartTime.ToString())
            .Map(dest => dest.EndTime, src => src.EndTime.ToString())
            .Map(dest => dest.Policy, src => src.Policy.ToString());

        config.NewConfig<JobWorkflowReq, JobWorkflow>().Map(to => to.JobWorkflowSteps, from => from.CreateSteps);

        config.NewConfig<JobWorkflowStepReq, JobWorkflowStep>()
            .Map(dest => dest.FailurePolicy, src => src.FailurePolicy.ToString());

        config.NewConfig<JobWorkflowRunReq, JobWorkflowRun>().Map(to => to.JobWorkflowRunSteps, from => from.CreateRunSteps);

        config.NewConfig<JobDefinitionReq, JobDefinition>()
            .Map(to => to.JobParameters, from => from.CreateParameters)
            .Map(to => to.JobSchedules, from => from.CreateSchedules)
            .Map(to => to.JobTriggerJobDefinitions, from => from.CreateTriggers)
            .Map(to => to.RetryBackoffType, from => from.RetryBackoffType.ToString());

        config.NewConfig<JobScheduleReq, JobSchedule>()
            .Map(dest => dest.Times, src => (src.Times ?? Enumerable.Empty<TimeOnly>()).Select(i => i.ToString()).ToList())
            .Map(dest => dest.Type, src => src.Type.ToString())
            .Map(dest => dest.DayFlags, src => src.DayFlags.ToString())
            .Map(dest => dest.MonthFlags, src => src.MonthFlags.ToString())
            .Map(dest => dest.MisfirePolicy, src => src.MisfirePolicy.ToString());

        config.NewConfig<JobTriggerReq, JobTrigger>()
            .Map(dest => dest.TriggerComparator, from => from.Comparison)
            .Map(dest => dest.TriggerJobResultKey, from => from.JobResultKey)
            .Map(dest => dest.TriggerJobResultValue, from => from.JobResultValue)
            .Map(dest => dest.JobTriggerParameters, from => from.CreateTriggerParameters);

        config.NewConfig<JobTriggerParameter, JobTriggerParameterRes>()
            .MapWith(src => new(src.Id, src.JobTriggerId, src.Key, Enum.Parse<JobParameterType>(src.Type), src.Value, src.Description, null, src.Enabled));

        config.NewConfig<JobTrigger, JobTriggerRes>()
            .MapWith(src => new(
                src.Id, src.TriggersJobDefinitionId, src.TriggerJobResultKey, Enum.Parse<ComparisonOperatorEnum>(src.TriggerComparator), src.TriggerJobResultValue, src.Description,
                src.Enabled, null, // JobDefinition — omitted to break circular ref
                src.JobTriggerParameters.Select(p => new JobTriggerParameterRes(
                        p.Id, p.JobTriggerId, p.Key, Enum.Parse<JobParameterType>(p.Type), p.Value, p.Description, null, p.Enabled))
                    .ToList(), null)); // TriggersJobDefinition — omitted to break circular ref

        config.NewConfig<JobScheduleParameter, JobScheduleParameterRes>()
            .MapWith(src => new(src.Id, src.JobScheduleId, src.Key, Enum.Parse<JobParameterType>(src.Type), src.Value, src.Description, null, src.Enabled));

        config.NewConfig<JobSchedule, JobScheduleRes>()
            .MapWith(src => new(
                src.Id, src.JobDefinitionId, Enum.Parse<MonthFlags>(src.MonthFlags), Enum.Parse<DayFlags>(src.DayFlags), Enum.Parse<ScheduleType>(src.Type),
                (src.Times ?? new List<string>()).Select(TimeOnly.Parse).ToList(), src.StartTime != null ? TimeOnly.Parse(src.StartTime) : null,
                src.EndTime != null ? TimeOnly.Parse(src.EndTime) : null, src.IntervalMinutes, src.Description, src.Enabled,
                src.JobScheduleParameters.Select(p => new JobScheduleParameterRes(
                        p.Id, p.JobScheduleId, p.Key, Enum.Parse<JobParameterType>(p.Type), p.Value, p.Description, null, p.Enabled))
                    .ToList(), src.CronExpression, Enum.Parse<JobMisfirePolicy>(src.MisfirePolicy), src.StartDateUtc, src.EndDateUtc, src.TimeZoneId, src.JobBlackoutCalendarId,
                src.JobBlackoutCalendar == null
                    ? null
                    : new JobBlackoutCalendarRes(
                        src.JobBlackoutCalendar.Id, src.JobBlackoutCalendar.Name, src.JobBlackoutCalendar.Description, src.JobBlackoutCalendar.Enabled,
                        src.JobBlackoutCalendar.JobBlackoutWindows.Select(w => new JobBlackoutWindowRes(
                                w.Id, w.JobBlackoutCalendarId, w.Name, Enum.Parse<DayFlags>(w.DayFlags), TimeOnly.Parse(w.StartTime), TimeOnly.Parse(w.EndTime),
                                Enum.Parse<JobBlackoutPolicy>(w.Policy), w.Enabled))
                            .ToList())));

        config.NewConfig<JobBlackoutCalendar, JobBlackoutCalendarRes>()
            .MapWith(src => new(
                src.Id, src.Name, src.Description, src.Enabled,
                src.JobBlackoutWindows.Select(w => new JobBlackoutWindowRes(
                        w.Id, w.JobBlackoutCalendarId, w.Name, Enum.Parse<DayFlags>(w.DayFlags), TimeOnly.Parse(w.StartTime), TimeOnly.Parse(w.EndTime),
                        Enum.Parse<JobBlackoutPolicy>(w.Policy), w.Enabled))
                    .ToList()));

        config.NewConfig<JobBlackoutWindow, JobBlackoutWindowRes>()
            .MapWith(src => new(
                src.Id, src.JobBlackoutCalendarId, src.Name, Enum.Parse<DayFlags>(src.DayFlags), TimeOnly.Parse(src.StartTime), TimeOnly.Parse(src.EndTime),
                Enum.Parse<JobBlackoutPolicy>(src.Policy), src.Enabled));

        config.NewConfig<JobWorkflow, JobWorkflowRes>()
            .MapWith(src => new(
                src.Id, src.Name, src.Description, src.Enabled,
                src.JobWorkflowSteps.Select(s => new JobWorkflowStepRes(
                        s.Id, s.JobWorkflowId, s.JobDefinitionId, s.StepName, s.StepOrder, s.DependsOnStepIds, Enum.Parse<JobWorkflowFailurePolicy>(s.FailurePolicy),
                        s.ParametersJson, s.Enabled, null))
                    .ToList()));

        config.NewConfig<JobWorkflowStep, JobWorkflowStepRes>()
            .MapWith(src => new(
                src.Id, src.JobWorkflowId, src.JobDefinitionId, src.StepName, src.StepOrder, src.DependsOnStepIds, Enum.Parse<JobWorkflowFailurePolicy>(src.FailurePolicy),
                src.ParametersJson, src.Enabled, null));

        config.NewConfig<JobWorkflowRun, JobWorkflowRunRes>()
            .MapWith(src => new(
                src.Id, src.JobWorkflowId, src.State, src.StartedTimestamp, src.FinishedTimestamp, src.CreatedTimestamp,
                src.JobWorkflowRunSteps.Select(s => new JobWorkflowRunStepRes(s.Id, s.JobWorkflowRunId, s.JobWorkflowStepId, s.JobRunId, s.State, null, null)).ToList(),
                null));

        config.NewConfig<JobWorkflowRunStep, JobWorkflowRunStepRes>()
            .MapWith(src => new(src.Id, src.JobWorkflowRunId, src.JobWorkflowStepId, src.JobRunId, src.State, null, null));

        config.NewConfig<JobParameter, JobParameterRes>()
            .MapWith(src => new(
                src.Id, src.JobDefinitionId, src.Key, src.Description, Enum.Parse<JobParameterType>(src.Type), MaskParameterValue(src.Value, src.EncryptedValue),
                MaskParameterEncryptedValue(src.EncryptedValue), src.AllowMultiple, true, src.Required,
                src.ValidationRegex, src.MinLength, src.MaxLength, src.AllowedValues));

        config.NewConfig<JobParallelRestriction, JobParallelRestrictionRes>()
            .MapWith(src => new(
                src.Id, src.BaseJobDefinitionId, src.OtherJobDefinitionId, src.Description, src.Enabled, null)); // OtherJobDefinition — omitted to break circular ref

        config.NewConfig<JobDefinition, JobDefinitionRes>()
            .MapWith(src => new(
                src.Id, src.Name, src.Description, src.Type, src.WorkerType, src.Enabled,
                src.JobParameters.Select(p => new JobParameterRes(
                        p.Id, p.JobDefinitionId, p.Key, p.Description, Enum.Parse<JobParameterType>(p.Type), MaskParameterValue(p.Value, p.EncryptedValue),
                        MaskParameterEncryptedValue(p.EncryptedValue), p.AllowMultiple, true, p.Required,
                        p.ValidationRegex, p.MinLength, p.MaxLength, p.AllowedValues))
                    .ToList(),
                src.JobSchedules.Select(s => new JobScheduleRes(
                        s.Id, s.JobDefinitionId, Enum.Parse<MonthFlags>(s.MonthFlags), Enum.Parse<DayFlags>(s.DayFlags), Enum.Parse<ScheduleType>(s.Type),
                        (s.Times ?? new List<string>()).Select(TimeOnly.Parse).ToList(), s.StartTime != null ? TimeOnly.Parse(s.StartTime) : null,
                        s.EndTime != null ? TimeOnly.Parse(s.EndTime) : null, s.IntervalMinutes, s.Description, s.Enabled,
                        s.JobScheduleParameters.Select(p => new JobScheduleParameterRes(
                                p.Id, p.JobScheduleId, p.Key, Enum.Parse<JobParameterType>(p.Type), p.Value, p.Description, null, p.Enabled))
                            .ToList(), s.CronExpression, Enum.Parse<JobMisfirePolicy>(s.MisfirePolicy), s.StartDateUtc, s.EndDateUtc, s.TimeZoneId, s.JobBlackoutCalendarId, null))
                    .ToList(),
                src.JobTriggerJobDefinitions.Select(t => new JobTriggerRes(
                        t.Id, t.TriggersJobDefinitionId, t.TriggerJobResultKey, Enum.Parse<ComparisonOperatorEnum>(t.TriggerComparator), t.TriggerJobResultValue, t.Description,
                        t.Enabled, null,
                        t.JobTriggerParameters.Select(p => new JobTriggerParameterRes(
                                p.Id, p.JobTriggerId, p.Key, Enum.Parse<JobParameterType>(p.Type), p.Value, p.Description, null, p.Enabled))
                            .ToList(), null))
                    .ToList(),
                src.JobParallelRestrictionBaseJobDefinitions
                    .Select(r => new JobParallelRestrictionRes(r.Id, r.BaseJobDefinitionId, r.OtherJobDefinitionId, r.Description, r.Enabled, null))
                    .ToList(), src.MaxRetryCount, src.RetryBackoffSeconds, src.TimeoutMinutes, src.MaxConcurrentRuns, src.CircuitBreakerThreshold, src.CircuitBreakerResetMinutes,
                src.CircuitBreakerTrippedAt, Enum.Parse<JobRetryBackoffType>(src.RetryBackoffType), src.Priority, src.RetentionDays, src.MaxRunsPerHour, src.ExpectedDurationMinutes,
                src.MustStartByMinutes, src.AlertOnFailure, src.AlertAfterConsecutiveFailures, src.AlertWebhookUrl, src.DefinitionVersion));

        config.NewConfig<JobRun, JobRunRes>()
            .MapWith(src => new() {
                Id = src.Id,
                State = src.State,
                Result = src.Result,
                CreatedTimestamp = src.CreatedTimestamp,
                StartedTimestamp = src.StartedTimestamp,
                FinishedTimestamp = src.FinishedTimestamp,
                AllowTriggers = src.AllowTriggers,
                JobDefinitionId = src.JobDefinitionId,
                // Map when the navigation is loaded; JobDefinitionRes has no JobRuns collection so there is no circular reference.
                JobDefinition = src.JobDefinition == null ? null : src.JobDefinition.Adapt<JobDefinitionRes>(config),
                JobScheduleId = src.JobScheduleId,
                JobSchedule = null, // break circular ref (JobSchedule.JobRuns → JobRun → JobSchedule)
                JobTriggerId = src.JobTriggerId,
                JobTrigger = null, // break circular ref
                ReRanFromJobRun = null, // self-referential — break
                ScheduledSlotUtc = src.ScheduledSlotUtc,
                RetryAttempt = src.RetryAttempt,
                LastHeartbeatUtc = src.LastHeartbeatUtc,
                Priority = src.Priority,
                ProgressPercent = src.ProgressPercent,
                ProgressMessage = src.ProgressMessage,
                IdempotencyKey = src.IdempotencyKey,
                DryRun = src.DryRun,
                SlaBreached = src.SlaBreached,
                TraceId = src.TraceId,
                ParentJobRunId = src.ParentJobRunId,
                BatchIndex = src.BatchIndex,
                BatchTotal = src.BatchTotal,
                DefinitionAuditVersion = src.DefinitionAuditVersion,
                JobRunParameters =
                    src.JobRunParameters.Select(p => new JobRunParameterRes(
                            p.Id, p.JobRunId, p.Key, Enum.Parse<JobParameterType>(p.Type), MaskParameterValue(p.Value, p.EncryptedValue), p.Description,
                            MaskParameterEncryptedValue(p.EncryptedValue), false))
                        .ToList(),
                JobRunResults = src.JobRunResults.Select(r => new JobRunResultRes(r.Id, r.JobRunId, r.Key, Enum.Parse<JobParameterType>(r.Type), r.Value)).ToList(),
                JobRunLogs = src.JobRunLogs.Select(l => new JobRunLogRes(l.Id, l.JobRunId, Enum.Parse<JobLogLevel>(l.Level), l.Message, l.Context, l.StackTrace, l.Timestamp))
                    .ToList()
            });

        config.NewConfig<JobRunParameter, JobRunParameterRes>()
            .MapWith(src => new(
                src.Id, src.JobRunId, src.Key, Enum.Parse<JobParameterType>(src.Type), MaskParameterValue(src.Value, src.EncryptedValue), src.Description,
                MaskParameterEncryptedValue(src.EncryptedValue), false));

        config.NewConfig<JobRunResult, JobRunResultRes>().MapWith(src => new(src.Id, src.JobRunId, src.Key, Enum.Parse<JobParameterType>(src.Type), src.Value));
        config.NewConfig<JobRunLog, JobRunLogRes>()
            .MapWith(src => new(src.Id, src.JobRunId, Enum.Parse<JobLogLevel>(src.Level), src.Message, src.Context, src.StackTrace, src.Timestamp));

        config.NewConfig<JobRunReq, JobRun>().Map(dest => dest.Priority, src => src.Priority ?? 0);
        config.NewConfig<JobWorkerInstanceReq, JobWorkerInstance>().Map(dest => dest.State, src => src.State.ToString());
        config.NewConfig<JobWorkerInstance, JobWorkerInstanceRes>()
            .MapWith(src => new(
                src.Id, src.WorkerType, src.MachineName, src.ProcessId, Enum.Parse<JobWorkerInstanceState>(src.State), src.InFlightCount, src.StartedTimestamp,
                src.LastHeartbeatUtc));

        return config;
    }

    private static void EncryptJobParameterEntity(IServiceProvider services, JobParameter entity)
    {
        var encryption = services.GetService<IJobParameterEncryptionService>();
        if (encryption is null)
            return;

        var value = entity.Value;
        var encrypted = entity.EncryptedValue;
        encryption.EncryptParameterValue(ref value, ref encrypted);
        entity.Value = value;
        entity.EncryptedValue = encrypted ?? entity.EncryptedValue;
    }

    private static string? MaskParameterValue(string? value, byte[]? encryptedValue) => encryptedValue is not null ? "***" : value;

    private static byte[]? MaskParameterEncryptedValue(byte[]? encryptedValue) => encryptedValue is not null ? null : encryptedValue;

    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds JobContext to the service collection.</summary>
        /// <param name="connectionString">The PostgreSQL connection string</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddJobDbContext(string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            return services.AddJobDbContextFactory(new PostgresJobOptions { ConnectionString = connectionString })
                .AddScoped<JobContext>(sp => sp.GetRequiredService<IDbContextFactory<JobContext>>().CreateDbContext());
        }

        /// <summary>Adds JobContext to the service collection.</summary>
        /// <param name="configure">Action to configure the DbContext options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddJobDbContext(Action<DbContextOptionsBuilder> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddDbContext<JobContext>(configure);
            return services;
        }

        /// <summary>Adds PostgreSQL job management DbContextFactory to the service collection with optional auto-migrations.</summary>
        /// <param name="configure">Action to configure the PostgreSQL job options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddJobDbContextFactory(Action<PostgresJobOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresJobOptions();
            configure(options);
            return services.AddJobDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL job management DbContextFactory to the service collection using configuration binding.</summary>
        /// <param name="configuration">The configuration (e.g. builder.Configuration)</param>
        /// <param name="configSectionName">The configuration section name (defaults to PostgresJobOptions.SectionName)</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddJobDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresJobOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresJobOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddJobDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL job management DbContextFactory to the service collection with optional auto-migrations.</summary>
        /// <param name="options">The PostgreSQL job options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddJobDbContextFactory(PostgresJobOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(Options.Create(options));
            services.AddPostgresMigrations<JobContext, PostgresJobOptions>();
            services.AddDbContextFactory<JobContext>(dbOptions => dbOptions.UseNpgsql(
                options.ConnectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PostgresJobOptions.Schema)));

            return services;
        }

        /// <summary>
        /// Adds job management with PostgreSQL backend. Drop-and-play: registers DbContextFactory, auto-migrations (if enabled), and CRUD services. Requires: AddLyoQueryServices,
        /// AddFusionCache or AddLocalCache, MapsterMapper.IMapper (add mapping yourself).
        /// </summary>
        public IServiceCollection AddPostgresJobManagement(Action<PostgresJobOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresJobOptions();
            configure(options);
            return services.AddPostgresJobManagement(options);
        }

        /// <summary>Adds job management with PostgreSQL backend using configuration binding.</summary>
        public IServiceCollection AddPostgresJobManagementFromConfiguration(IConfiguration configuration, string configSectionName = PostgresJobOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new PostgresJobOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddPostgresJobManagement(options);
        }

        /// <summary>
        /// Adds job management with PostgreSQL backend. Drop-and-play: registers DbContextFactory, auto-migrations (if enabled), and CRUD services. Requires: AddLyoQueryServices,
        /// AddFusionCache or AddLocalCache, MapsterMapper.IMapper (add mapping yourself).
        /// </summary>
        public IServiceCollection AddPostgresJobManagement(PostgresJobOptions options)
        {
            services.AddJobDbContextFactory(options);
            services.AddLyoCrudServices<JobContext>();
            services.AddScoped<JobService>();
            // Register a no-op publisher so JobService can be resolved without a message-queue transport.
            // Call AddMqJobEventPublisher() afterwards to replace this with a real implementation.
            services.TryAddSingleton<IJobEventPublisher, NullJobEventPublisher>();
            return services;
        }

        /// <summary>
        /// Adds <see cref="JobMaintenanceService" /> as a hosted background service. Automatically fails dead jobs (heartbeat timeout), resets circuit breakers, purges old
        /// run history per retention settings, and prunes stale worker instances. Requires <see cref="IDbContextFactory{JobContext}" /> to be registered (call
        /// <see cref="AddJobDbContextFactory(IServiceCollection, PostgresJobOptions)" /> first).
        /// </summary>
        /// <param name="configure">Optional action to configure <see cref="JobMaintenanceOptions" />.</param>
        public IServiceCollection AddJobMaintenanceService(Action<JobMaintenanceOptions>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            var options = new JobMaintenanceOptions();
            configure?.Invoke(options);
            options.Validate();
            services.TryAddSingleton(options);
            return services.AddJobMaintenanceServiceCore();
        }

        /// <summary>
        /// Adds <see cref="JobMaintenanceService" /> like <see cref="AddJobMaintenanceService(IServiceCollection, Action{JobMaintenanceOptions}?)" />, binding
        /// <see cref="JobMaintenanceOptions" /> from configuration and validating them on host start.
        /// </summary>
        public IServiceCollection AddJobMaintenanceServiceFromConfiguration(IConfiguration configuration, string configSectionName = JobMaintenanceOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            services.AddOptions<JobMaintenanceOptions>()
                .Bind(configuration.GetSection(configSectionName))
                .Validate(o => o.GetValidationErrors().Count == 0, $"Invalid {nameof(JobMaintenanceOptions)} — see {nameof(JobMaintenanceOptions.GetValidationErrors)}.")
                .ValidateOnStart();

            services.TryAddSingleton(p => p.GetRequiredService<IOptions<JobMaintenanceOptions>>().Value);
            return services.AddJobMaintenanceServiceCore();
        }

        private IServiceCollection AddJobMaintenanceServiceCore()
        {
            services.AddSingleton<JobMaintenanceService>();
            services.AddSingleton<IHealth>(p => p.GetRequiredService<JobMaintenanceService>());
            services.AddHostedService(p => p.GetRequiredService<JobMaintenanceService>());
            return services;
        }

        /// <summary>
        /// Registers <see cref="MqJobEventPublisher" /> as the <see cref="IJobEventPublisher" /> implementation. Requires <see cref="IMqService" /> to already be registered (e.g.
        /// via <c>AddRabbitMq</c>).
        /// </summary>
        public IServiceCollection AddMqJobEventPublisher()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<IJobEventPublisher, MqJobEventPublisher>();
            services.AddHostedService<JobEventPublisherStartupService>();
            return services;
        }

        /// <summary>Registers <see cref="JobParameterEncryptionService" /> using an optional keyed <see cref="IEncryptionService" />.</summary>
        /// <param name="keyName">Keyed service name for <see cref="IEncryptionService" />.</param>
        public IServiceCollection AddJobParameterEncryption(string keyName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            services.TryAddSingleton<IJobParameterEncryptionService>(sp => {
                var encryption = sp.GetKeyedService<IEncryptionService>(keyName);
                return new JobParameterEncryptionService(encryption, keyName, sp.GetService<Microsoft.Extensions.Logging.ILogger<JobParameterEncryptionService>>());
            });
            return services;
        }
    }
}