import {describe, expect, it} from "vitest";
import {createAsyncApiClient, type ApiResponse, type AsyncApiTransport} from "../index.js";
import {createAsyncCrudResource} from "./crudResource.js";

function ok<T>(data: T): ApiResponse<T> {
    return {status: 200, ok: true, data};
}

describe("createAsyncCrudResource patch", () => {
    it("forwards patch bodies and sets keys on patchById", async () => {
        const calls: Array<{method: string; url: string; body?: string}> = [];
        const transport: AsyncApiTransport = async (req) => {
            calls.push({method: req.method, url: req.url, body: req.body});
            return ok({});
        };
        const api = createAsyncApiClient({baseUrl: "http://x", transport});
        const resource = createAsyncCrudResource<{name: string}, {id: string}>(api, "/Job/Definition");

        await resource.patch({keys: [["a"]], properties: {Enabled: false}, query: {extra: 1}});
        await resource.patchById("b", {properties: {Enabled: true}, allowMultiple: false});

        expect(calls[0]).toMatchObject({method: "PATCH", url: "http://x/Job/Definition"});
        expect(JSON.parse(calls[0]!.body!)).toEqual({
            keys: [["a"]],
            properties: {Enabled: false},
            query: {extra: 1},
        });
        expect(JSON.parse(calls[1]!.body!)).toEqual({
            keys: [["b"]],
            properties: {Enabled: true},
            allowMultiple: false,
        });
    });
});
