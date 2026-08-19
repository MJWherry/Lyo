using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.Export;
using Lyo.Api.Mapping;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Error;
using Lyo.Cache;
using Lyo.Common.Identifiers;
using Lyo.Encryption;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Health;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Models.Security;
using Lyo.Job.Postgres.Database;
using Lyo.Job.Postgres.Events;
using Lyo.Job.Postgres.Mapping;
using Lyo.MessageQueue;
using Lyo.Postgres;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
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
                    BeforeCreate = ctx => {
                        ctx.Entity.Id = LyoGuid.CreateCombPostgres();
                        foreach (var parameter in ctx.Entity.JobParameters) {
                            if (parameter.Id == default)
                                parameter.Id = LyoGuid.CreateCombPostgres();

                            parameter.JobDefinitionId = ctx.Entity.Id;
                            EncryptJobParameterEntity(ctx.Services, parameter);
                        }

                        foreach (var schedule in ctx.Entity.JobSchedules) {
                            if (schedule.Id == default)
                                schedule.Id = LyoGuid.CreateCombPostgres();

                            schedule.JobDefinitionId = ctx.Entity.Id;
                            foreach (var scheduleParameter in schedule.JobScheduleParameters) {
                                if (scheduleParameter.Id == default)
                                    scheduleParameter.Id = LyoGuid.CreateCombPostgres();

                                scheduleParameter.JobScheduleId = schedule.Id;
                            }
                        }

                        foreach (var trigger in ctx.Entity.JobTriggerJobDefinitions) {
                            if (trigger.Id == default)
                                trigger.Id = LyoGuid.CreateCombPostgres();

                            trigger.JobDefinitionId = ctx.Entity.Id;
                            foreach (var triggerParameter in trigger.JobTriggerParameters) {
                                if (triggerParameter.Id == default)
                                    triggerParameter.Id = LyoGuid.CreateCombPostgres();

                                triggerParameter.JobTriggerId = trigger.Id;
                            }
                        }

                        foreach (var restriction in ctx.Entity.JobParallelRestrictionBaseJobDefinitions) {
                            if (restriction.Id == default)
                                restriction.Id = LyoGuid.CreateCombPostgres();

                            restriction.BaseJobDefinitionId = ctx.Entity.Id;
                        }

                        JobBlackoutCalendarEntityHelper.AssignNestedBlackoutCalendarIds(ctx.Entity);
                    },
                    AfterCreate = ctx => JobAuditHelper.RecordCreated(ctx.Services, nameof(JobDefinition), ctx.Entity.Id),
                    BeforeUpdate = ctx => {
                        ctx.Entity.DefinitionVersion++;
                        ctx.Entity.UpdatedTimestamp = DateTime.UtcNow;
                    },
                    AfterUpdate = ctx => {
                        JobAuditHelper.RecordUpdated(ctx.Services, nameof(JobDefinition), ctx.Entity.Id);
                        app.Services.GetRequiredService<IJobEventPublisher>().PublishDefinitionUpdatedAsync(ctx.Entity.Id).GetAwaiter().GetResult();
                    },
                    // Query dependents by FK — do not rely on DeleteIncludes; unloaded navigations left job_parameter rows behind (23503).
                    BeforeDelete = ctx => JobDefinitionCascadeDelete.RemoveDependents(ctx.DbContext, ctx.Entity.Id),
                    AfterDelete = ctx => {
                        app.Services.GetRequiredService<IJobEventPublisher>().PublishDefinitionUpdatedAsync(ctx.Entity.Id).GetAwaiter().GetResult();
                        JobRunQueryCache.InvalidateAsync(ctx.Services.GetService<ICacheService>()).GetAwaiter().GetResult();
                    }
                })
            .Build();

        app.CreateBuilder<JobContext, JobParameter, JobParameterReq, JobParameterRes, Guid>($"{Constants.Rest.Job.DefinitionParameters}", "Job")
            .WithCrud(
                ApiFeatureSet.DefaultCrud, new() {
                    BeforeCreate = ctx => {
                        ctx.Entity.Id = LyoGuid.CreateCombPostgres();
                        EncryptJobParameterEntity(ctx.Services, ctx.Entity);
                    },
                    AfterCreate = ctx => {
                        JobAuditHelper.RecordCreated(ctx.Services, nameof(JobParameter), ctx.Entity.Id);
                        app.Services.GetRequiredService<IJobEventPublisher>().PublishDefinitionUpdatedAsync(ctx.Entity.JobDefinitionId).GetAwaiter().GetResult();
                    },
                    BeforeUpdate = ctx => {
                        EncryptJobParameterEntity(ctx.Services, ctx.Entity);
                        ctx.Entity.UpdatedTimestamp = DateTime.UtcNow;
                    },
                    AfterUpdate = ctx => {
                        JobAuditHelper.RecordUpdated(ctx.Services, nameof(JobParameter), ctx.Entity.Id);
                        app.Services.GetRequiredService<IJobEventPublisher>().PublishDefinitionUpdatedAsync(ctx.Entity.JobDefinitionId).GetAwaiter().GetResult();
                    }
                })
            .Build();

        app.CreateBuilder<JobContext, JobSchedule, JobScheduleReq, JobScheduleRes, Guid>($"{Constants.Rest.Job.Schedules}", "Job")
            .WithCrud(
                ApiFeatureSet.DefaultCrud, new() {
                    BeforeCreate = ctx => {
                        ctx.Entity.Id = LyoGuid.CreateCombPostgres();
                        foreach (var scheduleParameter in ctx.Entity.JobScheduleParameters) {
                            if (scheduleParameter.Id == default)
                                scheduleParameter.Id = LyoGuid.CreateCombPostgres();

                            scheduleParameter.JobScheduleId = ctx.Entity.Id;
                        }

                        JobBlackoutCalendarEntityHelper.AssignNestedBlackoutCalendarIds(ctx.Entity);
                    },
                    AfterCreate = ctx => {
                        JobAuditHelper.RecordCreated(ctx.Services, nameof(JobSchedule), ctx.Entity.Id);
                        app.Services.GetRequiredService<IJobEventPublisher>().PublishDefinitionUpdatedAsync(ctx.Entity.JobDefinitionId).GetAwaiter().GetResult();
                    },
                    BeforeUpdate = ctx => ctx.Entity.UpdatedTimestamp = DateTime.UtcNow,
                    AfterUpdate = ctx => {
                        JobAuditHelper.RecordUpdated(ctx.Services, nameof(JobSchedule), ctx.Entity.Id);
                        app.Services.GetRequiredService<IJobEventPublisher>().PublishDefinitionUpdatedAsync(ctx.Entity.JobDefinitionId).GetAwaiter().GetResult();
                    },
                    BeforeDelete = ctx => RemoveScheduleDependents(ctx.DbContext, ctx.Entity)
                })
            .Build();

        app.CreateBuilder<JobContext, JobScheduleParameter, JobScheduleParameterReq, JobScheduleParameterRes, Guid>($"{Constants.Rest.Job.ScheduleParameters}", "Job")
            .WithCrud(
                ApiFeatureSet.DefaultCrud, new() {
                    BeforeCreate = ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres(),
                    AfterCreate = ctx => {
                        JobAuditHelper.RecordCreated(ctx.Services, nameof(JobScheduleParameter), ctx.Entity.Id);
                        PublishScheduleDefinitionUpdated(app, ctx.DbContext, ctx.Entity.JobScheduleId);
                    },
                    BeforeUpdate = ctx => ctx.Entity.UpdatedTimestamp = DateTime.UtcNow,
                    BeforePatch = ctx => ctx.Entity.UpdatedTimestamp = DateTime.UtcNow,
                    AfterUpdate = ctx => {
                        JobAuditHelper.RecordUpdated(ctx.Services, nameof(JobScheduleParameter), ctx.Entity.Id);
                        PublishScheduleDefinitionUpdated(app, ctx.DbContext, ctx.Entity.JobScheduleId);
                    },
                    AfterPatch = ctx => {
                        JobAuditHelper.RecordUpdated(ctx.Services, nameof(JobScheduleParameter), ctx.Entity.Id);
                        PublishScheduleDefinitionUpdated(app, ctx.DbContext, ctx.Entity.JobScheduleId);
                    },
                    AfterDelete = ctx => PublishScheduleDefinitionUpdated(app, ctx.DbContext, ctx.Entity.JobScheduleId)
                })
            .Build();

        app.CreateBuilder<JobContext, JobTrigger, JobTriggerReq, JobTriggerRes, Guid>($"{Constants.Rest.Job.Triggers}", "Job")
            .WithCrud(
                ApiFeatureSet.DefaultCrud, new() {
                    BeforeCreate = ctx => {
                        ctx.Entity.Id = LyoGuid.CreateCombPostgres();
                        foreach (var triggerParameter in ctx.Entity.JobTriggerParameters) {
                            if (triggerParameter.Id == default)
                                triggerParameter.Id = LyoGuid.CreateCombPostgres();

                            triggerParameter.JobTriggerId = ctx.Entity.Id;
                        }
                    },
                    AfterCreate = ctx => {
                        JobAuditHelper.RecordCreated(ctx.Services, nameof(JobTrigger), ctx.Entity.Id);
                        var publisher = app.Services.GetRequiredService<IJobEventPublisher>();
                        publisher.PublishDefinitionUpdatedAsync(ctx.Entity.TriggersJobDefinitionId).GetAwaiter().GetResult();
                        publisher.PublishDefinitionUpdatedAsync(ctx.Entity.JobDefinitionId).GetAwaiter().GetResult();
                    },
                    BeforeUpdate = ctx => ctx.Entity.UpdatedTimestamp = DateTime.UtcNow,
                    AfterUpdate = ctx => {
                        JobAuditHelper.RecordUpdated(ctx.Services, nameof(JobTrigger), ctx.Entity.Id);
                        var publisher = app.Services.GetRequiredService<IJobEventPublisher>();
                        publisher.PublishDefinitionUpdatedAsync(ctx.Entity.TriggersJobDefinitionId).GetAwaiter().GetResult();
                        publisher.PublishDefinitionUpdatedAsync(ctx.Entity.JobDefinitionId).GetAwaiter().GetResult();
                    },
                    BeforeDelete = ctx => RemoveTriggerDependents(ctx.DbContext, ctx.Entity)
                })
            .Build();

        app.CreateBuilder<JobContext, JobRun, JobRunReq, JobRunRes, Guid>(Constants.Rest.Job.Runs, "Job")
            .WithQuery()
            .WithGet()
            .WithDelete(
                ctx => RemoveJobRunDependents(ctx.DbContext, ctx.Entity),
                ctx => JobRunQueryCache.InvalidateAsync(ctx.Services.GetService<ICacheService>()).GetAwaiter().GetResult(),
                ["JobRunLogs", "JobRunParameters", "JobRunResults", "InverseReRanFromJobRun", "InverseTriggeredByJobRun", "InverseParentJobRun"])
            .WithDeleteBulk(
                ctx => RemoveJobRunDependents(ctx.DbContext, ctx.Entity),
                ctx => JobRunQueryCache.InvalidateAsync(ctx.Services.GetService<ICacheService>()).GetAwaiter().GetResult(),
                ["JobRunLogs", "JobRunParameters", "JobRunResults", "InverseReRanFromJobRun", "InverseTriggeredByJobRun", "InverseParentJobRun"])
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
            .WithCrud(
                ApiFeatureSet.DefaultCrud, new() {
                    BeforeCreate = ctx => {
                        ctx.Entity.Id = LyoGuid.CreateCombPostgres();
                        NormalizeJobWorkerInstanceTimestamps(ctx.Entity);
                    },
                    BeforePatch = ctx => NormalizeJobWorkerInstanceTimestamps(ctx.Entity)
                })
            .Build();

        app.CreateBuilder<JobContext, JobBlackoutCalendar, JobBlackoutCalendarReq, JobBlackoutCalendarRes, Guid>(Constants.Rest.Job.BlackoutCalendars, "Job")
            .WithCrud(
                ApiFeatureSet.DefaultCrud, new() {
                    BeforeCreate = ctx => {
                        ctx.Entity.Id = LyoGuid.CreateCombPostgres();
                        foreach (var window in ctx.Entity.JobBlackoutWindows) {
                            if (window.Id == default)
                                window.Id = LyoGuid.CreateCombPostgres();

                            window.JobBlackoutCalendarId = ctx.Entity.Id;
                        }
                    },
                    BeforeUpdate = ctx => ctx.Entity.UpdatedTimestamp = DateTime.UtcNow,
                    BeforeDelete = ctx => {
                        if (ctx.DbContext.JobSchedules.Any(s => s.JobBlackoutCalendarId == ctx.Entity.Id))
                            throw new ConflictException($"Cannot delete blackout calendar '{ctx.Entity.Name}' ({ctx.Entity.Id}) while schedules still reference it.");

                        ctx.DbContext.JobBlackoutWindows.RemoveRange(ctx.DbContext.JobBlackoutWindows.Where(w => w.JobBlackoutCalendarId == ctx.Entity.Id).ToList());
                    }
                })
            .Build();

        app.CreateBuilder<JobContext, JobBlackoutWindow, JobBlackoutWindowReq, JobBlackoutWindowRes, Guid>(Constants.Rest.Job.BlackoutWindows, "Job")
            .WithCrud(
                ApiFeatureSet.DefaultCrud,
                new() { BeforeCreate = ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres(), BeforeUpdate = ctx => ctx.Entity.UpdatedTimestamp = DateTime.UtcNow })
            .Build();

        app.CreateBuilder<JobContext, JobWorkflow, JobWorkflowReq, JobWorkflowRes, Guid>(Constants.Rest.Job.Workflows, "Job")
            .WithCrud(
                ApiFeatureSet.DefaultCrud, new() {
                    BeforeCreate = ctx => {
                        ctx.Entity.Id = LyoGuid.CreateCombPostgres();
                        foreach (var step in ctx.Entity.JobWorkflowSteps) {
                            if (step.Id == default)
                                step.Id = LyoGuid.CreateCombPostgres();

                            step.JobWorkflowId = ctx.Entity.Id;
                        }
                    },
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
                ApiFeatureSet.DefaultCrud,
                new() {
                    BeforeCreate = ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres(),
                    BeforeDelete = ctx => ctx.DbContext.JobWorkflowRunSteps.RemoveRange(ctx.Entity.JobWorkflowRunSteps),
                    DeleteIncludes = ["JobWorkflowRunSteps"]
                })
            .Build();

        app.CreateBuilder<JobContext, JobWorkflowRun, JobWorkflowRunReq, JobWorkflowRunRes, Guid>(Constants.Rest.Job.WorkflowRuns, "Job")
            .WithCrud(
                ApiFeatureSet.DefaultCrud, new() {
                    BeforeCreate = ctx => {
                        ctx.Entity.Id = LyoGuid.CreateCombPostgres();
                        foreach (var step in ctx.Entity.JobWorkflowRunSteps) {
                            if (step.Id == default)
                                step.Id = LyoGuid.CreateCombPostgres();

                            step.JobWorkflowRunId = ctx.Entity.Id;
                        }
                    },
                    BeforeDelete = ctx => ctx.DbContext.JobWorkflowRunSteps.RemoveRange(ctx.Entity.JobWorkflowRunSteps),
                    DeleteIncludes = ["JobWorkflowRunSteps"]
                })
            .Build();

        app.CreateBuilder<JobContext, JobWorkflowRunStep, JobWorkflowRunStepReq, JobWorkflowRunStepRes, Guid>(Constants.Rest.Job.WorkflowRunSteps, "Job")
            .WithCrud(ApiFeatureSet.DefaultCrud, new() { BeforeCreate = ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres() })
            .Build();

        MapStatsEndpoint(app);
        MapNextRunsEndpoint(app);
        MapDefinitionLatestRunsEndpoint(app);
        MapLifecycleEndpoints(app);
        return app;
    }

    /// <summary>Maps the <c>GET /Job/Definition/{id}/Stats</c> endpoint. Called by <see cref="BuildJobGroup" />.</summary>
    private static void MapStatsEndpoint(WebApplication app)
        => app.MapGet(
                $"/{Constants.Rest.Job.Definitions}/{{id:guid}}/Stats", async (Guid id, int days, JobService jobService, CancellationToken ct) => {
                    days = days > 0 ? days : 30;
                    var stats = await jobService.GetDefinitionStats(id, days, ct).ConfigureAwait(false);
                    return stats is null ? throw ApiErrorException.From(LyoProblemDetailsBuilder.CreateWithActivity().NotFound("Job definition", id.ToString()).Build()) : Results.Ok(stats);
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

    /// <summary>Maps the batch <c>POST /Job/Definition/LatestRuns</c> endpoint used by the scheduler's definition refresh. Called by <see cref="BuildJobGroup" />.</summary>
    private static void MapDefinitionLatestRunsEndpoint(WebApplication app)
        => app.MapPost(
                $"/{Constants.Rest.Job.DefinitionsLatestRuns}", async (List<Guid> definitionIds, JobService jobService, CancellationToken ct) => {
                    var results = await jobService.GetLatestRuns(definitionIds, ct).ConfigureAwait(false);
                    return Results.Ok(results);
                })
            .WithTags("Job")
            .WithName("GetJobDefinitionLatestRuns");

    /// <summary>
    /// Maps the run lifecycle endpoints that delegate to <see cref="JobService" /> (Create, Started, Finished, Cancel, Rerun, Resync, Log). These are required by
    /// <c>Lyo.Job.Scheduler</c> and <c>Lyo.Job.Worker</c> and are mapped automatically by <see cref="BuildJobGroup" />.
    /// </summary>
    private static void MapLifecycleEndpoints(WebApplication app)
    {
        app.MapPost(
                $"/{Constants.Rest.Job.RunsCreate}", async (JobRunReq req, JobService jobService, CancellationToken ct) => {
                    var result = await jobService.CreateJobRun(req, ct).ConfigureAwait(false);
                    // Return CreateResult so ApiClient deserializers (scheduler/worker/client) get IsSuccess/Data.
                    return result.IsSuccess ? Results.Created($"/{Constants.Rest.Job.Runs}/{result.Data!.Id}", result) : ProblemResult(result.Error);
                })
            .WithTags("Job")
            .WithName("CreateJobRun");

        app.MapPost(
                $"/{Constants.Rest.Job.Runs}/{{id:guid}}/Started", async (Guid id, JobService jobService) => {
                    var (run, error) = await jobService.StartedJobRun(id).ConfigureAwait(false);
                    return error is null ? Results.Ok(run) : ProblemResult(error);
                })
            .WithTags("Job")
            .WithName("StartedJobRun");

        app.MapPost(
                $"/{Constants.Rest.Job.Runs}/{{id:guid}}/Finished", async (Guid id, IReadOnlyList<JobRunResultReq> results, JobService jobService) => {
                    var (run, error) = await jobService.FinishedJobRun(id, results).ConfigureAwait(false);
                    return error is null ? Results.Ok(run) : ProblemResult(error);
                })
            .WithTags("Job")
            .WithName("FinishedJobRun");

        app.MapPost(
                $"/{Constants.Rest.Job.Runs}/{{id:guid}}/Cancel", async (Guid id, JobService jobService) => {
                    var (run, error) = await jobService.CancelJobRun(id).ConfigureAwait(false);
                    return error is null ? Results.Ok(run) : ProblemResult(error);
                })
            .WithTags("Job")
            .WithName("CancelJobRun");

        app.MapPost(
                $"/{Constants.Rest.Job.Runs}/{{id:guid}}/Requeue", async (Guid id, JobService jobService) => {
                    var (run, error) = await jobService.RequeueJobRun(id).ConfigureAwait(false);
                    return error is null ? Results.Ok(run) : ProblemResult(error);
                })
            .WithTags("Job")
            .WithName("RequeueJobRun");

        app.MapPost(
                $"/{Constants.Rest.Job.Runs}/{{id:guid}}/Rerun", async (Guid id, JobService jobService) => {
                    var result = await jobService.RerunJob(id).ConfigureAwait(false);
                    return result is { IsSuccess: true } ? Results.Ok(result.Data) : ProblemResult(result?.Error);
                })
            .WithTags("Job")
            .WithName("RerunJob");

        app.MapPost(
                $"/{Constants.Rest.Job.RunsResync}", async (Guid? definitionId, JobService jobService, CancellationToken ct) => {
                    var (result, error) = await jobService.ResyncQueuedRunsAsync(definitionId, ct).ConfigureAwait(false);
                    return error is null ? Results.Ok(result) : ProblemResult(error);
                })
            .WithTags("Job")
            .WithName("ResyncQueuedJobRuns");

        app.MapPost(
                $"/{Constants.Rest.Job.Runs}/{{id:guid}}/Log", async (Guid id, JobRunLogReq req, JobService jobService) => {
                    var result = await jobService.Log(id, req).ConfigureAwait(false);
                    return result.IsSuccess ? Results.Created($"/{Constants.Rest.Job.RunLogs}/{result.Data!.Id}", result.Data) : ProblemResult(result.Error);
                })
            .WithTags("Job")
            .WithName("LogJobRun");

        app.MapPatch(
                $"/{Constants.Rest.Job.Runs}/{{id:guid}}/Heartbeat", async (Guid id, JobRunHeartbeatReq? req, JobService jobService, CancellationToken ct) => {
                    var (run, error) = await jobService.HeartbeatJobRun(id, req, ct).ConfigureAwait(false);
                    return error is null ? Results.Ok(run) : ProblemResult(error);
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
                        throw ApiErrorException.From(LyoProblemDetails.FromCode(Api.Models.Constants.ApiErrorCodes.InvalidRequest, ex.Message));
                    }
                })
            .WithTags("Job")
            .WithName("CreateChildJobRuns");
    }

    /// <summary>Throws <see cref="ApiErrorException" /> so LoggingMiddleware writes and logs the problem. Falls back to a generic 400 when no problem was produced.</summary>
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static IResult ProblemResult(LyoProblemDetails? error)
    {
        error ??= LyoProblemDetailsBuilder.CreateWithActivity()
            .WithErrorCode(Api.Models.Constants.ApiErrorCodes.InvalidRequest)
            .WithMessage("The request could not be processed.")
            .Build();

        throw ApiErrorException.From(error);
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

    /// <summary>Notifies scheduler instances that the owning definition changed (schedule parameters affect run-parameter merging).</summary>
    private static void PublishScheduleDefinitionUpdated(WebApplication app, JobContext db, Guid jobScheduleId)
    {
        var definitionId = db.JobSchedules.Where(s => s.Id == jobScheduleId).Select(s => s.JobDefinitionId).FirstOrDefault();
        if (definitionId != default)
            app.Services.GetRequiredService<IJobEventPublisher>().PublishDefinitionUpdatedAsync(definitionId).GetAwaiter().GetResult();
    }

    private static void RemoveScheduleDependents(JobContext db, JobSchedule schedule)
    {
        db.JobScheduleParameters.RemoveRange(db.JobScheduleParameters.Where(p => p.JobScheduleId == schedule.Id).ToList());
        foreach (var run in db.JobRuns.Where(r => r.JobScheduleId == schedule.Id).ToList())
            run.JobScheduleId = null;

        if (!schedule.JobBlackoutCalendarId.HasValue)
            return;

        var calendarId = schedule.JobBlackoutCalendarId.Value;
        // Leave shared calendars; delete only when no other schedule still references this calendar.
        if (db.JobSchedules.Any(s => s.Id != schedule.Id && s.JobBlackoutCalendarId == calendarId))
            return;

        db.JobBlackoutWindows.RemoveRange(db.JobBlackoutWindows.Where(w => w.JobBlackoutCalendarId == calendarId).ToList());
        var calendar = db.JobBlackoutCalendars.Find(calendarId);
        if (calendar is not null)
            db.JobBlackoutCalendars.Remove(calendar);
    }

    private static void RemoveTriggerDependents(JobContext db, JobTrigger trigger)
    {
        db.JobTriggerParameters.RemoveRange(db.JobTriggerParameters.Where(p => p.JobTriggerId == trigger.Id).ToList());
        foreach (var run in db.JobRuns.Where(r => r.JobTriggerId == trigger.Id).ToList())
            run.JobTriggerId = null;
    }

    private static void RemoveJobRunDependents(JobContext db, JobRun jobRun)
    {
        db.JobWorkflowRunSteps.RemoveRange(db.JobWorkflowRunSteps.Where(s => s.JobRunId == jobRun.Id).ToList());
        foreach (var i in jobRun.InverseReRanFromJobRun)
            i.ReRanFromJobRunId = null;

        foreach (var i in jobRun.InverseTriggeredByJobRun)
            i.TriggeredByJobRunId = null;

        foreach (var i in jobRun.InverseParentJobRun)
            i.ParentJobRunId = null;

        db.JobRunLogs.RemoveRange(jobRun.JobRunLogs);
        db.JobRunParameters.RemoveRange(jobRun.JobRunParameters);
        db.JobRunResults.RemoveRange(jobRun.JobRunResults);
    }

    private static DateTime ToUtcDateTime(DateTime value)
        => value.Kind switch {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            var _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private static void NormalizeJobWorkerInstanceTimestamps(JobWorkerInstance entity)
    {
        entity.StartedTimestamp = ToUtcDateTime(entity.StartedTimestamp);
        entity.LastHeartbeatUtc = ToUtcDateTime(entity.LastHeartbeatUtc);
        if (entity.CreatedTimestamp != default)
            entity.CreatedTimestamp = ToUtcDateTime(entity.CreatedTimestamp);

        if (entity.UpdatedTimestamp.HasValue)
            entity.UpdatedTimestamp = ToUtcDateTime(entity.UpdatedTimestamp.Value);
    }

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
        /// Adds job management with PostgreSQL backend. Drop-and-play: registers DbContextFactory, auto-migrations (if enabled), CRUD services, and <see cref="JobLyoMapper" /> as
        /// <see cref="ILyoMapper" /> (hosts may replace with <see cref="CompositeLyoMapper" />). Requires: AddLyoQueryServices, AddFusionCache or AddLocalCache.
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
        /// Adds job management with PostgreSQL backend. Drop-and-play: registers DbContextFactory, auto-migrations (if enabled), CRUD services, and <see cref="JobLyoMapper" /> as
        /// <see cref="ILyoMapper" /> (hosts may replace with <see cref="CompositeLyoMapper" />). Requires: AddLyoQueryServices, AddFusionCache or AddLocalCache.
        /// </summary>
        public IServiceCollection AddPostgresJobManagement(PostgresJobOptions options)
        {
            services.AddJobDbContextFactory(options);
            services.AddLyoCrudServices<JobContext>();
            services.AddScoped<JobService>();
            services.TryAddSingleton<JobLyoMapper>();
            services.TryAddSingleton<ILyoMapper>(sp => sp.GetRequiredService<JobLyoMapper>());
            // Register a no-op publisher so JobService can be resolved without a message-queue transport.
            // Call AddMqJobEventPublisher() afterwards to replace this with a real implementation.
            services.TryAddSingleton<IJobEventPublisher, NullJobEventPublisher>();
            return services;
        }

        /// <summary>
        /// Adds <see cref="JobMaintenanceService" /> as a hosted background service. Automatically fails dead jobs (heartbeat timeout), resets circuit breakers, purges old run
        /// history per retention settings, and prunes stale worker instances. Requires <see cref="IDbContextFactory{JobContext}" /> to be registered (call
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
        /// Registers <see cref="Events.MqJobEventPublisher" /> as the <see cref="IJobEventPublisher" /> for API hosts with a job database. Scheduler/worker hosts must use
        /// <c>Lyo.Job.Client.AddMqJobEventPublisher*</c> instead. Requires <see cref="IMqService" />.
        /// </summary>
        public IServiceCollection AddMqJobEventPublisher()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddOptions<JobMqOptions>();
            services.TryAddSingleton(p => p.GetRequiredService<IOptions<JobMqOptions>>().Value);
            services.AddSingleton<IJobEventPublisher, MqJobEventPublisher>();
            services.AddHostedService<JobEventPublisherStartupService>();
            return services;
        }

        /// <summary>Registers the Postgres <see cref="Events.MqJobEventPublisher" /> and binds <see cref="JobMqOptions" /> from configuration (API hosts only).</summary>
        public IServiceCollection AddMqJobEventPublisherFromConfiguration(IConfiguration configuration, string configSectionName = JobMqOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddOptions<JobMqOptions>();
            services.TryAddSingleton(p => p.GetRequiredService<IOptions<JobMqOptions>>().Value);
            services.AddSingleton<IJobEventPublisher, MqJobEventPublisher>();
            services.AddHostedService<JobEventPublisherStartupService>();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                services.Configure<JobMqOptions>(section);

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
                return new JobParameterEncryptionService(encryption, keyName, sp.GetService<ILogger<JobParameterEncryptionService>>());
            });

            return services;
        }
    }
}