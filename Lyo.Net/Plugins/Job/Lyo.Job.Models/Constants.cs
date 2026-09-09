namespace Lyo.Job.Models;

/// <summary>Shared constants for the Job library.</summary>
public static class Constants
{
    /// <summary>Message-queue constants (Mq).</summary>
    public static class Mq
    {
        /// <summary>
        /// Queue for finished-run notifications, consumed by scheduler instances. With multiple schedulers this is a competing-consumer queue: each completion is handled by
        /// exactly one instance. Retry and trigger creation are deduplicated across instances via idempotency keys on the resulting run requests.
        /// </summary>
        public const string QueueJobRunFinish = "job.run.complete";

        /// <summary>
        /// Fan-in exchange for job lifecycle notifications. Declared at API and worker startup when missing (direct, durable). Bindings use the routing keys below.
        /// </summary>
        public const string JobEventExchange = "job.events";

        /// <summary>AMQP type for <see cref="JobEventExchange" />. Direct because every binding uses an exact routing key.</summary>
        public const string JobEventExchangeType = "direct";
        public const string JobDefinitionChangeKey = "job.notifications.definition.updated";
        public const string JobRunCreatedRoutingKey = "job.notifications.run.created";
        public const string JobRunStartedRoutingKey = "job.notifications.run.started";
        public const string JobRunCancelledRoutingKey = "job.notifications.run.cancelled";
        public const string JobRunFinishedRoutingKey = "job.notifications.run.finished";
        public const string JobAlertRoutingKey = "job.notifications.alert";

        /// <summary>Suffix for the RabbitMQ delay-wait companion of a worker dispatch queue (<c>job.run.{workerType}.wait</c>).</summary>
        public const string WaitQueueSuffix = ".wait";

        // Multiple worker types: build the queue from worker type to keep routing simple
        public static string QueueGetJobRunCreated(string workerType) => $"job.run.{workerType}";

        /// <summary>Delay-wait queue for a worker type. Delayed retries sit here until TTL dead-letters them onto <see cref="QueueGetJobRunCreated" />.</summary>
        public static string QueueGetJobRunCreatedWait(string workerType) => QueueGetJobRunCreated(workerType) + WaitQueueSuffix;

        /// <summary>Base name for cancellation queues of a worker type. Kept for backward compatibility and as the prefix for per-instance queues.</summary>
        public static string QueueGetJobRunCancel(string workerType) => $"job.run.{workerType}.cancel";

        /// <summary>
        /// Per-instance cancellation queue name (exclusive, auto-delete). Cancellations are broadcast through <see cref="JobEventExchange" />; each worker instance binds its own
        /// queue so every instance of a scaled-out worker type sees every cancel. A single shared queue would deliver each cancel to only one competing consumer.
        /// </summary>
        public static string QueueGetJobRunCancelInstance(string workerType, string instanceId) => $"job.run.{workerType}.cancel.{instanceId}";
    }

    /// <summary>REST API route names.</summary>
    public static class Rest
    {
        public static class Job
        {
            public const string Route = "Job";
            public const string Definitions = $"{Route}/Definition";
            public const string DefinitionsQuery = $"{Definitions}/QueryConcrete";

            /// <summary>POST endpoint that returns the latest run, latest successful run, and latest failed run per definition id (batch, used by scheduler refresh).</summary>
            public const string DefinitionsLatestRuns = $"{Definitions}/LatestRuns";

            public const string DefinitionParameters = $"{Definitions}/Parameter";
            public const string Schedules = $"{Route}/Schedule";
            public const string ScheduleParameters = $"{Route}/ScheduleParameters";
            public const string Triggers = $"{Route}/Triggers";
            public const string TriggerParameters = $"{Route}/TriggerParameters";
            public const string Runs = $"{Route}/Run";

            /// <summary>POST endpoint that creates a run through <c>JobService.CreateJobRun</c> (not the generic CRUD create).</summary>
            public const string RunsCreate = $"{Runs}/Create";

