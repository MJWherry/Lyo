import {describe, expect, it} from "vitest";
import {createAsyncApiClient, type ApiResponse, type AsyncApiTransport} from "lyo-api-client";
import {createAsyncAuthApiClient} from "lyo-auth-api-client";
import {execute, parseArgv} from "./run.js";

function ok<T>(data: T): ApiResponse<T> {
    return {status: 200, ok: true, data};
}

describe("lyo-auth argv", () => {
    it("tokens list never prints the secret", async () => {
        const transport: AsyncApiTransport = async () => ok([{id: "t1", displayName: "cli", secret: "s3cret", plaintext: "s3cret"}]);
        const auth = createAsyncAuthApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));
        const lines: string[] = [];
        await execute(parseArgv(["tokens", "list"]), auth, {stdout: (line) => lines.push(line)});
        expect(lines.join("\n")).not.toContain("s3cret");
        expect(lines.join("\n")).toContain("t1");
    });

    it("tokens create prints the secret once", async () => {
        const transport: AsyncApiTransport = async () => ok({plaintext: "s3cret", record: {id: "t1"}});
        const auth = createAsyncAuthApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));
        const lines: string[] = [];
        await execute(parseArgv(["tokens", "create", "--name", "laptop", "--scopes", "auth.tokens.read"]), auth, {
            stdout: (line) => lines.push(line),
        });
        expect(lines).toEqual(["s3cret"]);
    });
});
