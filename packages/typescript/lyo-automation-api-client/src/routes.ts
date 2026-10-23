/** REST paths from `Lyo.Web.Automation.Core.Constants.Rest` (no leading slash). */
export const AutomationRoutes = {
    route: "Automation",
    runs: "Automation/Run",
    plans: "Automation/Plan",
    planRecords: "Automation/Plans",
} as const;

export function automationPlanPath(name: string): string {
    return `${AutomationRoutes.plans}/${encodeURIComponent(name)}`;
}

export function automationPlanByIdPath(id: string): string {
    return `${AutomationRoutes.plans}/id/${id}`;
}

export function automationRunPath(runId: string): string {
    return `${AutomationRoutes.runs}/${runId}`;
}

export function automationRunCancelPath(runId: string): string {
    return `${AutomationRoutes.runs}/${runId}/Cancel`;
}

export function automationRunLogPath(runId: string): string {
    return `${AutomationRoutes.runs}/${runId}/Log`;
}

export function automationRunSnapshotsPath(runId: string): string {
    return `${AutomationRoutes.runs}/${runId}/Snapshots`;
}

export function automationRunSnapshotPath(runId: string, name: string): string {
    return `${AutomationRoutes.runs}/${runId}/Snapshot/${encodeURIComponent(name)}`;
}

export function automationRunStepScreenshotPath(runId: string, index: number): string {
    return `${AutomationRoutes.runs}/${runId}/Steps/${index}/Screenshot`;
}
