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

        //Multiple worker types, build queue based on worker type to simplify
        public static string QueueGetJobRunCreated(string workerType) => $"job.run.{workerType}";
    }

    /// <summary>REST API route constants.</summary>
    public static class Rest
    {
        public static class Job
        {
            public const string Route = "Job";
            public const string Definitions = $"{Route}/Definition";
            public const string DefinitionsQuery = $"{Definitions}/Query";
            public const string DefinitionParameters = $"{Definitions}/Parameter";
            public const string Schedules = $"{Route}/Schedule";
            public const string ScheduleParameters = $"{Route}/ScheduleParameters";
            public const string Triggers = $"{Route}/Triggers";
            public const string TriggerParameters = $"{Route}/TriggerParameters";
            public const string Runs = $"{Route}/Run";
            public const string RunsQuery = $"{Runs}/Query";
            public const string RunLogs = $"{Runs}/Log";
            public const string RunParameters = $"{Runs}/Parameter";
            public const string RunResults = $"{Runs}/Result";
            public const string Files = $"{Runs}/Files";

            /// <summary>POST endpoint to transition a run to <c>Running</c> state.</summary>
            public static string RunStarted(Guid runId) => $"{Runs}/{runId}/Started";

            /// <summary>POST endpoint to transition a run to <c>Finished</c> state.</summary>
            public static string RunFinished(Guid runId) => $"{Runs}/{runId}/Finished";

            /// <summary>POST endpoint to add a log entry to a run.</summary>
            public static string RunLog(Guid runId) => $"{Runs}/{runId}/Log";

            /// <summary>PATCH endpoint for the worker to bump <c>LastHeartbeatUtc</c> on a running job.</summary>
            public static string RunHeartbeat(Guid runId) => $"{Runs}/{runId}/Heartbeat";

            /// <summary>GET endpoint for aggregated run statistics on a definition.</summary>
            public static string DefinitionStats(Guid definitionId) => $"{Definitions}/{definitionId}/Stats";
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