/** REST paths from `Lyo.Job.Models.Constants.Rest.Job` (no leading slash). */
export const JobRoutes = {
    route: "Job",
    definitions: "Job/Definition",
    definitionsQuery: "Job/Definition/QueryConcrete",
    definitionsLatestRuns: "Job/Definition/LatestRuns",
    definitionParameters: "Job/Definition/Parameter",
    schedules: "Job/Schedule",
    scheduleParameters: "Job/ScheduleParameters",
    triggers: "Job/Triggers",
    triggerParameters: "Job/TriggerParameters",
    runs: "Job/Run",
    runsCreate: "Job/Run/Create",
    runsResync: "Job/Run/Resync",
    runsQuery: "Job/Run/QueryConcrete",
    runLogs: "Job/Run/Log",
    runParameters: "Job/Run/Parameter",
    runResults: "Job/Run/Result",
    files: "Job/Run/Files",
    workerInstances: "Job/WorkerInstance",
    blackoutCalendars: "Job/BlackoutCalendar",
    blackoutWindows: "Job/BlackoutCalendar/Window",
    workflows: "Job/Workflow",
    workflowSteps: "Job/Workflow/Step",
    workflowRuns: "Job/Workflow/Run",
    workflowRunSteps: "Job/Workflow/Run/Step",
} as const;

export function jobRunStartedPath(runId: string): string {
    return `${JobRoutes.runs}/${runId}/Started`;
}

export function jobRunFinishedPath(runId: string): string {
    return `${JobRoutes.runs}/${runId}/Finished`;
}

export function jobRunRequeuePath(runId: string): string {
    return `${JobRoutes.runs}/${runId}/Requeue`;
}

export function jobRunCancelPath(runId: string): string {
    return `${JobRoutes.runs}/${runId}/Cancel`;
}

export function jobRunRerunPath(runId: string): string {
    return `${JobRoutes.runs}/${runId}/Rerun`;
}

export function jobRunLogPath(runId: string): string {
    return `${JobRoutes.runs}/${runId}/Log`;
}

export function jobRunChildrenPath(parentRunId: string): string {
    return `${JobRoutes.runs}/${parentRunId}/Children`;
}

export function jobRunHeartbeatPath(runId: string): string {
    return `${JobRoutes.runs}/${runId}/Heartbeat`;
}

export function jobDefinitionStatsPath(definitionId: string): string {
    return `${JobRoutes.definitions}/${definitionId}/Stats`;
}

export function jobDefinitionNextRunsPath(definitionId: string): string {
    return `${JobRoutes.definitions}/${definitionId}/NextRuns`;
}

export function joinRoutePrefix(routePrefix: string | undefined, relativePath: string): string {
    const relative = relativePath.replace(/^\/+/, "");
    const prefix = (routePrefix ?? "").replace(/\/+$/, "").replace(/^\/+/, "");
    return prefix ? `/${prefix}/${relative}` : `/${relative}`;
}
