namespace Lyo.Job.Models;

/// <summary>Consolidated constants for the Job library.</summary>
public static class Constants
{
    /// <summary>Message queue constants (Mq).</summary>
    public static class Mq
    {
        //Only 1 scheduler, so 1 queue to process finished jobs
        public const string QueueJobRunFinish = "job.run.complete";

        //Exchange for job events below
        public const string JobEventExchange = "job.events";
        public const string JobDefinitionChangeKey = "job.notifications.definition.updated";
        public const string JobRunCreatedRoutingKey = "job.notifications.run.created";
        public const string JobRunStartedRoutingKey = "job.notifications.run.started";
        public const string JobRunCancelledRoutingKey = "job.notifications.run.cancelled";
        public const string JobRunFinishedRoutingKey = "job.notifications.run.finished";
        public const string JobAlertRoutingKey = "job.notifications.alert";

        //Multiple worker types, build queue based on worker type to simplify
        public static string QueueGetJobRunCreated(string workerType) => $"job.run.{workerType}";

        public static string QueueGetJobRunCancel(string workerType) => $"job.run.{workerType}.cancel";
    }

    /// <summary>REST API route constants.</summary>
    public static class Rest
    {
        public static class Job
        {
            public const string Route = "Job";
            public const string Definitions = $"{Route}/Definition";
            public const string DefinitionsQuery = $"{Definitions}/QueryConcrete";
            public const string DefinitionParameters = $"{Definitions}/Parameter";
            public const string Schedules = $"{Route}/Schedule";
            public const string ScheduleParameters = $"{Route}/ScheduleParameters";
            public const string Triggers = $"{Route}/Triggers";
            public const string TriggerParameters = $"{Route}/TriggerParameters";
            public const string Runs = $"{Route}/Run";
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

            /// <summary>POST endpoint to transition a run to <c>Running</c> state.</summary>
            public static string RunStarted(Guid runId) => $"{Runs}/{runId}/Started";

            /// <summary>POST endpoint to transition a run to <c>Finished</c> state.</summary>
            public static string RunFinished(Guid runId) => $"{Runs}/{runId}/Finished";

            /// <summary>POST endpoint to add a log entry to a run.</summary>
            public static string RunLog(Guid runId) => $"{Runs}/{runId}/Log";

            /// <summary>POST endpoint to create fan-out child runs under a parent batch run.</summary>
            public static string RunChildren(Guid parentRunId) => $"{Runs}/{parentRunId}/Children";

            /// <summary>PATCH endpoint for the worker to bump <c>LastHeartbeatUtc</c> on a running job.</summary>
            public static string RunHeartbeat(Guid runId) => $"{Runs}/{runId}/Heartbeat";

            /// <summary>GET endpoint for aggregated run statistics on a definition.</summary>
            public static string DefinitionStats(Guid definitionId) => $"{Definitions}/{definitionId}/Stats";
        }
    }

    /// <summary>Metric names emitted by the job system (recorded via <c>IMetrics</c> when registered).</summary>
    public static class Metrics
    {
        /// <summary>Metrics emitted by <c>Lyo.Job.Scheduler.JobScheduler</c>.</summary>
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
            public const string MisfiresCaughtUp = "job.scheduler.misfires.caught_up";
            public const string MisfiresSkipped = "job.scheduler.misfires.skipped";
        }

        /// <summary>Metrics emitted by <c>Lyo.Job.Postgres.JobService</c>.</summary>
        public static class Service
        {
            public const string RunCreated = "job.service.run.created";
            public const string RunCreateRejected = "job.service.run.create.rejected";
            public const string RunStarted = "job.service.run.started";
            public const string RunFinished = "job.service.run.finished";
            public const string RunCancelled = "job.service.run.cancelled";
            public const string RunRerun = "job.service.run.rerun";
            public const string RunDuration = "job.service.run.duration";
            public const string RunQueueLatency = "job.service.run.queue_latency";
        }

        /// <summary>Metrics emitted by <c>Lyo.Job.Worker.JobWorkerBase</c> (in addition to the inherited <c>queue.worker.*</c> metrics).</summary>
        public static class Worker
        {
            public const string RunExecuted = "job.worker.run.executed";
            public const string RunDuration = "job.worker.run.duration";
            public const string HeartbeatSent = "job.worker.heartbeat.sent";
            public const string HeartbeatFailed = "job.worker.heartbeat.failed";
            public const string CancellationHonored = "job.worker.cancellation.honored";
            public const string ProgressReported = "job.worker.progress.reported";
        }

        /// <summary>Metrics emitted by <c>Lyo.Job.Postgres.JobMaintenanceService</c>.</summary>
        public static class Maintenance
        {
            public const string TickDuration = "job.maintenance.tick.duration";
            public const string TickError = "job.maintenance.tick.error";
            public const string DeadJobsFailed = "job.maintenance.dead_jobs.failed";
            public const string CircuitBreakersReset = "job.maintenance.circuit_breakers.reset";
            public const string RunsPurged = "job.maintenance.runs.purged";
            public const string WorkerInstancesPruned = "job.maintenance.worker_instances.pruned";
        }

        /// <summary>Metrics emitted when job SLA thresholds are breached.</summary>
        public static class Sla
        {
            public const string Breach = "job.sla.breach";
        }
    }

    /// <summary>Data keys and identifiers.</summary>
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

            // Application-domain-specific constants below.
            // These belong in the consuming application, not in this shared library.
            // Define your own constants alongside the job definition that uses them.

            [Obsolete("Define application-specific parameter keys in the consuming application, not in this shared library.")]
            public const string IntegratedBusiness = "IntegratedBusiness";

            [Obsolete("Define application-specific parameter keys in the consuming application, not in this shared library.")]
            public const string ProgrammingLanguage = "Programming Language";

            [Obsolete("Define email parameter keys in the consuming application, not in this shared library.")]
            public const string EmailToPrefix = "EmailTo_";

            [Obsolete("Define email parameter keys in the consuming application, not in this shared library.")]
            public const string EmailCcPrefix = "EmailCc_";

            [Obsolete("Define email parameter keys in the consuming application, not in this shared library.")]
            public const string EmailBccPrefix = "EmailBcc_";

            [Obsolete("Define email parameter keys in the consuming application, not in this shared library.")]
            public const string EmailSubject = "EmailSubject";

            [Obsolete("Define email parameter keys in the consuming application, not in this shared library.")]
            public const string EmailBody = "EmailBody";

            [Obsolete("Define email parameter keys in the consuming application, not in this shared library.")]
            public const string EmailAttachmentPrefix = "EmailAttachment_";

            [Obsolete("Define email parameter keys in the consuming application, not in this shared library.")]
            public const string EmailAttachmentNamePrefix = "EmailAttachmentName_";

            [Obsolete("Define email parameter keys in the consuming application, not in this shared library.")]
            public const string ReportId = "ReportId";

            [Obsolete("Define file-watcher parameter keys in the consuming application, not in this shared library.")]
            public const string FileNamePrefix = "FileName_";

            [Obsolete("Define file-watcher parameter keys in the consuming application, not in this shared library.")]
            public const string FileNameRegexPrefix = "FileNameRegex_";

            [Obsolete("Define file-watcher parameter keys in the consuming application, not in this shared library.")]
            public const string FileDirectoryPrefix = "FileDirectory_";

            public static string Unknown => $"Unknown_{Guid.NewGuid()}";
        }
    }
}