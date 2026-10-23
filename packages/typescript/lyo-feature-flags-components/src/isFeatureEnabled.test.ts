import {describe, expect, it} from "vitest";
import {isFeatureEnabled} from "./isFeatureEnabled.js";

describe("isFeatureEnabled", () => {
    it("matches_OrdinalIgnoreCase", () => {
        expect(isFeatureEnabled(["sample"], "Sample")).toBe(true);
        expect(isFeatureEnabled(["billing"], "sample")).toBe(false);
    });
});