            /// <summary>POST endpoint that republishes due <c>Queued</c> runs that are missing from the worker RabbitMQ queues.</summary>
            public const string RunsResync = $"{Runs}/Resync";

            public const string RunsQuery = $"{Runs}/QueryConcrete";
            public const string RunLogs = $"{Runs}/Log";
            public const string RunParameters = $"{Runs}/Parameter";
            public const string RunResults = $"{Runs}/Result";
            public const string Files = $"{Runs}/Files";
            public const string WorkerInstances = $"{Route}/WorkerInstance";
            public const string BlackoutCalendars = $"{Route}/BlackoutCalendar";
            public const string BlackoutWindows = $"{Route}/BlackoutCalendar/Window";
            public const string Workflows = $"{Route}/Workflow";
            public const string WorkflowSteps = $"{Route}/Workflow/Step";
            public const string WorkflowRuns = $"{Route}/Workflow/Run";
            public const string WorkflowRunSteps = $"{Route}/Workflow/Run/Step";

            /// <summary>POST endpoint that moves a run to <c>Running</c>.</summary>
            public static string RunStarted(Guid runId) => $"{Runs}/{runId}/Started";

            /// <summary>POST endpoint that moves a run to <c>Finished</c>.</summary>
            public static string RunFinished(Guid runId) => $"{Runs}/{runId}/Finished";

            /// <summary>POST endpoint that moves a run from <c>Running</c> back to <c>Queued</c> (worker host shutdown hand-back).</summary>
            public static string RunRequeue(Guid runId) => $"{Runs}/{runId}/Requeue";

            /// <summary>POST endpoint that adds a log entry to a run.</summary>
            public static string RunLog(Guid runId) => $"{Runs}/{runId}/Log";

            /// <summary>POST endpoint that creates fan-out child runs under a parent batch run.</summary>
            public static string RunChildren(Guid parentRunId) => $"{Runs}/{parentRunId}/Children";

            /// <summary>PATCH endpoint the worker uses to bump <c>LastHeartbeatUtc</c> on a running job.</summary>
            public static string RunHeartbeat(Guid runId) => $"{Runs}/{runId}/Heartbeat";

            /// <summary>GET endpoint for aggregated run statistics on one definition.</summary>
            public static string DefinitionStats(Guid definitionId) => $"{Definitions}/{definitionId}/Stats";

            /// <summary>GET endpoint for the next scheduled run timestamps on one definition.</summary>
            public static string DefinitionNextRuns(Guid definitionId) => $"{Definitions}/{definitionId}/NextRuns";
        }
    }

    /// <summary>Metric names the job system emits (recorded via <c>IMetrics</c> when registered).</summary>
    public static class Metrics
    {
        /// <summary>Metrics from <c>Lyo.Job.Scheduler.JobScheduler</c>.</summary>
        public static class Scheduler
        {
            public const string DefinitionsLoaded = "job.scheduler.definitions.loaded";
            public const string RefreshDuration = "job.scheduler.refresh.duration";
            public const string RefreshError = "job.scheduler.refresh.error";
            public const string CheckDuration = "job.scheduler.check.duration";
            public const string CheckError = "job.scheduler.check.error";
            public const string RunsCreated = "job.scheduler.runs.created";
            public const string RunCreateFailed = "job.scheduler.runs.create.failed";
            public const string SlotConflicts = "job.scheduler.slot.conflicts";
            public const string TriggersFired = "job.scheduler.triggers.fired";
            public const string RetriesScheduled = "job.scheduler.retries.scheduled";
            public const string CircuitBreakerTripped = "job.scheduler.circuit_breaker.tripped";

            /// <summary>The breaker threshold was reached but the definition could not be disabled, so it remains enabled.</summary>
            public const string CircuitBreakerTripFailed = "job.scheduler.circuit_breaker.trip.failed";
            public const string MisfiresCaughtUp = "job.scheduler.misfires.caught_up";
            public const string MisfiresSkipped = "job.scheduler.misfires.skipped";
        }

