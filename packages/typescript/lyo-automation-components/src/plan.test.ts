import {describe, expect, it} from "vitest";
import {createEmptyPlan, createStep, formStepTypes, hasForm, insertNavigate, moveStep, planFileName, removeStep, roundTrip, stepLabel} from "./plan.js";
import {overallContext, runnablePlan, selectedStepDetail} from "./runModel.js";

describe("automation plan draft", () => {
    it("InsertStep_AppendsNavigate", () => {
        const plan = insertNavigate({name: "demo", steps: []}, "https://example.com/start");
        expect(plan.name).toBe("demo");
        expect(plan.steps).toEqual([{type: "navigate", url: "https://example.com/start"}]);
    });

    it("MoveStep_Reorders", () => {
        const plan = moveStep({steps: [{type: "navigate", url: "https://a.example"}, {type: "reload"}]}, 1, 0);
        expect(plan.steps.map((step) => step.type)).toEqual(["reload", "navigate"]);
    });

    it("RoundTrip_PreservesUnknownStepType", () => {
        const json = JSON.stringify({
            name: "keep",
            steps: [{type: "httpRequest", method: "GET", url: "https://example.com/hook"}],
        });
        const plan = JSON.parse(roundTrip(json)) as {steps: Array<{type: string; method: string; url: string}>};
        expect(plan.steps[0]?.type).toBe("httpRequest");
        expect(plan.steps[0]?.method).toBe("GET");
        expect(plan.steps[0]?.url).toBe("https://example.com/hook");
        expect(hasForm(plan.steps[0]!)).toBe(true);
    });

    it("CreateEmptyPlan_HasTheNameAndNoSteps", () => {
        expect(createEmptyPlan("Checkout")).toEqual({name: "Checkout", steps: []});
    });

    it("PlanFileName_IsOneJsonSegment", () => {
        expect(planFileName("Checkout")).toBe("Checkout.json");
        expect(planFileName("Checkout.json")).toBe("Checkout.json");
        expect(planFileName("nested/Checkout")).toMatch(/^[^/\\]+\.json$/);
        expect(planFileName("../secret")).not.toMatch(/[/\\]/);
    });

    it("FormCatalog_IncludesCaptureDownloadAndStringList", () => {
        expect(hasForm({type: "captureNetwork"})).toBe(true);
        expect(hasForm({type: "downloadUrls"})).toBe(true);
        expect(hasForm({type: "downloadUrlsToDirectory"})).toBe(true);
        expect(hasForm({type: "storeStringList"})).toBe(true);
        expect(hasForm({type: "extractSources"})).toBe(true);
        expect(stepLabel("captureNetwork")).toBe("Capture network");
        expect(stepLabel("downloadUrlsToDirectory")).toBe("Download URLs");
    });

    it("Catalog_CoversEveryRunnerStep", () => {
        expect(formStepTypes).toContain("httpRequest");
        expect(formStepTypes).toContain("invokeDiMethod");
        expect(formStepTypes).toHaveLength(33);
        for (const type of formStepTypes) {
            const step = createStep(type);
            expect(step.type === "downloadUrlsToDirectory" || step.type === type).toBe(true);
            expect(hasForm(step)).toBe(true);
        }
        expect(createStep("reload")).toEqual({type: "reload"});
        expect(createStep("closeCurrentTab")).toEqual({type: "closeCurrentTab"});
    });

    it("SelectedStep_ShowsThatStepsContext", () => {
        const run = {
            runId: "run-1",
            status: "running" as const,
            steps: [
                {index: 0, type: "storeLiteral", outcome: "Success", durationMs: 12, screenshotFileId: "snap-1", context: {strings: {greeting: "hello"}}},
                {index: 1, type: "reload", outcome: "Failed", durationMs: 4, screenshotFileId: "snap-2", context: {strings: {greeting: "hello"}, contextItems: {note: "x"}}},
            ],
        };
        expect(selectedStepDetail(run, 1)).toEqual({screenshotFileId: "snap-2", outcome: "Failed", context: run.steps[1]?.context});
        expect(overallContext(run)?.strings).toEqual({greeting: "hello"});
        expect(runnablePlan({name: "file", steps: [{type: "navigate", url: "https://example.com"}]}).steps).toHaveLength(1);
    });

    it("RemoveStep_DropsThatIndex", () => {
        const plan = {steps: [{type: "navigate", url: "https://a"}, {type: "delay", delayMilliseconds: 1}]};
        expect(removeStep(plan, 0).steps.map((step) => step.type)).toEqual(["delay"]);
    });
});
