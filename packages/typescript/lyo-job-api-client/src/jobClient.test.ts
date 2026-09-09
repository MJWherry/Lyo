import {describe, expect, it} from "vitest";
import {createAsyncApiClient, type ApiResponse, type AsyncApiTransport} from "lyo-api-client";
import {createAsyncJobApiClient} from "./job/asyncJobClient.js";
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
        expect(JobRoutes.triggers).toBe("Job/Triggers");
    });
});
