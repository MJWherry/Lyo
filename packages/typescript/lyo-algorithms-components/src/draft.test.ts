import {describe, expect, it} from "vitest";
import {contextObject, emptyDraft, isIdentifier, issues, readWorkbenchState, toPayload} from "./draft.js";
import {scheduleContext, scheduleDraft} from "./presets.js";

describe("toPayload", () => {
  it("toPayload_Expression_WritesTypeMap", () => {
    const payload = JSON.parse(toPayload(scheduleDraft())!) as {result: string; parameters: Record<string, string>};
    expect(payload.result).toBe("decimal");
    expect(payload.parameters.Periods).toBe("int");
    expect(payload.parameters.Dates).toBe("datetime[]");
    expect(payload.parameters.Rate).toBe("decimal");
  });

  it("toPayload_StringMapZero_IncludesMissingValue", () => {
    const draft = emptyDraft();
    draft.kind = "discreteMap";
    draft.result = "string";
    draft.input = "Code";
    draft.missing = "Zero";
    draft.missingValue = "none";
    draft.entries = [{key: "a", value: "yes"}];
    const payload = JSON.parse(toPayload(draft)!) as {missingValue: string; entries: Array<{points: string}>};
    expect(payload.missingValue).toBe("none");
    expect(payload.entries[0]!.points).toBe("yes");
  });
});

describe("issues", () => {
  it("issues_StringMapWithoutMissingValue_ReportsIt", () => {
    const draft = emptyDraft();
    draft.kind = "discreteMap";
    draft.result = "string";
    draft.missing = "Zero";
    expect(issues(draft).join(" ")).toContain("missingValue");
  });

  it("issues_ChildRefAndInline_ReportsXor", () => {
    const draft = emptyDraft();
    draft.kind = "composite";
    draft.children = [{ref: "board.type", kind: "discreteMap", weight: "1"}];
    expect(issues(draft).join(" ")).toContain("not both");
  });

  it("isIdentifier_DottedName_IsFalse", () => {
    expect(isIdentifier("fish.type")).toBe(false);
    expect(isIdentifier("fish_type")).toBe(true);
  });
});

describe("contextObject", () => {
  it("contextObject_IntAndArray_UsesJsonTypes", () => {
    const context = contextObject(scheduleContext());
    expect(context.Periods).toBe(4);
    expect(context.Dates).toEqual(["2020-01-01T00:00:00"]);
  });
});

describe("readWorkbenchState", () => {
  it("readWorkbenchState_RestoresDraftAndBag", () => {
    const raw = JSON.stringify({draft: scheduleDraft(), context: scheduleContext()});
    const state = readWorkbenchState(raw);
    expect(state?.draft.result).toBe("decimal");
    expect(state?.draft.source).toContain("Convert.ToDecimal");
    expect(state?.context.find((row) => row.name === "Periods")?.text).toBe("4");
  });
});
