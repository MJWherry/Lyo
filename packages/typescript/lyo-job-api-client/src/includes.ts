/** QueryConcrete includes used when opening the job definition editor. Matches `JobDefinitionEditorQuery` after triggers were dropped. */
export const JOB_DEFINITION_EDITOR_INCLUDES = [
    "JobParameters",
    "JobSchedules.JobScheduleParameters",
    "JobSchedules.JobBlackoutCalendar.JobBlackoutWindows",
] as const;

/** Includes `JobWorkflowClient.GetRunAsync` always requests so the graph can be drawn. */
export const JOB_WORKFLOW_RUN_GRAPH_INCLUDES = [
    "JobWorkflowRunSteps",
    "JobWorkflowRunSteps.JobWorkflowStep",
    "JobWorkflow",
    "JobWorkflow.JobWorkflowSteps",
] as const;

export const JOB_RUN_DETAIL_INCLUDES = [
    "JobDefinition",
    "JobRunLogs",
    "JobRunResults",
    "JobRunParameters",
    "JobSchedule",
] as const;

export const JOB_DEFINITION_GRID_SELECT = [
    "Id",
    "Name",
    "Type",
    "WorkerType",
    "Enabled",
    "Priority",
] as const;

export const JOB_RUN_GRID_SELECT = [
    "Id",
    "JobDefinitionId",
    "State",
    "Result",
    "CreatedTimestamp",
    "StartedTimestamp",
    "FinishedTimestamp",
    "ProgressPercent",
    "WorkerMachineName",
] as const;
