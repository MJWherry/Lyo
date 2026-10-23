import type {AutomationPlanContextSnapshot, AutomationRunRes, AutomationRunStepRes} from "lyo-automation-api-client";
import type {AutomationPlan} from "./plan.js";

/** Detail for the step the runner has selected. */
export function selectedStepDetail(run: AutomationRunRes, index: number): {screenshotFileId: string | null; outcome: string; context: AutomationPlanContextSnapshot | null} | null {
    const step = (run.steps ?? [])[index];
    if (!step)
        return null;
    return {screenshotFileId: step.screenshotFileId ?? null, outcome: step.outcome, context: step.context ?? null};
}

/** The latest finished step supplies the overall context bag. */
export function overallContext(run: AutomationRunRes): AutomationPlanContextSnapshot | null {
    const steps = run.steps ?? [];
    return steps.length === 0 ? null : steps[steps.length - 1]?.context ?? null;
}

/** A file-loaded plan can be posted even when it has no stored id. */
export function runnablePlan(plan: AutomationPlan): AutomationPlan {
    return {name: plan.name ?? null, steps: plan.steps};
}

export function stepTitle(step: AutomationRunStepRes): string {
    return step.name?.trim() ? step.name : step.type;
}
