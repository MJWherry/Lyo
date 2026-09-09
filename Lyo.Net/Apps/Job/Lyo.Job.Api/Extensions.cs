using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.Export;
using Lyo.Api.Mapping;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Error;
using Lyo.Cache;
using Lyo.Common.Core.Identifiers;
using Lyo.Encryption;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Models.Security;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Lyo.Job.Postgres.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Constants = Lyo.Job.Models.Constants;
using JobRunResult = Lyo.Job.Postgres.Database.JobRunResult;

namespace Lyo.Job.Api;

/// <summary>HTTP mapping for the Lyo Job API. Register store DI with <c>AddPostgresJobManagement</c> first.</summary>
public static class Extensions
{
    /// <summary>Maps job API endpoints. Invoke after <c>AddPostgresJobManagement</c>.</summary>
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

                        JobDefinitionWriteValidator.ValidateParameters(ctx.Entity.JobParameters);

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
                    // Query dependents by FK. Do not rely on DeleteIncludes; unloaded navigations left job_parameter rows behind (23503).
                    BeforeDelete = ctx => JobDefinitionCascadeDelete.RemoveDependents(ctx.DbContext, ctx.Entity.Id),
                    AfterDelete = ctx => {
                        app.Services.GetRequiredService<IJobEventPublisher>().PublishDefinitionUpdatedAsync(ctx.Entity.Id).GetAwaiter().GetResult();
                        QueryCacheInvalidation.InvalidateQueryCachesForBroadEntityTypeAsync<JobRun, JobDefinition>(ctx.Services.GetService<ICacheService>()).GetAwaiter().GetResult();
                    }
                })
            .Build();

        app.CreateBuilder<JobContext, JobParameter, JobParameterReq, JobParameterRes, Guid>($"{Constants.Rest.Job.DefinitionParameters}", "Job")
            .WithCrud(
                ApiFeatureSet.DefaultCrud, new() {
                    BeforeCreate = ctx => {
                        ctx.Entity.Id = LyoGuid.CreateCombPostgres();
                        EncryptJobParameterEntity(ctx.Services, ctx.Entity);
                        JobDefinitionWriteValidator.ValidateParameter(ctx.Entity);
                    },
                    AfterCreate = ctx => {
                        JobAuditHelper.RecordCreated(ctx.Services, nameof(JobParameter), ctx.Entity.Id);
                        app.Services.GetRequiredService<IJobEventPublisher>().PublishDefinitionUpdatedAsync(ctx.Entity.JobDefinitionId).GetAwaiter().GetResult();
                        InvalidateChildDefinitionQueryCaches<JobParameter>(ctx.Services);
                    },
                    BeforeUpdate = ctx => {
                        EncryptJobParameterEntity(ctx.Services, ctx.Entity);
                        ctx.Entity.UpdatedTimestamp = DateTime.UtcNow;
                        JobDefinitionWriteValidator.ValidateParameter(ctx.Entity);
                    },
                    AfterUpdate = ctx => {
                        JobAuditHelper.RecordUpdated(ctx.Services, nameof(JobParameter), ctx.Entity.Id);
                        app.Services.GetRequiredService<IJobEventPublisher>().PublishDefinitionUpdatedAsync(ctx.Entity.JobDefinitionId).GetAwaiter().GetResult();
                        InvalidateChildDefinitionQueryCaches<JobParameter>(ctx.Services);
                    },
                    BeforePatch = ctx => {
                        ctx.Entity.UpdatedTimestamp = DateTime.UtcNow;
                        JobDefinitionWriteValidator.ValidateParameter(ctx.Entity);
                    },
                    AfterPatch = ctx => InvalidateChildDefinitionQueryCaches<JobParameter>(ctx.Services),
                    AfterDelete = ctx => InvalidateChildDefinitionQueryCaches<JobParameter>(ctx.Services)
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
                        InvalidateChildDefinitionQueryCaches<JobSchedule>(ctx.Services);
                    },
                    BeforeUpdate = ctx => ctx.Entity.UpdatedTimestamp = DateTime.UtcNow,
                    AfterUpdate = ctx => {
                        JobAuditHelper.RecordUpdated(ctx.Services, nameof(JobSchedule), ctx.Entity.Id);
                        app.Services.GetRequiredService<IJobEventPublisher>().PublishDefinitionUpdatedAsync(ctx.Entity.JobDefinitionId).GetAwaiter().GetResult();
                        InvalidateChildDefinitionQueryCaches<JobSchedule>(ctx.Services);
                    },
                    BeforeDelete = ctx => RemoveScheduleDependents(ctx.DbContext, ctx.Entity),
                    AfterDelete = ctx => InvalidateChildDefinitionQueryCaches<JobSchedule>(ctx.Services)
                })
            .Build();

        app.CreateBuilder<JobContext, JobScheduleParameter, JobScheduleParameterReq, JobScheduleParameterRes, Guid>($"{Constants.Rest.Job.ScheduleParameters}", "Job")
            .WithCrud(
                ApiFeatureSet.DefaultCrud, new() {
                    BeforeCreate = ctx => ctx.Entity.Id = LyoGuid.CreateCombPostgres(),
                    AfterCreate = ctx => {
                        JobAuditHelper.RecordCreated(ctx.Services, nameof(JobScheduleParameter), ctx.Entity.Id);
                        PublishScheduleDefinitionUpdated(app, ctx.DbContext, ctx.Entity.JobScheduleId);
                        InvalidateChildDefinitionQueryCaches<JobScheduleParameter>(ctx.Services);
                    },
                    BeforeUpdate = ctx => ctx.Entity.UpdatedTimestamp = DateTime.UtcNow,
                    BeforePatch = ctx => ctx.Entity.UpdatedTimestamp = DateTime.UtcNow,
                    AfterUpdate = ctx => {
                        JobAuditHelper.RecordUpdated(ctx.Services, nameof(JobScheduleParameter), ctx.Entity.Id);
                        PublishScheduleDefinitionUpdated(app, ctx.DbContext, ctx.Entity.JobScheduleId);
                        InvalidateChildDefinitionQueryCaches<JobScheduleParameter>(ctx.Services);
                    },
                    AfterPatch = ctx => {
                        JobAuditHelper.RecordUpdated(ctx.Services, nameof(JobScheduleParameter), ctx.Entity.Id);
                        PublishScheduleDefinitionUpdated(app, ctx.DbContext, ctx.Entity.JobScheduleId);
                        InvalidateChildDefinitionQueryCaches<JobScheduleParameter>(ctx.Services);
                    },
                    AfterDelete = ctx => {
                        PublishScheduleDefinitionUpdated(app, ctx.DbContext, ctx.Entity.JobScheduleId);
                        InvalidateChildDefinitionQueryCaches<JobScheduleParameter>(ctx.Services);
                    }
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
                        InvalidateChildDefinitionQueryCaches<JobTrigger>(ctx.Services);
                    },
                    BeforeUpdate = ctx => ctx.Entity.UpdatedTimestamp = DateTime.UtcNow,
                    AfterUpdate = ctx => {
                        JobAuditHelper.RecordUpdated(ctx.Services, nameof(JobTrigger), ctx.Entity.Id);
                        var publisher = app.Services.GetRequiredService<IJobEventPublisher>();
                        publisher.PublishDefinitionUpdatedAsync(ctx.Entity.TriggersJobDefinitionId).GetAwaiter().GetResult();
                        publisher.PublishDefinitionUpdatedAsync(ctx.Entity.JobDefinitionId).GetAwaiter().GetResult();
                        InvalidateChildDefinitionQueryCaches<JobTrigger>(ctx.Services);
                    },
                    BeforeDelete = ctx => RemoveTriggerDependents(ctx.DbContext, ctx.Entity),
                    AfterDelete = ctx => InvalidateChildDefinitionQueryCaches<JobTrigger>(ctx.Services)
                })
            .Build();

        app.CreateBuilder<JobContext, JobRun, JobRunReq, JobRunRes, Guid>(Constants.Rest.Job.Runs, "Job")
            .WithQuery()
            .WithGet()
            .WithDelete(
                ctx => RemoveJobRunDependents(ctx.DbContext, ctx.Entity),
                ctx => QueryCacheInvalidation.InvalidateQueryCachesForBroadEntityTypeAsync<JobRun, JobDefinition>(ctx.Services.GetService<ICacheService>()).GetAwaiter().GetResult(),
                ["JobRunLogs", "JobRunParameters", "JobRunResults", "InverseReRanFromJobRun", "InverseTriggeredByJobRun", "InverseParentJobRun"])
            .WithDeleteBulk(
                ctx => RemoveJobRunDependents(ctx.DbContext, ctx.Entity),
                ctx => QueryCacheInvalidation.InvalidateQueryCachesForBroadEntityTypeAsync<JobRun, JobDefinition>(ctx.Services.GetService<ICacheService>()).GetAwaiter().GetResult(),
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
                            JobBlackoutWindowWriteValidator.ValidateAndNormalize(window);
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
                new() {
                    BeforeCreate = ctx => {
                        ctx.Entity.Id = LyoGuid.CreateCombPostgres();
                        JobBlackoutWindowWriteValidator.ValidateAndNormalize(ctx.Entity);
                    },
                    BeforeUpdate = ctx => {
                        ctx.Entity.UpdatedTimestamp = DateTime.UtcNow;
                        JobBlackoutWindowWriteValidator.ValidateAndNormalize(ctx.Entity);
                    }
                })
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

    /// <summary>Maps the <c>GET /Job/Definition/{id}/Stats</c> route. Invoked by <see cref="BuildJobGroup" />.</summary>
    private static void MapStatsEndpoint(WebApplication app)
        => app.MapGet(
                $"/{Constants.Rest.Job.Definitions}/{{id:guid}}/Stats", async (Guid id, int days, JobService jobService, CancellationToken ct) => {
                    days = days > 0 ? days : 30;
                    var stats = await jobService.GetDefinitionStats(id, days, ct).ConfigureAwait(false);
                    return stats is null ? throw ApiErrorException.From(LyoProblemDetailsBuilder.CreateWithActivity().NotFound("Job definition", id.ToString()).Build()) : Results.Ok(stats);
                })
            .WithTags("Job")
            .WithName("GetJobDefinitionStats");

    /// <summary>Maps the <c>GET /Job/Definition/{id}/NextRuns</c> route. Invoked by <see cref="BuildJobGroup" />.</summary>
    private static void MapNextRunsEndpoint(WebApplication app)
        => app.MapGet(
                $"/{Constants.Rest.Job.Definitions}/{{id:guid}}/NextRuns", async (Guid id, int count, JobService jobService, CancellationToken ct) => {
                    count = count > 0 ? count : 20;
                    var nextRuns = await jobService.GetNextRuns(id, count, ct).ConfigureAwait(false);
                    return Results.Ok(nextRuns);
                })
            .WithTags("Job")
            .WithName("GetJobDefinitionNextRuns");

    /// <summary>Maps the batch <c>POST /Job/Definition/LatestRuns</c> route used by the scheduler's definition refresh. Invoked by <see cref="BuildJobGroup" />.</summary>
    private static void MapDefinitionLatestRunsEndpoint(WebApplication app)
        => app.MapPost(
                $"/{Constants.Rest.Job.DefinitionsLatestRuns}", async (List<Guid> definitionIds, JobService jobService, CancellationToken ct) => {
                    var results = await jobService.GetLatestRuns(definitionIds, ct).ConfigureAwait(false);
                    return Results.Ok(results);
                })
            .WithTags("Job")
            .WithName("GetJobDefinitionLatestRuns");

    /// <summary>
    /// Maps the run lifecycle routes that delegate to <see cref="JobService" /> (Create, Started, Finished, Cancel, Rerun, Resync, Log). <c>Lyo.Job.Scheduler</c> and
    /// <c>Lyo.Job.Worker</c> require these, and <see cref="BuildJobGroup" /> maps them automatically.
    /// </summary>
    private static void MapLifecycleEndpoints(WebApplication app)
    {
        app.MapPost(
                $"/{Constants.Rest.Job.RunsCreate}", async (JobRunReq req, JobService jobService, CancellationToken ct) => {
                    var result = await jobService.CreateJobRun(req, ct).ConfigureAwait(false);
                    // Return CreateResult so ApiClient deserializers (scheduler, worker, and client) receive IsSuccess/Data.
                    return result.IsSuccess ? Results.Created($"/{Constants.Rest.Job.Runs}/{result.Data!.Id}", result) : ProblemResult(result.Error);
                })
            .WithTags("Job")
            .WithName("CreateJobRun");

        app.MapPost(
                $"/{Constants.Rest.Job.Runs}/{{id:guid}}/Started",
                async (Guid id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] JobRunStartedReq? req, JobService jobService, CancellationToken ct) => {
                    var (run, error) = await jobService.StartedJobRun(id, req, ct).ConfigureAwait(false);
                    return error is null ? Results.Ok(run) : ProblemResult(error);
                })
            .WithTags("Job")
            .WithName("StartedJobRun");

        app.MapPost(
                $"/{Constants.Rest.Job.Runs}/{{id:guid}}/Finished", async (Guid id, IReadOnlyList<JobRunResultReq> results, JobService jobService, CancellationToken ct) => {
                    var (run, error) = await jobService.FinishedJobRun(id, results, ct).ConfigureAwait(false);
                    return error is null ? Results.Ok(run) : ProblemResult(error);
                })
            .WithTags("Job")
            .WithName("FinishedJobRun");

        app.MapPost(
                $"/{Constants.Rest.Job.Runs}/{{id:guid}}/Cancel", async (Guid id, JobService jobService, CancellationToken ct) => {
                    var (run, error) = await jobService.CancelJobRun(id, ct).ConfigureAwait(false);
                    return error is null ? Results.Ok(run) : ProblemResult(error);
                })
            .WithTags("Job")
            .WithName("CancelJobRun");

        app.MapPost(
                $"/{Constants.Rest.Job.Runs}/{{id:guid}}/Requeue", async (Guid id, JobService jobService, CancellationToken ct) => {
                    var (run, error) = await jobService.RequeueJobRun(id, ct).ConfigureAwait(false);
                    return error is null ? Results.Ok(run) : ProblemResult(error);
                })
            .WithTags("Job")
            .WithName("RequeueJobRun");

        app.MapPost(
                $"/{Constants.Rest.Job.Runs}/{{id:guid}}/Rerun", async (Guid id, JobService jobService, CancellationToken ct) => {
                    var result = await jobService.RerunJob(id, ct).ConfigureAwait(false);
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
                $"/{Constants.Rest.Job.Runs}/{{id:guid}}/Log", async (Guid id, JobRunLogReq req, JobService jobService, CancellationToken ct) => {
                    var result = await jobService.Log(id, req, ct).ConfigureAwait(false);
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
                        throw ApiErrorException.From(LyoProblemDetails.FromCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidRequest, ex.Message));
                    }
                })
            .WithTags("Job")
            .WithName("CreateChildJobRuns");
    }

    /// <summary>Throws <see cref="ApiErrorException" /> so LoggingMiddleware writes and logs the problem. Uses a generic 400 when no problem was produced.</summary>
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static IResult ProblemResult(LyoProblemDetails? error)
    {
        error ??= LyoProblemDetailsBuilder.CreateWithActivity()
            .WithErrorCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidRequest)
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

    /// <summary>
    /// Invalidates cached JobDefinition queries that include or project this child (editor QueryConcrete, Granular tags, QueryProject grids that never attached the child type
    /// tag).
    /// </summary>
    private static void InvalidateChildDefinitionQueryCaches<TChild>(IServiceProvider services)
        where TChild : class
        => QueryCacheInvalidation.InvalidateQueryCachesForBroadEntityTypeAsync<TChild, JobDefinition>(services.GetService<ICacheService>()).GetAwaiter().GetResult();

    /// <summary>Tells scheduler instances the owning definition changed, because schedule parameters affect run-parameter merging.</summary>
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
        // Keep shared calendars. Delete only when no other schedule still references this calendar.
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
        // An explicit run delete removes the workflow run-steps outright. Retention purge instead keeps them and nulls the reference, which is why this is not in JobRunDependents.
        db.JobWorkflowRunSteps.RemoveRange(db.JobWorkflowRunSteps.Where(s => s.JobRunId == jobRun.Id).ToList());
        JobRunDependents.Remove(db, jobRun);
    }

    private static void NormalizeJobWorkerInstanceTimestamps(JobWorkerInstance entity)
    {
        entity.StartedTimestamp = JobTimestamps.ToUtc(entity.StartedTimestamp);
        entity.LastHeartbeatUtc = JobTimestamps.ToUtc(entity.LastHeartbeatUtc);
        if (entity.CreatedTimestamp != default)
            entity.CreatedTimestamp = JobTimestamps.ToUtc(entity.CreatedTimestamp);

        if (entity.UpdatedTimestamp.HasValue)
            entity.UpdatedTimestamp = JobTimestamps.ToUtc(entity.UpdatedTimestamp.Value);
    }
}
