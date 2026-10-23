import {describe, expect, it} from "vitest";
import {layoutSteps, loadDraft, saveDraft, selectStep, type WorkflowStepDraft} from "./workflowGraph.js";

const steps: WorkflowStepDraft[] = [
    {id: "a", stepName: "Fetch", dependsOn: []},
    {id: "b", stepName: "Map", dependsOn: ["a"]},
    {id: "c", stepName: "Notify", dependsOn: ["a"]},
    {id: "d", stepName: "Done", dependsOn: ["b", "c"]},
];

describe("layoutSteps", () => {
    it("places steps left to right and keeps that order", () => {
        const first = layoutSteps(steps);
        const second = layoutSteps(steps);
        expect(first.map((node) => [node.name, node.column, node.row])).toEqual([
            ["Fetch", 0, 0],
            ["Map", 1, 0],
            ["Notify", 1, 1],
            ["Done", 2, 0],
        ]);
        expect(second.map((node) => node.x)).toEqual(first.map((node) => node.x));
        expect(first[1]!.x).toBeGreaterThan(first[0]!.x);
    });
});

describe("selectStep", () => {
    it("returns the node for an id and none when the id is missing", () => {
        const nodes = layoutSteps(steps);
        expect(selectStep(nodes, "b")?.name).toBe("Map");
        expect(selectStep(nodes, "missing")).toBeNull();
    });
});

describe("draft", () => {
    it("round-trips steps, links, and the selected step", () => {
        const draft = {
            steps,
            links: [
                {fromId: "a", toId: "b"},
                {fromId: "a", toId: "c"},
            ],
            selectedStepId: "c",
        };
        expect(loadDraft(saveDraft(draft))).toEqual(draft);
    });
});
