import {describe, expect, it} from "vitest";
import {createAsyncApiClient, type ApiResponse, type AsyncApiTransport} from "lyo-api-client";
import {createAsyncConfigApiClient} from "lyo-config-api-client";
import {execute, parseArgv} from "./run.js";

function ok<T>(data: T): ApiResponse<T> {
    return {status: 200, ok: true, data};
}

describe("lyo-config argv", () => {
    it("cli_set_PutsBinding", async () => {
        const calls: Array<{method: string; url: string; body?: string}> = [];
        const transport: AsyncApiTransport = async (req) => {
            calls.push({method: req.method, url: req.url, body: typeof req.body === "string" ? req.body : undefined});
            if (req.method === "GET") {
                return ok({
                    id: "def-1",
                    subjectEntityType: "App",
                    key: "Theme",
                    forValueType: "System.String",
                    isEncrypted: false,
                });
            }
            return ok({id: "bind-1"});
        };
        const config = createAsyncConfigApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));
        const parsed = parseArgv([
            "set",
            "--type",
            "App",
            "--id",
            "gateway:local",
            "--key",
            "Theme",
            "--value",
            "light",
            "--encrypted",
        ]);
        await execute(parsed, config, {readText: async () => "", stdout: () => undefined});

        const putBinding = calls.find((call) => call.method === "PUT" && call.url.endsWith("/api/config/manage/bindings"));
        expect(putBinding?.url).toBe("http://x/api/config/manage/bindings");
        const body = JSON.parse(putBinding!.body!);
        expect(body.value).toEqual({typeName: "System.String", json: '"light"'});
        expect(JSON.stringify(body)).not.toMatch(/cipher|ciphertext/i);
        const putDefinition = calls.find((call) => call.url.endsWith("/api/config/manage/definitions"));
        expect(JSON.parse(putDefinition!.body!).isEncrypted).toBe(true);
    });
});
