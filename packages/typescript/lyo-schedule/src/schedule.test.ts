import {describe, expect, it} from "vitest";
import {intervalPreview} from "./schedule.js";

describe("schedule preview", () => {
    it("schedule_Interval_PreviewsNextOccurrences", () => {
        const from = new Date("2026-03-01T00:00:00.000Z");
        const hits = intervalPreview({type: "Interval", intervalMinutes: 1440, executeAt: from.toISOString()}, from, 2026, 3);
        expect(hits[0]).toBe("2026-03-01T00:00:00.000Z");
        expect(hits[1]).toBe("2026-03-02T00:00:00.000Z");
        expect(hits.at(-1)).toBe("2026-03-31T00:00:00.000Z");
        expect(hits).toHaveLength(31);
    });
});
