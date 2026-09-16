import {describe, expect, it} from "vitest";
import {createAsyncApiClient, type ApiResponse, type AsyncApiTransport} from "lyo-api-client";
import {createAsyncAuthApiClient} from "./authClient.js";
import {loginUrl} from "./routes.js";

function ok<T>(data: T): ApiResponse<T> {
    return {status: 200, ok: true, data};
}

describe("auth client", () => {
    it("me_GetsAuthMe", async () => {
        const calls: Array<{method: string; url: string}> = [];
        const transport: AsyncApiTransport = async (req) => {
            calls.push({method: req.method, url: req.url});
            return ok({id: "u1", displayName: "Ada"});
        };
        const auth = createAsyncAuthApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));
        await auth.me();
        expect(calls[0]).toMatchObject({method: "GET", url: "http://x/auth/me"});
    });

    it("loginUrl_IsProviderRedirect", () => {
        expect(loginUrl("google", "/home")).toBe("/auth/login/google?returnUrl=%2Fhome");
    });

    it("createToken_ReturnsSecretOnce", async () => {
        const transport: AsyncApiTransport = async (req) => {
            if (req.method === "POST") return ok({plaintext: "s3cret", record: {id: "t1", displayName: "cli"}});
            return ok([{id: "t1", displayName: "cli", secret: "s3cret", secretHash: "hash", plaintext: "s3cret"}]);
        };
        const auth = createAsyncAuthApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));
        const created = await auth.tokens.create({displayName: "cli", scopes: ["auth.tokens.read"]});
        expect(created.data?.plaintext).toBe("s3cret");
        const listed = await auth.tokens.list();
        expect(listed.data?.[0]).toMatchObject({id: "t1", displayName: "cli"});
        expect(listed.data?.[0]).not.toHaveProperty("secret");
        expect(listed.data?.[0]).not.toHaveProperty("plaintext");
        expect(listed.data?.[0]).not.toHaveProperty("secretHash");
    });

    it("adminUserQuery_UsesAuthUserRoute", async () => {
        const calls: Array<{method: string; url: string}> = [];
        const transport: AsyncApiTransport = async (req) => {
            calls.push({method: req.method, url: req.url});
            return ok({items: [], isSuccess: true});
        };
        const auth = createAsyncAuthApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));
        await auth.admin.users.queryProject({Amount: 10});
        expect(calls[0]).toMatchObject({method: "POST", url: "http://x/Auth/User/QueryProject"});
    });
});
