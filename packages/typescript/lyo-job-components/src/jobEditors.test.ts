import {describe, expect, it} from "vitest";
import {canCancelRun} from "./jobColors.js";
import {
    blackoutKind,
    linkBlackout,
    runCreateError,
    runCreateRequest,
    statisticSeries,
    unlinkBlackout,
} from "./jobEditors.js";

describe("statisticSeries", () => {
    it("groups rows into named series by state", () => {
        const series = statisticSeries([
            {successCount: 2, failureCount: 1},
            {SuccessCount: 3, TimeoutCount: 1},
        ]);
        expect(series).toEqual([
            {name: "success", count: 5},
            {name: "failure", count: 1},
            {name: "timeout", count: 1},
        ]);
    });
});

describe("blackoutKind", () => {
    it("classifies weekday, range, and holiday windows", () => {
        expect(blackoutKind({dayFlags: 1})).toBe("weekday");
        expect(blackoutKind({startDateUtc: "2026-01-01"})).toBe("range");
        expect(blackoutKind({holidaySlug: "christmas", startDateUtc: "2026-12-25"})).toBe("holiday");
    });

    it("links and unlinks a window's calendar by schedule", () => {
        const schedule = {jobDefinitionId: "d1", jobBlackoutCalendarId: null};
        const linked = linkBlackout(schedule, "cal-1");
        expect(linked.jobBlackoutCalendarId).toBe("cal-1");
        expect(unlinkBlackout(linked).jobBlackoutCalendarId).toBeNull();
    });
});

describe("runCreateRequest", () => {
    it("blocks an empty required parameter and sends the typed list", () => {
        const params = [
            {key: "Region", value: "", required: true, type: "System.String"},
            {key: "Limit", value: "10", required: false, type: "System.Int32"},
        ];
        expect(runCreateError("def-1", params)).toBe("Region is required.");
        const filled = [{...params[0]!, value: "east"}, params[1]!];
        expect(runCreateError("def-1", filled)).toBeNull();
        expect(runCreateRequest("def-1", "ui", filled)).toEqual({
            jobDefinitionId: "def-1",
            createdBy: "ui",
            jobScheduleId: null,
            jobRunParameters: [
                {key: "Region", value: "east", type: "System.String"},
                {key: "Limit", value: "10", type: "System.Int32"},
            ],
        });
    });
});

describe("canCancelRun", () => {
    it("stays true only for queued and running", () => {
        expect(canCancelRun({State: "queued"})).toBe(true);
        expect(canCancelRun({State: "running"})).toBe(true);
        expect(canCancelRun({State: "finished"})).toBe(false);
    });
});
