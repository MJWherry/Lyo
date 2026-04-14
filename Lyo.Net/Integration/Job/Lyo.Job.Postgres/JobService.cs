using System.Diagnostics;
using System.Text.Json;
using Lyo.Api.Mapping;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Error;
using ApiErrCodes = Lyo.Api.Models.Constants.ApiErrorCodes;
using Lyo.Api.Services.Crud.Create;
using Lyo.Api.Services.Crud.Read;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Api.Services.Crud.Update;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres.Database;
using Lyo.MessageQueue.RabbitMq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UUIDNext;
using Constants = Lyo.Job.Models.Constants;
using JobRun = Lyo.Job.Postgres.Database.JobRun;
using JobRunLog = Lyo.Job.Postgres.Database.JobRunLog;
using JobRunResult = Lyo.Job.Models.Enums.JobRunResult;

namespace Lyo.Job.Postgres;

public class JobService(
    ILogger<JobService> logger,
    IQueryService<JobContext> queryService,
    ICreateService<JobContext> createService,
    IPatchService<JobContext> patchService,
    ILyoMapper mapper,
    IRabbitMqService mqService,
    IHttpContextAccessor? httpContextAccessor = null)
{
    public async Task<CreateResult<JobRunLogRes>> Log(Guid jobRunId, JobRunLogReq request)
        => await createService.CreateAsync<JobRunLogReq, JobRunLog, JobRunLogRes>(
                request, ctx => {
                    ctx.Entity.Id = Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql);
                    ctx.Entity.JobRunId = jobRunId;
                })
            .ConfigureAwait(false);

    public async Task<CreateResult<JobRunRes>> CreateJobRun(JobRunReq request)
    {
        if (!mqService.IsConnected())
            return ResultFactory.CreateFailure<JobRunRes>(LogAndReturnApiError("Could not connect to Message Queue Service", ApiErrCodes.MessageQueueConnectionIssue));

        var result = await createService.CreateAsync<JobRunReq, JobRun, JobRunRes>(
                request, ctx => {
                    ctx.Entity.Id = Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql);
                    ctx.Entity.State = nameof(JobState.Queued);
                    ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
                    foreach (var j in ctx.Entity.JobRunParameters)
                        j.Id = Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql);
                }, ctx => {
                    ctx.DbContext.Entry(ctx.Entity).Navigation("JobDefinition").Load();
                })
            .ConfigureAwait(false);

        if (!result.IsSuccess)
            return result;

        var notified = await MqCreateJobRun(result.Data!).ConfigureAwait(false);
        return !notified
            ? ResultFactory.CreateFailure<JobRunRes>(LogAndReturnApiError("Could not connect to Message Queue Service", ApiErrCodes.MessageQueueConnectionIssue))
            : result;
    }

    public async Task<(JobRunRes? Result, LyoProblemDetails? Error)> StartedJobRun(Guid jobRunId)
    {
        if (!mqService.IsConnected())
            return (null, LogAndReturnApiError("Could not connect to Message Queue Service", ApiErrCodes.MessageQueueConnectionIssue));

        var existing = await queryService.Get<JobRun, JobRunRes>([jobRunId], ["JobRunParameters"]).ConfigureAwait(false);
        if (existing is null)
            return (null, LogAndReturnApiError("Job run not found", ApiErrCodes.NotFound));

        var patchRequest = PatchRequestBuilder.ForId(jobRunId).SetProperty("State", JobState.Running).SetProperty("StartedTimestamp", DateTime.UtcNow);
        var result = await patchService.PatchAsync<JobRun, JobRunRes>(patchRequest.Build()).ConfigureAwait(false);
        if (!result.IsSuccess)
            return (null, LogAndReturnApiError("Failed to patch start job", ApiErrCodes.InvalidPatchRequest));

        var notified = await MqNotifyStartedJobRun(jobRunId).ConfigureAwait(false);
        if (!notified)
            return (null, LogAndReturnApiError("Could not notify to start job", ApiErrCodes.MessageQueueConnectionIssue));

        return (result.NewData, null);
    }

    public async Task<(JobRunRes? Result, LyoProblemDetails? Error)> CancelJobRun(Guid jobRunId)
    {
        if (!mqService.IsConnected())
            return (null, LogAndReturnApiError("Could not connect to Message Queue Service", ApiErrCodes.MessageQueueConnectionIssue));

        var existing = await queryService.Get<JobRun, JobRunRes>([jobRunId], ["JobRunParameters"]).ConfigureAwait(false);
        if (existing is null)
            return (null, LogAndReturnApiError("Job run not found", ApiErrCodes.NotFound));

        if (existing.State != JobState.Running)
            return (null, LogAndReturnApiError("Job is not in running state", ApiErrCodes.InvalidRequest));

        var notified = await MqCancelJobRun(jobRunId).ConfigureAwait(false);
        if (!notified)
            return (null, LogAndReturnApiError("Could not notify to cancel job", ApiErrCodes.MessageQueueConnectionIssue));

        return (existing, null);
    }

    public async Task<(JobRunRes? Result, LyoProblemDetails? Error)> FinishedJobRun(Guid jobRunId, IReadOnlyList<JobRunResultReq> results)
    {
        if (!mqService.IsConnected())
            return (null, LogAndReturnApiError("Could not connect to Message Queue Service", ApiErrCodes.MessageQueueConnectionIssue));

        var existing = await queryService.Get<JobRun, JobRunRes>([jobRunId], ["JobRunParameters"]).ConfigureAwait(false);
        if (existing is null)
            return (null, LogAndReturnApiError("Job run not found", ApiErrCodes.NotFound));

        if (existing.State != JobState.Running)
            return (null, LogAndReturnApiError("Job is not in running state", ApiErrCodes.InvalidRequest));

        var resultEnum = Enum.Parse<JobRunResult>(results.FirstOrDefault(i => i.Key == Constants.Data.JobRunResultKey.Result)?.Value ?? "Unknown");
        var request = PatchRequestBuilder.ForId(jobRunId)
            .SetProperty("State", JobState.Finished)
            .SetProperty("FinishedTimestamp", DateTime.UtcNow)
            .SetProperty("Result", resultEnum)
            .Build();

        var result = await patchService.PatchAsync<JobRun, JobRunRes>(
                request, ctx => {
                    foreach (var res in results) {
                        var r = mapper.Map<Database.JobRunResult>(res);
                        r.Id = Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql);
                        r.JobRunId = jobRunId;
                        ctx.DbContext.JobRunResults.Add(r);
                    }
                })
            .ConfigureAwait(false);

        if (!result.IsSuccess)
            return (null, LogAndReturnApiError("Failed to patch finished job", ApiErrCodes.InvalidPatchRequest));

        var notified = await MqFinishJobRun(jobRunId).ConfigureAwait(false);
        if (!notified)
            return (null, LogAndReturnApiError("Could not notify to finish job", ApiErrCodes.MessageQueueConnectionIssue));

        var savedResult = await queryService.Get<JobRun, JobRunRes>(
                [jobRunId],
                [
                    "JobRunResults", "JobRunLogs", "JobRunParameters", "JobTrigger", "JobSchedule", "JobDefinition", "JobDefinition.JobSchedules",
                    "JobDefinition.JobTriggerTriggersJobDefinitions"
                ])
            .ConfigureAwait(false);

        return (savedResult, null);
    }

    public async Task<CreateResult<JobRunRes>?> RerunJob(Guid jobRunId)
    {
        if (!mqService.IsConnected())
            return ResultFactory.CreateFailure<JobRunRes>(LogAndReturnApiError("Could not connect to Message Queue Service", ApiErrCodes.MessageQueueConnectionIssue));

        var existing = await queryService.Get<JobRun, JobRunRes>([jobRunId], ["JobRunParameters"]).ConfigureAwait(false);
        if (existing is null)
            return ResultFactory.CreateFailure<JobRunRes>(LogAndReturnApiError("Existing job not found", ApiErrCodes.NotFound));

        var request = mapper.Map<JobRunReq>(existing);
        request.CreatedBy = httpContextAccessor?.HttpContext?.User.Identity?.Name ?? "Unknown";
        request.Result = null;
        request.JobScheduleId = null;
        request.JobTriggerId = null;
        request.TriggeredByJobRunId = null;
        request.ReRanFromJobRunId = jobRunId;
        var result = await createService.CreateAsync<JobRunReq, JobRun, JobRunRes>(
                request, ctx => {
                    ctx.Entity.Id = Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql);
                    ctx.Entity.State = nameof(JobState.Queued);
                    ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
                    foreach (var j in ctx.Entity.JobRunParameters)
                        j.Id = Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql);
                }, ctx => {
                    ctx.DbContext.Entry(ctx.Entity).Navigation("JobDefinition").Load();
                })
            .ConfigureAwait(false);

        if (!result.IsSuccess)
            return result;

        var notified = await MqCreateJobRun(result.Data!).ConfigureAwait(false);
        return !notified ? ResultFactory.CreateFailure<JobRunRes>(LogAndReturnApiError("Could not notify to create job", ApiErrCodes.MessageQueueConnectionIssue)) : result;
    }

    private LyoProblemDetails LogAndReturnApiError(string message, string code = ApiErrCodes.Unknown, LogLevel level = LogLevel.Warning)
    {
        logger.Log(level, message);
        return LyoProblemDetailsBuilder.CreateWithTrace(Activity.Current?.TraceId.ToString(), Activity.Current?.SpanId.ToString())
            .WithErrorCode(code)
            .WithMessage(message)
            .Build();
    }

    private async Task<bool> MqCreateJobRun(JobRunRes jobRun)
    {
        var queue = Constants.Mq.QueueGetJobRunCreated(jobRun.JobDefinition!.WorkerType);
        try {
            logger.LogDebug("Sending job {JobRunId} to job queue {JobQueueName}", jobRun.Id, queue);
            await mqService.SendToQueue(queue, JsonSerializer.SerializeToUtf8Bytes(jobRun.Id)).ConfigureAwait(false);
            await mqService.SendToExchange(Constants.Mq.JobEventExchange, Constants.Mq.JobRunCreatedRoutingKey, JsonSerializer.SerializeToUtf8Bytes(jobRun.Id))
                .ConfigureAwait(false);

            return true;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Couldn't send {JobRunId} to queue {JobQueueName}", jobRun.Id, queue);
            return false;
        }
    }

    private async Task<bool> MqCancelJobRun(Guid jobRunId)
    {
        try {
            logger.LogDebug("Canceling job {JobRunId} ", jobRunId);
            await mqService.SendToExchange(Constants.Mq.JobEventExchange, Constants.Mq.JobRunCancelledRoutingKey, JsonSerializer.SerializeToUtf8Bytes(jobRunId))
                .ConfigureAwait(false);

            return true;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Couldn't cancel {JobRunId}", jobRunId);
            return false;
        }
    }

    private async Task<bool> MqNotifyStartedJobRun(Guid jobRunId)
    {
        try {
            logger.LogDebug("Sending job {JobRunId} to exchange {JobExchangeName}", jobRunId, Constants.Mq.JobRunStartedRoutingKey);
            await mqService.SendToExchange(Constants.Mq.JobEventExchange, Constants.Mq.JobRunStartedRoutingKey, JsonSerializer.SerializeToUtf8Bytes(jobRunId))
                .ConfigureAwait(false);

            return true;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Sending job {JobRunId} to exchange {JobExchangeName}", jobRunId, Constants.Mq.JobRunStartedRoutingKey);
            return false;
        }
    }

    private async Task<bool> MqFinishJobRun(Guid jobRunId)
    {
        try {
            logger.LogDebug("Sending job {JobRunId} to job queue {JobQueueName}", jobRunId, Constants.Mq.QueueJobRunFinish);
            await mqService.SendToQueue(Constants.Mq.QueueJobRunFinish, JsonSerializer.SerializeToUtf8Bytes(jobRunId)).ConfigureAwait(false);
            await mqService.SendToExchange(Constants.Mq.JobEventExchange, Constants.Mq.JobRunFinishedRoutingKey, JsonSerializer.SerializeToUtf8Bytes(jobRunId))
                .ConfigureAwait(false);

            return true;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Couldn't send {JobRunId} to queue {JobQueueName}", jobRunId, Constants.Mq.QueueJobRunFinish);
            return false;
        }
    }
}