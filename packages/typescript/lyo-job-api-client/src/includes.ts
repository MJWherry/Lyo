/** QueryConcrete includes used when opening the Blazor job definition editor. */
export const JOB_DEFINITION_EDITOR_INCLUDES = [
    "JobParameters",
    "JobSchedules.JobScheduleParameters",
    "JobSchedules.JobBlackoutCalendar.JobBlackoutWindows",
    "JobTriggerTriggersJobDefinitions",
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
