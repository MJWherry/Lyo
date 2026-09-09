import type {
    JobBlackoutPolicy,
    JobLogLevel,
    JobMisfirePolicy,
    JobRetryBackoffType,
    JobRunResult,
    JobState,
    JobWorkerInstanceState,
    JobWorkflowFailurePolicy,
    JobWorkflowRunState,
    JobWorkflowStepState,
} from "../enums.js";

export interface JobParameterReq {
    jobDefinitionId?: string;
    key?: string;
    description?: string | null;
    type?: string;
    value?: string | null;
    encryptedValue?: string | null;
    enabled?: boolean;
    required?: boolean;
    validationRegex?: string | null;
    minLength?: number | null;
    maxLength?: number | null;
    allowedValues?: string | null;
    options?: string | null;
    order?: number;
    defaultKind?: string;
    defaultTemplate?: string | null;
}

export interface JobParameterRes {
    id: string;
    jobDefinitionId: string;
    key: string;
    description?: string | null;
    type: string;
    value?: string | null;
    encryptedValue?: string | null;
    enabled: boolean;
    required: boolean;
    validationRegex?: string | null;
    minLength?: number | null;
    maxLength?: number | null;
    allowedValues?: string | null;
    options?: string | null;
    order?: number;
    defaultKind?: string;
    defaultTemplate?: string | null;
}

export interface JobScheduleParameterReq {
    jobScheduleId?: string;
    key?: string;
    type?: string;
    value?: string | null;
    description?: string | null;
    encryptedValue?: string | null;
    enabled?: boolean;
}

export interface JobScheduleParameterRes {
    id: string;
    jobScheduleId: string;
    key: string;
    type: string;
    value?: string | null;
    description?: string | null;
    encryptedValue?: string | null;
    enabled?: boolean;
}

export interface JobTriggerParameterReq {
    jobTriggerId?: string;
    key?: string;
    type?: string;
    value?: string | null;
    description?: string | null;
}

export interface JobTriggerParameterRes {
    id: string;
    jobTriggerId: string;
    key: string;
    type: string;
    value?: string | null;
    description?: string | null;
}

export interface JobBlackoutWindowReq {
    jobBlackoutCalendarId?: string;
    name: string;
    dayFlags?: unknown;
    startDateUtc?: string | null;
    endDateUtc?: string | null;
    startTime?: string;
    endTime?: string;
    policy?: JobBlackoutPolicy;
    enabled?: boolean;
    holidaySlug?: string | null;
    includeObservedDate?: boolean;
    monthFlags?: unknown;
    daysOfMonth?: number[] | null;
}

export interface JobBlackoutWindowRes {
    id: string;
    jobBlackoutCalendarId: string;
    name: string;
    dayFlags?: unknown;
    startTime?: string;
    endTime?: string;
    policy: JobBlackoutPolicy;
    enabled: boolean;
    startDateUtc?: string | null;
    endDateUtc?: string | null;
    holidaySlug?: string | null;
    includeObservedDate?: boolean;
    monthFlags?: unknown;
    daysOfMonth?: number[] | null;
}

export interface JobBlackoutCalendarReq {
    name: string;
    description?: string | null;
    enabled?: boolean;
    createBlackoutWindows?: JobBlackoutWindowReq[];
}

export interface JobBlackoutCalendarRes {
    id: string;
    name: string;
    description?: string | null;
    enabled: boolean;
    blackoutWindows?: JobBlackoutWindowRes[] | null;
}

export interface JobScheduleReq {
    jobDefinitionId: string;
    monthFlags?: unknown;
    dayFlags?: unknown;
    type?: unknown;
    times?: string[] | null;
    startTime?: string | null;
    endTime?: string | null;
    intervalMinutes?: number | null;
    cronExpression?: string | null;
    misfirePolicy?: JobMisfirePolicy;
    startDateUtc?: string | null;
    endDateUtc?: string | null;
    timeZoneId?: string | null;
    jobBlackoutCalendarId?: string | null;
    createBlackoutCalendar?: JobBlackoutCalendarReq | null;
    description?: string | null;
    enabled?: boolean;
    createScheduleParameters?: JobScheduleParameterReq[];
}

export interface JobScheduleRes {
    id: string;
    jobDefinitionId: string;
    monthFlags?: unknown;
    dayFlags?: unknown;
    type?: unknown;
    times?: string[] | null;
    startTime?: string | null;
    endTime?: string | null;
    intervalMinutes?: number | null;
    description?: string | null;
    enabled: boolean;
    parameters?: JobScheduleParameterRes[] | null;
    cronExpression?: string | null;
    misfirePolicy?: JobMisfirePolicy;
    startDateUtc?: string | null;
    endDateUtc?: string | null;
    timeZoneId?: string | null;
    jobBlackoutCalendarId?: string | null;
    jobBlackoutCalendar?: JobBlackoutCalendarRes | null;
}

