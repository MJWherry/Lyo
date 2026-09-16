import { describe, expect, it } from "vitest";
import { LyoChipHues } from "lyo-web-components";
import { formatChip, runParameterEntries } from "./gridDisplay.js";

describe("runParameterEntries", () => {
    it("maps key, type, required, value, and description", () => {
        const entries = runParameterEntries({
            parameters: [
                {
                    id: "p1",
                    reportDefinitionId: "d1",
                    key: "StartDate",
                    type: "System.DateOnly",
                    required: true,
                    value: "2026-01-01",
                    description: "Inclusive start",
                },
            ],
        });
        expect(entries).toHaveLength(1);
        expect(entries[0]).toMatchObject({
            value: "2026-01-01",
            definition: { key: "StartDate", type: "System.DateOnly", required: true, description: "Inclusive start" },
        });
    });

    it("returns an empty list when the include is missing", () => {
        expect(runParameterEntries(null)).toEqual([]);
        expect(runParameterEntries({})).toEqual([]);
    });
});

describe("formatChip", () => {
    it("uses the Rose hue for pdf", () => {
        expect(formatChip("pdf")).toMatchObject({ label: "pdf", hue: LyoChipHues.Rose });
    });
});
