export const NODE_WIDTH = 200;
export const NODE_HEIGHT = 64;
export const COLUMN_GAP = 96;
export const ROW_GAP = 20;

export type WorkflowStepDraft = {
    id: string;
    stepName: string;
    dependsOn: string[];
    failurePolicy?: string;
    parametersJson?: string | null;
    jobDefinitionId?: string;
};

export type WorkflowGraphNode = {
    id: string;
    name: string;
    column: number;
    row: number;
    x: number;
    y: number;
};

export type WorkflowDraft = {
    steps: WorkflowStepDraft[];
    links: Array<{fromId: string; toId: string}>;
    selectedStepId: string | null;
};

export function layoutSteps(steps: readonly WorkflowStepDraft[]): WorkflowGraphNode[] {
    const named = steps.filter((step) => step.id && step.stepName.trim());
    const byId = new Map(named.map((step) => [step.id, step]));
    const remaining = [...named];
    const placed = new Set<string>();
    const levels: WorkflowStepDraft[][] = [];
    while (remaining.length > 0) {
        const ready = remaining.filter((step) =>
            step.dependsOn.filter((id) => byId.has(id)).every((id) => placed.has(id))
        );
        if (ready.length === 0) break;
        levels.push(ready);
        for (const step of ready) {
            placed.add(step.id);
            const index = remaining.indexOf(step);
            if (index >= 0) remaining.splice(index, 1);
        }
    }
    const nodes: WorkflowGraphNode[] = [];
    levels.forEach((level, column) => {
        level.forEach((step, row) => {
            nodes.push({
                id: step.id,
                name: step.stepName,
                column,
                row,
                x: column * (NODE_WIDTH + COLUMN_GAP),
                y: row * (NODE_HEIGHT + ROW_GAP),
            });
        });
    });
    return nodes;
}

export function selectStep(nodes: readonly WorkflowGraphNode[], id: string | null | undefined): WorkflowGraphNode | null {
    if (!id) return null;
    return nodes.find((node) => node.id === id) ?? null;
}

export function saveDraft(draft: WorkflowDraft): string {
    return JSON.stringify(draft);
}

export function loadDraft(raw: string): WorkflowDraft {
    const parsed = JSON.parse(raw) as WorkflowDraft;
    return {
        steps: parsed.steps.map((step) => ({...step, dependsOn: [...step.dependsOn]})),
        links: parsed.links.map((link) => ({...link})),
        selectedStepId: parsed.selectedStepId,
    };
}

export function linksFromSteps(steps: readonly WorkflowStepDraft[]): Array<{fromId: string; toId: string}> {
    const ids = new Set(steps.map((step) => step.id));
    const links: Array<{fromId: string; toId: string}> = [];
    for (const step of steps) {
        for (const fromId of step.dependsOn) {
            if (ids.has(fromId)) links.push({fromId, toId: step.id});
        }
    }
    return links;
}
