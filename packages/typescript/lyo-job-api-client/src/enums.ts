/** Wire JSON enum names (`JsonStringEnumConverter` + camelCase). */
export type JobState = "unknown" | "queued" | "running" | "finished" | "cancelled" | "cancelling";

export type JobRunResult =
    | "unknown"
    | "success"
    | "successWithWarnings"
    | "partialSuccess"
    | "failure"
    | "cancelled"
    | "skipped"
    | "timeout";

export type JobLogLevel =
    | "unknown"
    | "trace"
    | "debug"
    | "information"
    | "warning"
    | "error"
    | "critical";

export type JobWorkerInstanceState = "unknown" | "running" | "draining" | "stopped";

export type JobRetryBackoffType = "linear" | "exponential";

export type JobMisfirePolicy = "skip" | "runOnce";

export type JobBlackoutPolicy = "skip" | "defer";

export type JobWorkflowFailurePolicy = "stop" | "continue";

export type JobWorkflowRunState = "pending" | "running" | "finished" | "failed" | "cancelled";

export type JobWorkflowStepState = "pending" | "running" | "finished" | "failed" | "skipped";
