import {describe, expect, it} from "vitest";
import {createAsyncApiClient, type ApiResponse, type AsyncApiTransport} from "lyo-api-client";
import {createAsyncAlgorithmApiClient} from "./algorithmClient.js";

function ok<T>(data: T): ApiResponse<T> {
  return {status: 200, ok: true, data};
}

describe("createAsyncAlgorithmApiClient", () => {
  it("preview_PostsPreviewRoute", async () => {
    const calls: Array<{method: string; url: string; body?: string}> = [];
    const transport: AsyncApiTransport = async (req) => {
      calls.push({method: req.method, url: req.url, body: req.body});
      return ok({value: 5, result: "double", steps: []});
    };
    const client = createAsyncAlgorithmApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));
    const preview = await client.preview({
      key: "attack.physical",
      kind: "expression",
      source: "Atk * 2 - Def",
      context: {Atk: 3, Def: 1},
    });

    expect(calls[0]).toMatchObject({method: "POST", url: "http://x/Algorithms/preview"});
    expect(JSON.parse(calls[0]!.body!)).toMatchObject({key: "attack.physical", kind: "expression", context: {Atk: 3, Def: 1}});
    expect(preview.value).toBe(5);
  });

  it("delete_DeletesDefinitionRoute", async () => {
    const calls: Array<{method: string; url: string}> = [];
    const transport: AsyncApiTransport = async (req) => {
      calls.push({method: req.method, url: req.url});
      return {status: 204, ok: true};
    };
    const client = createAsyncAlgorithmApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));
    await client.delete("attack.physical");
    expect(calls[0]).toEqual({method: "DELETE", url: "http://x/Algorithms/attack.physical"});
  });
});
