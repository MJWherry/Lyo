import {describe, expect, it} from "vitest";
import {createAsyncApiClient, type ApiResponse, type AsyncApiTransport} from "lyo-api-client";
import {clearCache} from "./cache.js";

function ok(): ApiResponse<unknown> {
    return {status: 200, ok: true, data: {}};
}

describe("cache", () => {
    it("cacheClear_PostsRouteClear", async () => {
        const calls: Array<{method: string; url: string}> = [];
        const transport: AsyncApiTransport = async (req) => {
            calls.push({method: req.method, url: req.url});
            return ok();
        };
        await clearCache(createAsyncApiClient({baseUrl: "http://x", transport}));
        expect(calls[0]).toMatchObject({method: "POST", url: "http://x/Cache/Clear"});
    });
});