export interface JobTriggerReq {
    jobDefinitionId: string;
    triggersJobDefinitionId: string;
    jobResultKey: string;
    comparison?: string;
    jobResultValue?: string | null;
    description?: string | null;
    enabled?: boolean;
    createTriggerParameters?: JobTriggerParameterReq[];
}

export interface JobTriggerRes {
    id: string;
    triggersJobDefinitionId: string;
    jobResultKey: string;
    comparison?: string;
    jobResultValue?: string | null;
    description?: string | null;
    enabled: boolean;
    jobDefinition?: JobDefinitionRes | null;
    triggerParameters?: JobTriggerParameterRes[] | null;
    triggersJobDefinition?: JobDefinitionRes | null;
}

export interface JobParallelRestrictionReq {
    jobDefinitionId?: string;
    [key: string]: unknown;
}

export interface JobParallelRestrictionRes {
    id: string;
    jobDefinitionId: string;
    [key: string]: unknown;
}

export interface JobDefinitionReq {
    name: string;
    description?: string | null;
    type: string;
    workerType: string;
    enabled?: boolean;
    maxRetryCount?: number;
    retryBackoffSeconds?: number;
    timeoutMinutes?: number;
    maxConcurrentRuns?: number;
    circuitBreakerThreshold?: number;
    circuitBreakerResetMinutes?: number;
    retryBackoffType?: JobRetryBackoffType;
    priority?: number;
    retentionDays?: number;
    maxRunsPerHour?: number;
    expectedDurationMinutes?: number;
    mustStartByMinutes?: number;
    alertOnFailure?: boolean;
    alertAfterConsecutiveFailures?: number;
    alertWebhookUrl?: string | null;
    jobBlackoutCalendarId?: string | null;
    createBlackoutCalendar?: JobBlackoutCalendarReq | null;
    createParameters?: JobParameterReq[];
    createSchedules?: JobScheduleReq[];
    createTriggers?: JobTriggerReq[];
    createParallelRestrictions?: JobParallelRestrictionReq[];
}

export interface JobDefinitionRes {
    id: string;
    name: string;
    description?: string | null;
    type: string;
    workerType: string;
    enabled: boolean;
    jobParameters?: JobParameterRes[] | null;
    jobSchedules?: JobScheduleRes[] | null;
    jobTriggers?: JobTriggerRes[] | null;
    jobParallelRestrictions?: JobParallelRestrictionRes[] | null;
    maxRetryCount?: number;
    retryBackoffSeconds?: number;
    timeoutMinutes?: number;
    maxConcurrentRuns?: number;
    circuitBreakerThreshold?: number;
    circuitBreakerResetMinutes?: number;
    circuitBreakerTrippedAt?: string | null;
    retryBackoffType?: JobRetryBackoffType;
    priority?: number;
    retentionDays?: number;
    maxRunsPerHour?: number;
    expectedDurationMinutes?: number;
    mustStartByMinutes?: number;
    alertOnFailure?: boolean;
    alertAfterConsecutiveFailures?: number;
    alertWebhookUrl?: string | null;
    definitionVersion?: number;
}

export interface JobDefinitionStatsRes {
    jobDefinitionId: string;
    totalRuns: number;
    successCount: number;
    failureCount: number;
    successRate?: number | null;
    avgDurationMs?: number | null;
    p95DurationMs?: number | null;
    lastRunAt?: string | null;
    lastSuccessAt?: string | null;
    consecutiveFailures: number;
    runningCount: number;
    queuedCount: number;
    windowDays: number;
}

export interface JobDefinitionLatestRunsRes {
    jobDefinitionId: string;
    lastRun?: JobRunRes | null;
    lastSuccessfulRun?: JobRunRes | null;
    lastFailedRun?: JobRunRes | null;
}

export interface JobRunParameterReq {
    key: string;
    type?: string;
    value?: string | null;
    description?: string | null;
    encryptedValue?: string | null;
    enabled?: boolean;
}

export interface JobRunParameterRes {
    id: string;
    jobRunId: string;
    key: string;
    type: string;
    value?: string | null;
    description?: string | null;
    encryptedValue?: string | null;
    enabled: boolean;
}

export interface JobRunResultReq {
    key: string;
    type?: string;
    value?: string | null;
}

export interface JobRunResultRes {
    id: string;
    jobRunId: string;
    key: string;
    type: string;
    value?: string | null;
}

export interface JobRunLogReq {
    level: JobLogLevel;
    message: string;
    context?: string | null;
    stackTrace?: string | null;
    timestamp: string;
}

export interface JobRunLogRes {
    id: string;
    jobRunId: string;
    level: JobLogLevel;
    message: string;
    context?: string | null;
    stackTrace?: string | null;
    timestamp: string;
}

