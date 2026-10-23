import {describe, expect, it} from "vitest";
import {createAsyncApiClient, type ApiResponse, type AsyncApiTransport} from "lyo-api-client";
import {createAsyncStoryApiClient} from "./storyClient.js";
import {joinRoutePrefix} from "./routes.js";

function ok<T>(data: T): ApiResponse<T> {
    return {status: 200, ok: true, data};
}

const storyId = "11111111-1111-1111-1111-111111111111";
const playId = "22222222-2222-2222-2222-222222222222";

describe("joinRoutePrefix", () => {
    it("adds a leading slash without prefix", () => {
        expect(joinRoutePrefix(undefined, "Story/graph")).toBe("/Story/graph");
    });

    it("joins a prefix", () => {
        expect(joinRoutePrefix("api", "Story/graph")).toBe("/api/Story/graph");
    });
});

describe("createAsyncStoryApiClient", () => {
    it("posts graph save and list, deletes a story, and posts a choice", async () => {
        const calls: Array<{method: string; url: string; body?: string}> = [];
        const transport: AsyncApiTransport = async (req) => {
            calls.push({method: req.method, url: req.url, body: req.body});
            return ok({id: storyId});
        };
        const stories = createAsyncStoryApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));

        await stories.saveGraph({title: "The Heist", slug: "the-heist", status: "draft", nodes: []});
        await stories.listGraphs({gameKey: "heist"});
        await stories.delete(storyId);
        await stories.choose(playId, {choiceKey: "smash"});

        expect(calls[0]).toMatchObject({method: "POST", url: "http://x/Story/graph"});
        expect(JSON.parse(calls[0]!.body!)).toMatchObject({title: "The Heist", slug: "the-heist"});
        expect(calls[1]).toMatchObject({method: "POST", url: "http://x/Story/graph/list"});
        expect(JSON.parse(calls[1]!.body!)).toMatchObject({gameKey: "heist"});
        expect(calls[2]).toMatchObject({method: "DELETE", url: `http://x/Story/Stories/${storyId}`});
        expect(calls[3]).toMatchObject({method: "POST", url: `http://x/Story/playthrough/${playId}/choose`});
        expect(JSON.parse(calls[3]!.body!)).toMatchObject({choiceKey: "smash"});
    });
});
