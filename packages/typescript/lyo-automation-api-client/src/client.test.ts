import {describe, expect, it} from "vitest";
import {createAsyncApiClient, type ApiResponse, type AsyncApiTransport} from "lyo-api-client";
import {createAsyncAutomationApiClient} from "./client.js";

function ok<T>(data: T): ApiResponse<T> {
    return {status: 200, ok: true, data};
}

describe("createAsyncAutomationApiClient", () => {
    it("post returns a run id and poll stops on completed", async () => {
        let gets = 0;
        const transport: AsyncApiTransport = async (req) => {
            if (req.method === "POST" && req.url.endsWith("/Automation/Run"))
                return {status: 202, ok: true, data: {runId: "run-1", status: "queued"}};
            if (req.method === "GET" && req.url.endsWith("/Automation/Run/run-1")) {
                gets += 1;
                return ok({runId: "run-1", status: gets < 2 ? "queued" : "completed"});
            }
            return {status: 404, ok: false};
        };
        const client = createAsyncAutomationApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));

        const accepted = await client.start({plan: {name: "demo", steps: []}});
        const settled = await client.waitUntilSettled(accepted.runId);

        expect(accepted.runId).toBe("run-1");
        expect(settled.status).toBe("completed");
        expect(gets).toBe(2);
    });
});