        /// <summary>Metrics from <c>Lyo.Job.Postgres.JobService</c>.</summary>
        public static class Service
        {
            public const string RunCreated = "job.service.run.created";

            /// <summary>A finish attempt lost the compare-and-swap because the run was no longer Running or Cancelling.</summary>
            public const string RunFinishRejected = "job.service.run.finish.rejected";

            /// <summary>A run started, but its advisory started event could not be published.</summary>
            public const string RunStartedPublishFailed = "job.service.run.started.publish.failed";
            public const string RunCreateRejected = "job.service.run.create.rejected";
            public const string RunDispatchDeferred = "job.service.run.dispatch.deferred";
            public const string RunRequeued = "job.service.run.requeued";
            public const string RunStarted = "job.service.run.started";
            public const string RunStartRejected = "job.service.run.start.rejected";
            public const string RunFinished = "job.service.run.finished";
            public const string RunCancelled = "job.service.run.cancelled";
            public const string RunRerun = "job.service.run.rerun";
            public const string RunResynced = "job.service.run.resynced";
            public const string RunDuration = "job.service.run.duration";
            public const string RunQueueLatency = "job.service.run.queue_latency";
        }

        /// <summary>Metrics from <c>Lyo.Job.Worker.JobWorkerBase</c> (on top of the inherited <c>queue.worker.*</c> metrics).</summary>
        public static class Worker
        {
            public const string RunExecuted = "job.worker.run.executed";
            public const string RunDuration = "job.worker.run.duration";
            public const string HeartbeatSent = "job.worker.heartbeat.sent";
            public const string HeartbeatFailed = "job.worker.heartbeat.failed";
            public const string CancellationHonored = "job.worker.cancellation.honored";
            public const string ProgressReported = "job.worker.progress.reported";
            public const string StartRejected = "job.worker.start.rejected";
            public const string ShutdownRequeued = "job.worker.shutdown.requeued";

            /// <summary>A shutdown hand-back to <c>Queued</c> failed, so the message was acked and the run was left to dead-job detection.</summary>
            public const string ShutdownRequeueFailed = "job.worker.shutdown.requeue.failed";

            /// <summary>A dispatch message pointed at a run that no longer exists, so it was dropped without consuming the requeue budget.</summary>
            public const string RunNotFound = "job.worker.run.not_found";
            public const string LateFinishDropped = "job.worker.late_finish.dropped";
        }

        /// <summary>Metrics from <c>Lyo.Job.Postgres.JobMaintenanceService</c>.</summary>
        public static class Maintenance
        {
            public const string TickDuration = "job.maintenance.tick.duration";
            public const string TickError = "job.maintenance.tick.error";
            public const string DeadJobsFailed = "job.maintenance.dead_jobs.failed";
            public const string CircuitBreakersReset = "job.maintenance.circuit_breakers.reset";
            public const string RunsPurged = "job.maintenance.runs.purged";
            public const string RunsRedispatched = "job.maintenance.runs.redispatched";
            public const string WorkerInstancesPruned = "job.maintenance.worker_instances.pruned";

            /// <summary>A maintenance tick lost an optimistic-concurrency race because a worker updated a run during the pass.</summary>
            public const string ConcurrencyConflict = "job.maintenance.concurrency.conflict";
        }

        /// <summary>Metrics from <c>Lyo.Job.Alerts.JobAlertConsumer</c>.</summary>
        public static class Alerts
        {
            public const string Dispatched = "job.alerts.dispatched";

            /// <summary>An alert was dropped because the same definition/alert-type pair was dispatched inside the dedup window.</summary>
            public const string Suppressed = "job.alerts.suppressed";

            /// <summary>An alert message could not be deserialized and was sent to the DLQ (or dropped when none is configured).</summary>
            public const string Poison = "job.alerts.poison";

            /// <summary>Routing a poison alert message to the DLQ failed, so the payload was discarded.</summary>
            public const string PoisonDlqFailed = "job.alerts.poison.dlq.failed";

