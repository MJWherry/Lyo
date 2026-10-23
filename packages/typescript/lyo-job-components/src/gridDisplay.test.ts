import { describe, expect, it } from "vitest";
import { enabledChip, jobFormatterContext, jobStateChip, runParameterEntries } from "./gridDisplay.js";

describe("runParameterEntries", () => {
    it("maps key, type, required, value, and description", () => {
        const entries = runParameterEntries({
            jobParameters: [
                {
                    id: "p1",
                    jobDefinitionId: "d1",
                    key: "Region",
                    type: "System.String",
                    required: true,
                    value: "east",
                    description: "Area",
                    enabled: true,
                },
            ],
        });
        expect(entries).toHaveLength(1);
        expect(entries[0]).toMatchObject({
            value: "east",
            definition: { key: "Region", type: "System.String", required: true, description: "Area" },
        });
    });

    it("returns an empty list when the include is missing", () => {
        expect(runParameterEntries(null)).toEqual([]);
        expect(runParameterEntries({})).toEqual([]);
    });
});

describe("jobFormatterContext", () => {
    it("exposes the definition and parameter values on the run bag", () => {
        expect(jobFormatterContext({Name: "Sync"}, [{key: "Region", value: "east"}])).toMatchObject({
            Definition: {Name: "Sync"},
            jobrun: {Parameters: {Region: "east"}},
            LastRun: {State: "Queued"},
        });
    });

    it("uses latest, last successful, and last failed runs when the route returned them", () => {
        const context = jobFormatterContext({Name: "Sync"}, [], {
            lastRun: {State: "Succeeded"},
            lastSuccessfulRun: {State: "Succeeded"},
            lastFailedRun: {State: "Failed"},
        });
        expect(context.LastRun).toMatchObject({State: "Succeeded"});
        expect(context.jobrun).toMatchObject({State: "Succeeded", Parameters: {}});
        expect(context.LastSuccessfulRun).toMatchObject({State: "Succeeded"});
        expect(context.LastFailedRun).toMatchObject({State: "Failed"});
    });
});

describe("job chips", () => {
    it("maps enabled true to Enabled / success and running to warning", () => {
        expect(enabledChip(true)).toMatchObject({ label: "Enabled", color: "success" });
        expect(jobStateChip("running")).toMatchObject({ label: "running", color: "warning" });
    });
});
