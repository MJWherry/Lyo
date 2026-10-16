import {describe, expect, it} from "vitest";
import {createAsyncApiClient, type ApiResponse, type AsyncApiTransport} from "lyo-api-client";
import {createAsyncFeatureFlagApiClient} from "./featureFlagClient.js";

function ok<T>(data: T): ApiResponse<T> {
    return {status: 200, ok: true, data};
}

describe("feature flag client", () => {
    it("enabled_list_setEnabled_UseFeatureRoutes", async () => {
        const calls: Array<{method: string; url: string; body?: string}> = [];
        const transport: AsyncApiTransport = async (req) => {
            calls.push({method: req.method, url: req.url, body: typeof req.body === "string" ? req.body : undefined});
            return ok([]);
        };
        const client = createAsyncFeatureFlagApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));
        await client.enabled();
        await client.list();
        await client.setEnabled("sample", false);
        expect(calls.map((call) => `${call.method} ${call.url}`)).toEqual([
            "GET http://x/features",
            "GET http://x/features/manage",
            "PUT http://x/features/manage/sample",
        ]);
        expect(JSON.parse(calls[2]!.body!)).toEqual({enabled: false});
    });
});