            /// <summary>A webhook POST attempt failed; a later attempt may still succeed.</summary>
            public const string WebhookAttemptFailed = "job.alerts.webhook.attempt.failed";

            /// <summary>Every webhook attempt for an alert failed.</summary>
            public const string WebhookFailed = "job.alerts.webhook.failed";

            /// <summary>Dispatch threw, so the message was requeued for another try.</summary>
            public const string DispatchFailed = "job.alerts.dispatch.failed";
        }

        /// <summary>Metrics recorded when job SLA thresholds are breached.</summary>
        public static class Sla
        {
            public const string Breach = "job.sla.breach";
        }
    }

    /// <summary>Built-in keys <c>JobWorkerBase</c> writes to <c>JobWorkerInstanceReq.Metadata</c>.</summary>
    public static class WorkerMetadata
    {
        public const string Os = "os";
        public const string OsPlatform = "osPlatform";
        public const string OsVersion = "osVersion";
        public const string Framework = "framework";
        public const string RuntimeIdentifier = "runtimeIdentifier";
        public const string ClrVersion = "clrVersion";
        public const string ProcessArchitecture = "processArchitecture";
        public const string OsArchitecture = "osArchitecture";
        public const string ProcessorCount = "processorCount";
        public const string CpuModel = "cpuModel";
        public const string TotalPhysicalMemoryBytes = "totalPhysicalMemoryBytes";
        public const string WorkingSetBytes = "workingSetBytes";
        public const string GcHeapBytes = "gcHeapBytes";
        public const string IsServerGc = "isServerGc";
        public const string ProcessName = "processName";
        public const string AssemblyVersion = "assemblyVersion";
        public const string Queue = "queue";
        public const string WaitQueue = "waitQueue";
        public const string CancelQueue = "cancelQueue";
        public const string Dlq = "dlq";
        public const string Subscriptions = "subscriptions";
        public const string MaxRequeueCount = "maxRequeueCount";
        public const string HeartbeatInterval = "heartbeatInterval";
        public const string RequeueDelay = "requeueDelay";

        /// <summary>Host/runtime keys (CPU, memory, OS). Anything else is worker/queue metadata or a host extra.</summary>
        public static readonly string[] SystemKeys = [
            Os, OsPlatform, OsVersion, Framework, RuntimeIdentifier, ClrVersion, ProcessArchitecture, OsArchitecture, ProcessorCount, CpuModel, TotalPhysicalMemoryBytes,
            WorkingSetBytes, GcHeapBytes, IsServerGc, ProcessName, AssemblyVersion
        ];

        /// <summary>True when <paramref name="key" /> is a built-in system-info key.</summary>
        public static bool IsSystemKey(string? key) => key is not null && SystemKeys.Contains(key, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Data keys and identifiers used by job runs.</summary>
    public static class Data
    {
        public static class JobRunResultKey
        {
            public const string Result = "Result";
            public const string ExecutionTime = "ExecutionTime";
            public const string CreateCount = "CreateCount";
            public const string UpdateCount = "UpdateCount";
            public const string DeleteCount = "DeleteCount";
            public const string FailedCount = "FailedCount";
            public const string NoChangeCount = "NoChangeCount";

            public static string Unknown => $"Unknown_{Guid.NewGuid()}";

            public static string FailureReason(object n) => $"FailureReason_{n}";

            public static string FailedItem(object n) => $"FailedItem_{n}";

            public static string ApiCallTime(string name, params string[] other) => $"ApiCallTime_{name}";

            public static string QueryCount(string name, params string[] other) => $"QueryCount_{name}";
        }

        public static class JobRunParameterKey
        {
            public const string JobType = "JobType";
            public const string PaginationAmount = "PaginationAmount";
            public const string DegreeOfParallel = "DegreeOfParallel";
            public const string UpsertChunkSize = "UpsertChunkSize";

            public static string Unknown => $"Unknown_{Guid.NewGuid()}";
        }
    }
}