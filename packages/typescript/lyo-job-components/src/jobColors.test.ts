import {describe, expect, it} from "vitest";
import {canCancelRun, jobStateColor, projectedId} from "./jobColors.js";

describe("job helpers", () => {
    it("maps states", () => {
        expect(jobStateColor("Running")).toBe("warning");
        expect(jobStateColor("queued")).toBe("info");
    });

    it("reads projected ids from PascalCase or camelCase", () => {
        expect(projectedId({Id: "abc"})).toBe("abc");
        expect(projectedId({id: "xyz"})).toBe("xyz");
    });

    it("allows cancel on queued/running", () => {
        expect(canCancelRun({State: "Queued"})).toBe(true);
        expect(canCancelRun({State: "Finished"})).toBe(false);
    });
});
