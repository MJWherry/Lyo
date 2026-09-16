import {describe, expect, it} from "vitest";
import {createAsyncApiClient, type ApiResponse, type AsyncApiTransport} from "lyo-api-client";
import {createAsyncAuthorizationApiClient} from "./authorizationClient.js";

function ok<T>(data: T): ApiResponse<T> {
    return {status: 200, ok: true, data};
}

describe("authorization client", () => {
    it("list_save_revoke_check_UseGrantRoutes", async () => {
        const calls: Array<{method: string; url: string}> = [];
        const transport: AsyncApiTransport = async (req) => {
            calls.push({method: req.method, url: req.url});
            return ok([]);
        };
        const client = createAsyncAuthorizationApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));
        await client.list({subject: "user-a"});
        await client.save({subject: "user-a", action: "read", resourceType: "note"});
        await client.revoke("row-1");
        await client.check({subject: "user-a", action: "read", resourceType: "note"});
        expect(calls.map((call) => `${call.method} ${call.url}`)).toEqual([
            "GET http://x/authorization/grants?subject=user-a",
            "POST http://x/authorization/grants",
            "POST http://x/authorization/grants/row-1/revoke",
            "POST http://x/authorization/grants/check",
        ]);
    });
});
