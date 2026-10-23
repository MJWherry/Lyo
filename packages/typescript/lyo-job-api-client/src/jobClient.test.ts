import {describe, expect, it} from "vitest";
import {createApiClient, createAsyncApiClient, type ApiResponse, type ApiTransport, type AsyncApiTransport} from "lyo-api-client";
import {createAsyncJobApiClient} from "./job/asyncJobClient.js";
import {createJobApiClient} from "./job/jobClient.js";
import {JOB_DEFINITION_EDITOR_INCLUDES, JOB_WORKFLOW_RUN_GRAPH_INCLUDES} from "./includes.js";
import {emptyConcreteQuery} from "./job/queryBuilders.js";
import {JobRoutes, joinRoutePrefix} from "./routes.js";

function ok<T>(data: T): ApiResponse<T> {
    return {status: 200, ok: true, data};
}

describe("joinRoutePrefix", () => {
    it("adds a leading slash without prefix", () => {
        expect(joinRoutePrefix(undefined, "Job/Definition")).toBe("/Job/Definition");
    });

    it("joins a prefix", () => {
        expect(joinRoutePrefix("api", "Job/Run")).toBe("/api/Job/Run");
    });
});

describe("createAsyncJobApiClient", () => {
    it("posts QueryConcrete and lifecycle routes", async () => {
        const calls: Array<{method: string; url: string; body?: string}> = [];
        const transport: AsyncApiTransport = async (req) => {
            calls.push({method: req.method, url: req.url, body: req.body});
            return ok({isSuccess: true, items: [], queryScore: 0});
        };
        const api = createAsyncApiClient({baseUrl: "http://x", transport});
        const jobs = createAsyncJobApiClient(api);

        await jobs.definitions.queryConcrete(emptyConcreteQuery(10));
        await jobs.runs.create({jobDefinitionId: "d1", createdBy: "tester"});
        await jobs.runs.cancel("run-1");
        await jobs.definitions.stats("def-1", 7);

        expect(calls[0]).toMatchObject({
            method: "POST",
            url: "http://x/Job/Definition/QueryConcrete",
        });
        expect(JSON.parse(calls[0]!.body!)).toMatchObject({Amount: 10});
        expect(calls[1]).toMatchObject({method: "POST", url: "http://x/Job/Run/Create"});
        expect(calls[2]).toMatchObject({method: "POST", url: "http://x/Job/Run/run-1/Cancel"});
        expect(calls[3]).toMatchObject({
            method: "GET",
            url: "http://x/Job/Definition/def-1/Stats?days=7",
        });
        expect(JobRoutes.workflows).toBe("Job/Workflow");
        expect([...JOB_DEFINITION_EDITOR_INCLUDES]).toEqual([
            "JobParameters",
            "JobSchedules.JobScheduleParameters",
            "JobSchedules.JobBlackoutCalendar.JobBlackoutWindows",
        ]);
    });

    it("starts a workflow and loads a run graph", async () => {
        const calls: Array<{method: string; url: string; body?: string}> = [];
        const transport: AsyncApiTransport = async (req) => {
            calls.push({method: req.method, url: req.url, body: req.body});
            return ok({isSuccess: true, id: "run-1"});
        };
        const jobs = createAsyncJobApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));

        await jobs.workflows.start("wf-1");
        await jobs.workflows.start("wf-1", {
            idempotencyKey: "once",
            jobScheduleId: "sched-1",
            scheduledSlotUtc: "2026-09-24T12:00:00.000Z",
            inputs: {region: "east"},
        });
        await jobs.workflowRuns.getGraph("wrun-1");
        await jobs.runs.patch("run-1", {properties: {ProgressMessage: "hi"}});

        expect(calls[0]).toMatchObject({method: "POST", url: "http://x/Job/Workflow/wf-1/Start"});
        expect(calls[0]!.body).toBeUndefined();
        expect(JSON.parse(calls[1]!.body!)).toEqual({
            idempotencyKey: "once",
            jobScheduleId: "sched-1",
            scheduledSlotUtc: "2026-09-24T12:00:00.000Z",
            inputs: {region: "east"},
        });
        const graphUrl = calls[2]!.url;
        for (const include of JOB_WORKFLOW_RUN_GRAPH_INCLUDES) {
            expect(graphUrl).toContain(`include=${encodeURIComponent(include)}`);
        }
        expect(calls[3]).toMatchObject({method: "PATCH", url: "http://x/Job/Run/run-1"});
        expect(JSON.parse(calls[3]!.body!)).toMatchObject({keys: [["run-1"]], properties: {ProgressMessage: "hi"}});
    });
});

describe("createJobApiClient", () => {
    it("types schedules and workflows and patches runs", () => {
        const calls: Array<{method: string; url: string; body?: string}> = [];
        const transport: ApiTransport = (req) => {
            calls.push({method: req.method, url: req.url, body: req.body});
            return ok({isSuccess: true, items: []});
        };
        const jobs = createJobApiClient(createApiClient({baseUrl: "http://x", transport}));

        jobs.schedules.queryConcrete(emptyConcreteQuery(5));
        jobs.workflows.queryConcrete(emptyConcreteQuery(5));
        jobs.workflows.start("wf-2", {idempotencyKey: "sync"});
        jobs.runs.patchProgress("run-9", 40, "halfway");
        jobs.workflowRuns.getGraph("wrun-2");

        expect(calls.slice(0, 4).map((c) => c.url)).toEqual([
            "http://x/Job/Schedule/QueryConcrete",
            "http://x/Job/Workflow/QueryConcrete",
            "http://x/Job/Workflow/wf-2/Start",
            "http://x/Job/Run/run-9",
        ]);
        expect(calls[4]!.url.startsWith("http://x/Job/Workflow/Run/wrun-2")).toBe(true);
        expect(JSON.parse(calls[2]!.body!)).toEqual({idempotencyKey: "sync"});
        expect(JSON.parse(calls[3]!.body!).properties).toMatchObject({
            ProgressPercent: 40,
            ProgressMessage: "halfway",
        });
    });
});
