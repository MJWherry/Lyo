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
            public const string IntegratedBusiness = "IntegratedBusiness";
            public const string ProgrammingLanguage = "Programming Language";
            public const string PaginationAmount = "PaginationAmount";
            public const string DegreeOfParallel = "DegreeOfParallel";
            public const string UpsertChunkSize = "UpsertChunkSize";

            //for email jobs
            public const string EmailToPrefix = "EmailTo_";
            public const string EmailCcPrefix = "EmailCc_";
            public const string EmailBccPrefix = "EmailBcc_";
            public const string EmailSubject = "EmailSubject";
            public const string EmailBody = "EmailBody";
            public const string EmailAttachmentPrefix = "EmailAttachment_";
            public const string EmailAttachmentNamePrefix = "EmailAttachmentName_";
            public const string ReportId = "ReportId";

            //for filewatcher for triggering jobs
            public const string FileNamePrefix = "FileName_";
            public const string FileNameRegexPrefix = "FileNameRegex_";
            public const string FileDirectoryPrefix = "FileDirectory_";

            public static string Unknown => $"Unknown_{Guid.NewGuid()}";
        }
    }
}