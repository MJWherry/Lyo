import { describe, expect, it } from "vitest";
import { createAsyncApiClient, type ApiResponse, type AsyncApiTransport } from "lyo-api-client";
import { createAsyncFantasyFootballApiClient } from "./fantasyFootballClient.js";

function ok<T>(data: T): ApiResponse<T> {
  return { status: 200, ok: true, data };
}

describe("fantasy football client", () => {
  it("refresh_conversations_message_trades_propose_UsePluginRoutes", async () => {
    const calls: Array<{ method: string; url: string; body?: string }> = [];
    const transport: AsyncApiTransport = async (req) => {
      calls.push({ method: req.method, url: req.url, body: typeof req.body === "string" ? req.body : undefined });
      return ok({});
    };
    const client = createAsyncFantasyFootballApiClient(createAsyncApiClient({ baseUrl: "http://x", transport }));
    await client.refresh(7, 2026);
    await client.listConversations(7, 2026);
    await client.postMessage(7, 2026, "topic-1", "hello");
    await client.listTrades(7, 2026, true);
    await client.proposeTrade(7, 2026, {
      teamId: 1,
      otherTeamId: 2,
      scoringPeriodId: 6,
      sendPlayerIds: [100],
      receivePlayerIds: [200],
      comment: "swap",
    });
    expect(calls.map((call) => `${call.method} ${call.url}`)).toEqual([
      "POST http://x/FantasyFootball/7/2026/refresh",
      "GET http://x/FantasyFootball/7/2026/conversations",
      "POST http://x/FantasyFootball/7/2026/conversations/topic-1/messages",
      "GET http://x/FantasyFootball/7/2026/trades?pending=true",
      "POST http://x/FantasyFootball/7/2026/trades",
    ]);
    expect(JSON.parse(calls[2]!.body!)).toEqual({ content: "hello" });
    expect(JSON.parse(calls[4]!.body!)).toEqual({
      teamId: 1,
      otherTeamId: 2,
      scoringPeriodId: 6,
      sendPlayerIds: [100],
      receivePlayerIds: [200],
      comment: "swap",
    });
  });

  it("refresh_SuppliedCookies_SendsAuthHeaders", async () => {
    const calls: Array<Record<string, string>> = [];
    const transport: AsyncApiTransport = async (req) => {
      calls.push(req.headers);
      return ok({});
    };
    const client = createAsyncFantasyFootballApiClient(
      createAsyncApiClient({ baseUrl: "http://x", transport }),
      { espnS2: "pasted-s2", swid: "{PASTED-SWID}" }
    );
    await client.refresh(7, 2026);
    expect(calls[0]).toMatchObject({ "X-Espn-S2": "pasted-s2", "X-Swid": "{PASTED-SWID}" });
  });
});
