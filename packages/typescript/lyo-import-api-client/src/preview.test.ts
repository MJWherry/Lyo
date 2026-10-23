import {describe, expect, it} from "vitest";
import {emptyPreview, showPreview, tryAddField} from "./preview.js";

describe("import preview", () => {
    it("tryAddField_DuplicateTarget_Rejects", () => {
        const once = tryAddField(emptyPreview(), {source: "given", target: "Name"});
        const twice = tryAddField(once, {source: "family", target: "Name"});
        expect(twice.fields).toEqual([{source: "given", target: "Name"}]);
        expect(twice.blockedReason).toBe("Two sources map to 'Name'.");
    });

    it("show_CapsPreviewAt50_KeepsEveryIssue", () => {
        const rows = Array.from({length: 60}, (_, index) => ({name: `r${index}`}));
        const issues = [
            {index: 1, message: "a"},
            {index: 2, message: "b"},
            {index: 3, message: "c"},
        ];
        const shown = showPreview(rows, issues, (row) => row.name);
        expect(shown.preview).toHaveLength(50);
        expect(shown.preview[0]).toBe("r0");
        expect(shown.issues).toEqual(issues);
    });
});