export interface JobRunReq {
    jobDefinitionId: string;
    jobScheduleId?: string | null;
    jobTriggerId?: string | null;
    triggeredByJobRunId?: string | null;
    reRanFromJobRunId?: string | null;
    createdBy: string;
    allowTriggers?: boolean;
    result?: JobRunResult | null;
    scheduledSlotUtc?: string | null;
    retryAttempt?: number;
    priority?: number | null;
    idempotencyKey?: string | null;
    suppressDispatch?: boolean;
    dryRun?: boolean;
    traceId?: string | null;
    parentJobRunId?: string | null;
    batchIndex?: number | null;
    batchTotal?: number | null;
    jobRunParameters?: JobRunParameterReq[];
}

export interface JobRunStartedReq {
    workerInstanceId?: string | null;
    machineName?: string | null;
    processId?: number | null;
}

export interface JobRunHeartbeatReq {
    progressPercent?: number | null;
    progressMessage?: string | null;
}

export interface JobChildRunSpec {
    batchIndex: number;
    parameters?: JobRunParameterReq[];
}

export interface JobCreateChildRunsReq {
    children: JobChildRunSpec[];
}

export interface JobRunRes {
    id: string;
    state: JobState;
    result?: JobRunResult | null;
    createdTimestamp: string;
    startedTimestamp?: string | null;
    finishedTimestamp?: string | null;
    jobRunParameters?: JobRunParameterRes[] | null;
    jobScheduleId?: string | null;
    jobSchedule?: JobScheduleRes | null;
    allowTriggers: boolean;
    jobTriggerId?: string | null;
    jobTrigger?: JobTriggerRes | null;
    jobRunResults?: JobRunResultRes[] | null;
    jobDefinitionId: string;
    jobDefinition?: JobDefinitionRes | null;
    reRanFromJobRun?: JobRunRes | null;
    jobRunLogs?: JobRunLogRes[] | null;
    scheduledSlotUtc?: string | null;
    retryAttempt?: number;
    lastHeartbeatUtc?: string | null;
    priority?: number;
    progressPercent?: number | null;
    progressMessage?: string | null;
    idempotencyKey?: string | null;
    dryRun?: boolean;
    slaBreached?: boolean;
    traceId?: string | null;
    parentJobRunId?: string | null;
    batchIndex?: number | null;
    batchTotal?: number | null;
    definitionAuditVersion?: number | null;
    workerInstanceId?: string | null;
    workerMachineName?: string | null;
    workerProcessId?: number | null;
}

export interface JobRunResyncRes {
    queued: number;
    alreadyInQueue: number;
    republished: number;
    failed: number;
    truncated: boolean;
}

export interface JobWorkerInstanceReq {
    workerType: string;
    machineName: string;
    processId: number;
    state?: JobWorkerInstanceState;
    inFlightCount?: number;
    startedTimestamp: string;
    lastHeartbeatUtc: string;
    metadata?: Record<string, string | null> | null;
}

export interface JobWorkerInstanceRes {
    id: string;
    workerType: string;
    machineName: string;
    processId: number;
    state: JobWorkerInstanceState;
    inFlightCount: number;
    startedTimestamp: string;
    lastHeartbeatUtc: string;
    createdTimestamp: string;
    updatedTimestamp?: string | null;
    metadata?: Record<string, string | null> | null;
}

export interface JobWorkflowStepReq {
    jobWorkflowId?: string;
    jobDefinitionId: string;
    stepName: string;
    stepOrder: number;
    dependsOnStepIds?: string | null;
    failurePolicy?: JobWorkflowFailurePolicy;
    parametersJson?: string | null;
    enabled?: boolean;
}

export interface JobWorkflowStepRes {
    id: string;
    jobWorkflowId: string;
    jobDefinitionId: string;
    stepName: string;
    stepOrder: number;
    dependsOnStepIds?: string | null;
    failurePolicy: JobWorkflowFailurePolicy;
    parametersJson?: string | null;
    enabled: boolean;
    jobDefinition?: JobDefinitionRes | null;
}

export interface JobWorkflowReq {
    name: string;
    description?: string | null;
    enabled?: boolean;
    createSteps?: JobWorkflowStepReq[];
}

export interface JobWorkflowRes {
    id: string;
    name: string;
    description?: string | null;
    enabled: boolean;
    steps?: JobWorkflowStepRes[] | null;
}

export interface JobWorkflowRunReq {
    jobWorkflowId: string;
    [key: string]: unknown;
}

export interface JobWorkflowRunRes {
    id: string;
    jobWorkflowId: string;
    state?: JobWorkflowRunState;
    [key: string]: unknown;
}

export interface JobWorkflowRunStepReq {
    jobWorkflowRunId?: string;
    [key: string]: unknown;
}

export interface JobWorkflowRunStepRes {
    id: string;
    jobWorkflowRunId: string;
    state?: JobWorkflowStepState;
    [key: string]: unknown;
}
