import {describe, expect, it} from "vitest";
import {createAsyncApiClient, type ApiResponse, type AsyncApiTransport} from "lyo-api-client";
import {createAsyncConfigApiClient} from "./configClient.js";

function ok<T>(data: T, status = 200): ApiResponse<T> {
    return {status, ok: status >= 200 && status < 300, data};
}

describe("createAsyncConfigApiClient", () => {
    it("resolve_NotModified_WhenEtagMatches", async () => {
        const calls: Array<{method: string; url: string; headers: Record<string, string>}> = [];
        const transport: AsyncApiTransport = async (req) => {
            calls.push({method: req.method, url: req.url, headers: req.headers});
            return {status: 304, ok: false};
        };
        const config = createAsyncConfigApiClient(createAsyncApiClient({baseUrl: "http://x", transport}), {
            apiKey: "secret",
        });

        const result = await config.resolve("App", "gateway:local", {ifNoneMatch: '"abc"'});

        expect(result.outcome).toBe(304);
        expect(result.record).toBeUndefined();
        expect(calls[0]).toMatchObject({
            method: "GET",
            url: "http://x/api/config/App/gateway%3Alocal",
        });
        expect(calls[0]!.headers["If-None-Match"]).toBe('"abc"');
        expect(calls[0]!.headers["X-Api-Key"]).toBe("secret");
    });

    it("putDefinition_SendsPlaintextAndEncryptedFlag", async () => {
        const calls: Array<{method: string; url: string; body?: string}> = [];
        const transport: AsyncApiTransport = async (req) => {
            calls.push({method: req.method, url: req.url, body: req.body});
            return ok({});
        };
        const config = createAsyncConfigApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));

        await config.putDefinition({
            subjectEntityType: "App",
            key: "Theme",
            forValueType: "System.String",
            isEncrypted: true,
            defaultValue: {typeName: "System.String", json: '"light"'},
        });

        expect(calls[0]).toMatchObject({method: "PUT", url: "http://x/api/config/manage/definitions"});
        const body = JSON.parse(calls[0]!.body!);
        expect(body.isEncrypted).toBe(true);
        expect(body.defaultValue).toEqual({typeName: "System.String", json: '"light"'});
        expect(JSON.stringify(body)).not.toMatch(/cipher|ciphertext/i);
    });

    it("resolve_Head_NotModified_WhenEtagMatches", async () => {
        const calls: Array<{method: string; url: string}> = [];
        const transport: AsyncApiTransport = async (req) => {
            calls.push({method: req.method, url: req.url});
            return {status: 304, ok: false};
        };
        const config = createAsyncConfigApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));
        const result = await config.resolve("App", "local", {method: "HEAD", version: '"abc"'});
        expect(result.outcome).toBe(304);
        expect(result.record).toBeUndefined();
        expect(calls[0]).toMatchObject({method: "HEAD", url: "http://x/api/config/App/local?version=%22abc%22"});
    });
});
