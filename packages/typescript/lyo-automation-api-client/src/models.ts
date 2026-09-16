/** Body of `POST Automation/Run`. `plan` is the automation document. */
export type AutomationRunReq = {
    plan: unknown;
    browserKind?: string | null;
    headless?: boolean | null;
};

/** `202` body. */
export type AutomationRunAccepted = {
    runId: string;
    status: "queued";
};

/** Variables and context bag after one step. */
export type AutomationPlanContextSnapshot = {
    strings?: Record<string, string> | null;
    stringLists?: Record<string, string[]> | null;
    contextItems?: Record<string, string | null> | null;
    elementRefs?: string[] | null;
};

/** One finished step on a run. */
export type AutomationRunStepRes = {
    index: number;
    name?: string | null;
    type: string;
    outcome: string;
    durationMs: number;
    error?: string | null;
    screenshotFileId?: string | null;
    context?: AutomationPlanContextSnapshot | null;
};

/** `GET Automation/Run/{id}` body. */
export type AutomationRunRes = {
    runId: string;
    status: "queued" | "running" | "completed" | "failed" | "cancelled";
    error?: string | null;
    strings?: Record<string, string> | null;
    stringLists?: Record<string, string[]> | null;
    steps?: AutomationRunStepRes[] | null;
};

export function isSettled(status: AutomationRunRes["status"]): boolean {
    return status === "completed" || status === "failed" || status === "cancelled";
}
