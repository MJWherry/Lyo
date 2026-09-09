import {describe, expect, it} from "vitest";
import {canDownloadGeneration, generationStatusColor, projectedId} from "./reportColors.js";

describe("report helpers", () => {
    it("maps generation status", () => {
        expect(generationStatusColor("Succeeded")).toBe("success");
        expect(generationStatusColor("pending")).toBe("warning");
    });

    it("reads ids", () => {
        expect(projectedId({Id: "r1"})).toBe("r1");
    });

    it("allows download when succeeded", () => {
        expect(canDownloadGeneration({Status: "Succeeded", OutputFileId: "f"})).toBe(true);
        expect(canDownloadGeneration({Status: "Failed"})).toBe(false);
    });
});
