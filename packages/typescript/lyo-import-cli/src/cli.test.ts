import {describe, expect, it} from "vitest";
import {createAsyncApiClient, type ApiResponse, type AsyncApiTransport} from "lyo-api-client";
import {createAsyncImportApiClient} from "lyo-import-api-client";
import {execute, parseArgv} from "./run.js";

function ok<T>(data: T): ApiResponse<T> {
    return {status: 200, ok: true, data};
}

describe("lyo-import argv", () => {
    it("cli_postsImport_DoesNotSave", async () => {
        const calls: Array<{method: string; url: string; body?: string}> = [];
        const transport: AsyncApiTransport = async (req) => {
            calls.push({method: req.method, url: req.url, body: typeof req.body === "string" ? req.body : undefined});
            return ok({isSuccess: true, rowsJson: "[]", issues: []});
        };
        const client = createAsyncImportApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));
        expect("persist" in client).toBe(false);
        expect("save" in client).toBe(false);
        await execute(parseArgv(["./rows.csv", "--format", "Csv"]), client, {
            readFile: async () => new TextEncoder().encode("Name\nAda\n"),
            stdout: () => undefined,
        });
        expect(calls[0]).toMatchObject({method: "POST", url: "http://x/Import"});
        expect(JSON.parse(calls[0]!.body!)).toMatchObject({format: "Csv", content: "Name\nAda\n"});
    });
});
