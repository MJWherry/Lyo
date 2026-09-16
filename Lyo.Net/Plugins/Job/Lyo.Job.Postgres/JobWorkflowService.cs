using System.Diagnostics;
using System.Text.Json;
using Lyo.Api.Mapping;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Error;
using Lyo.Api.Services.Crud.Create;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Cache;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres.Database;
using Lyo.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ApiErrCodes = Lyo.Api.Models.Constants.ApiErrorCodes;
using Constants = Lyo.Job.Models.Constants;

namespace Lyo.Job.Postgres;

/// <summary>
/// Starts workflow runs. A workflow run is materialized here as a <see cref="JobWorkflowRunState.Pending" /> run plus one <see cref="JobWorkflowStepState.Pending" /> run step
/// per enabled step; the <c>JobWorkflowEngine</c> picks it up from the published created-event and dispatches the roots. Nothing in this service dispatches job runs itself.
/// </summary>
public sealed class JobWorkflowService(
    ILogger<JobWorkflowService> logger,
    ICreateService<JobContext> createService,
    ILyoMapper mapper,
    IJobEventPublisher eventPublisher,
    IDbContextFactory<JobContext> dbFactory,
    IMetrics? metrics = null,
    ICacheService? cache = null)
{
    private const string IdempotencyKeyUniqueConstraint = "ix_job_workflow_run_idempotency_key_unique";
    private const string ScheduleSlotUniqueConstraint = "ix_job_workflow_run_schedule_slot_unique";

    private readonly IMetrics _metrics = metrics ?? NullMetrics.Instance;

    /// <summary>
    /// Materializes a run for <paramref name="workflowId" /> and publishes the created-event that gets the engine moving. Safe to retry when
    /// <see cref="JobWorkflowStartReq.IdempotencyKey" /> is set: a repeat resolves to the run the first call created rather than starting the workflow twice.
    /// </summary>
    /// <param name="workflowId">Workflow to start.</param>
    /// <param name="request">Start options. Null is treated as an empty request (start once, no de-duplication).</param>
    /// <param name="ct">Token used to cancel the start.</param>
    /// <returns>The created (or already-existing) workflow run, or a failure when the workflow is missing, disabled, or has nothing to run.</returns>
    public async Task<CreateResult<JobWorkflowRunRes>> StartWorkflowAsync(Guid workflowId, JobWorkflowStartReq? request = null, CancellationToken ct = default)
    {
        request ??= new JobWorkflowStartReq();
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var workflow = await db.JobWorkflows.AsNoTracking().Include(w => w.JobWorkflowSteps).FirstOrDefaultAsync(w => w.Id == workflowId, ct).ConfigureAwait(false);
        if (workflow is null)
            return Rejected("Job workflow not found", ApiErrCodes.NotFound, "workflow_not_found");

        if (!workflow.Enabled)
            return Rejected($"Job workflow '{workflow.Name}' is disabled.", ApiErrCodes.Conflict, "workflow_disabled");

        var steps = workflow.JobWorkflowSteps.Where(s => s.Enabled).OrderBy(s => s.StepOrder).ToList();

        // A run with zero steps would never finalize: finalization is driven by step completions, and there would be none.
        if (steps.Count == 0)
            return Rejected($"Job workflow '{workflow.Name}' has no enabled steps.", ApiErrCodes.InvalidRequest, "workflow_no_enabled_steps");

        // Concurrent starts of the same workflow are serialized on a transaction-scoped advisory lock so the idempotency lookup below cannot
        // race a second insert. The lock is released when the transaction commits.
        await using var guardTx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", [BitConverter.ToInt64(workflowId.ToByteArray(), 0)], ct).ConfigureAwait(false);
        var alreadyStarted = await FindExistingRunAsync(db, workflowId, request, ct).ConfigureAwait(false);
        if (alreadyStarted is not null) {
            await guardTx.CommitAsync(CancellationToken.None).ConfigureAwait(false);
            return ResultFactory.CreateSuccess(alreadyStarted);
        }

        var inputsJson = await ResolveInputsAsync(db, request, ct).ConfigureAwait(false);

        CreateResult<JobWorkflowRunRes> result;
        try {
            result = await CreateRunAsync(workflowId, request, steps, inputsJson, ct).ConfigureAwait(false);
        }
        finally {
            await guardTx.CommitAsync(CancellationToken.None).ConfigureAwait(false);
        }

        // CreateService swallows the unique-violation DbUpdateException and returns a failure result, so a start that lost the race has to be
        // recovered from the failure text rather than a catch. The advisory lock above only serializes starts inside one database; a second
        // Postgres connection pool, or a schedule slot raced by two hosts, still reaches the index.
        if (!result.IsSuccess && MentionsDedupeConstraint(result.Error)) {
            var existing = await FindExistingRunAsync(db, workflowId, request, ct).ConfigureAwait(false);
            if (existing is not null)
                return ResultFactory.CreateSuccess(existing);
        }

        if (!result.IsSuccess)
            return result;

        await QueryCacheInvalidation.InvalidateQueryCachesForBroadEntityTypeAsync<JobWorkflowRun, JobWorkflowRunStep>(cache).ConfigureAwait(false);
        _metrics.IncrementCounter(Constants.Metrics.Workflow.RunCreated, tags: [("workflow", workflow.Name)]);

        // The run is committed as Pending. A failed publish leaves it there rather than losing it: the engine picks Pending runs up again on
        // the next start, and operators can see a run that never moved.
        try {
            await eventPublisher.PublishWorkflowRunCreatedAsync(result.Data!.Id, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            _metrics.IncrementCounter(Constants.Metrics.Workflow.RunDispatchDeferred);
            logger.LogError(ex, "Workflow run {WorkflowRunId} was created but the created-event publish failed; the run will stay Pending until it is redispatched", result.Data!.Id);
        }

        return result;
    }

    /// <summary>
    /// Freezes the values this run branches on: the schedule's enabled parameters as the base, the caller's inputs layered over them. Resolving once here rather than per
    /// step is what stops an edit to a schedule parameter mid-run from flipping a gate that already went the other way.
    /// </summary>
    /// <returns>Serialized flat string map, or null when the start supplied nothing and the schedule declares nothing.</returns>
    private static async Task<string?> ResolveInputsAsync(JobContext db, JobWorkflowStartReq start, CancellationToken ct)
    {
        var inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (start.JobScheduleId is { } scheduleId) {
            var parameters = await db.JobScheduleParameters.AsNoTracking()
                .Where(p => p.JobScheduleId == scheduleId && p.Enabled)
                .Select(p => new { p.Key, p.Value })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var parameter in parameters)
                inputs[parameter.Key] = parameter.Value ?? "";
        }

        foreach (var input in start.Inputs ?? new Dictionary<string, string>())
            inputs[input.Key] = input.Value;

        return inputs.Count == 0 ? null : JsonSerializer.Serialize(inputs);
    }

    private Task<CreateResult<JobWorkflowRunRes>> CreateRunAsync(
        Guid workflowId,
        JobWorkflowStartReq start,
        IReadOnlyList<JobWorkflowStep> steps,
        string? inputsJson,
        CancellationToken ct)
    {
        var runId = LyoGuid.CreateCombPostgres();
        var request = new JobWorkflowRunReq {
            JobWorkflowId = workflowId,
            State = JobWorkflowRunState.Pending,
            IdempotencyKey = start.IdempotencyKey,
            JobScheduleId = start.JobScheduleId,
            ScheduledSlotUtc = start.ScheduledSlotUtc,
            InputsJson = inputsJson,
            CreateRunSteps = [
                .. steps.Select(
                    s => new JobWorkflowRunStepReq {
                        JobWorkflowRunId = runId,
                        JobWorkflowStepId = s.Id,
                        State = JobWorkflowStepState.Pending
                    })
            ]
        };

        return createService.CreateAsync<JobWorkflowRunReq, JobWorkflowRun, JobWorkflowRunRes>(
            request, ctx => {
                ctx.Entity.Id = runId;
                foreach (var runStep in ctx.Entity.JobWorkflowRunSteps) {
                    if (runStep.Id == default)
                        runStep.Id = LyoGuid.CreateCombPostgres();

                    runStep.JobWorkflowRunId = runId;
                }
            }, ct: ct);
    }

    /// <summary>
    /// Finds the run a repeated start should resolve to. Two independent de-duplication keys are honored: the caller's idempotency key, and the schedule slot the workflow
    /// scheduler supplies. A start carrying neither always creates a new run.
    /// </summary>
    private async Task<JobWorkflowRunRes?> FindExistingRunAsync(JobContext db, Guid workflowId, JobWorkflowStartReq request, CancellationToken ct)
    {
        var runs = db.JobWorkflowRuns.AsNoTracking().Include(r => r.JobWorkflowRunSteps);
        JobWorkflowRun? run = null;
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            run = await runs.FirstOrDefaultAsync(r => r.JobWorkflowId == workflowId && r.IdempotencyKey == request.IdempotencyKey, ct).ConfigureAwait(false);

        if (run is null && request.JobScheduleId is { } scheduleId && request.ScheduledSlotUtc is { } slot)
            run = await runs.FirstOrDefaultAsync(r => r.JobScheduleId == scheduleId && r.ScheduledSlotUtc == slot, ct).ConfigureAwait(false);

        return run is null ? null : mapper.Map<JobWorkflowRunRes>(run);
    }

    private static bool MentionsDedupeConstraint(LyoProblemDetails? error)
    {
        var message = error?.GetFullMessage();
        return message is not null
            && (message.Contains(IdempotencyKeyUniqueConstraint, StringComparison.OrdinalIgnoreCase)
                || message.Contains(ScheduleSlotUniqueConstraint, StringComparison.OrdinalIgnoreCase));
    }

    private CreateResult<JobWorkflowRunRes> Rejected(string message, string code, string metricReason)
    {
        _metrics.IncrementCounter(Constants.Metrics.Workflow.RunCreateRejected, tags: [("reason", metricReason)]);
        logger.LogWarning("{Message}", message);
        var problem = LyoProblemDetailsBuilder.CreateWithTrace(Activity.Current?.TraceId.ToString(), Activity.Current?.SpanId.ToString())
            .WithErrorCode(code)
            .WithMessage(message)
            .Build();

        return ResultFactory.CreateFailure<JobWorkflowRunRes>(problem);
    }
}
