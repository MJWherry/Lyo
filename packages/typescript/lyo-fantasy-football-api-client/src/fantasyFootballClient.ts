import type { AsyncApiClient } from "lyo-api-client";
import type {
  FantasyConversation,
  FantasyLeague,
  FantasyMessage,
  FantasyTrade,
  ProposeTradeRequest,
  RespondTradeRequest,
} from "./types.js";

export interface FantasyFootballApiClientOptions {
  /** Route group registered by MapFantasyFootballApi. */
  routePrefix?: string;
  /** ESPN espn_s2 cookie. Blank keeps the cookie configured on the API host. */
  espnS2?: string;
  /** ESPN SWID cookie. Blank keeps the cookie configured on the API host. */
  swid?: string;
}

export interface FantasyFootballApiClient {
  refresh(leagueId: number, seasonId: number): Promise<FantasyLeague>;
  getLeague(leagueId: number, seasonId: number): Promise<FantasyLeague>;
  listConversations(leagueId: number, seasonId: number): Promise<FantasyConversation[]>;
  listMessages(leagueId: number, seasonId: number, topicId: string): Promise<FantasyMessage[]>;
  postMessage(leagueId: number, seasonId: number, topicId: string, content: string): Promise<FantasyMessage>;
  postDirect(leagueId: number, seasonId: number, recipient: string, content: string): Promise<FantasyMessage>;
  listTrades(leagueId: number, seasonId: number, pending?: boolean): Promise<FantasyTrade[]>;
  proposeTrade(leagueId: number, seasonId: number, request: ProposeTradeRequest): Promise<FantasyTrade>;
  respondTrade(leagueId: number, seasonId: number, transactionId: string, request: RespondTradeRequest): Promise<FantasyTrade>;
}

/** Reads and writes chats and trades through the Fantasy Football plugin API. */
export function createAsyncFantasyFootballApiClient(
  api: AsyncApiClient,
  options: FantasyFootballApiClientOptions = {}
): FantasyFootballApiClient {
  const prefix = `/${(options.routePrefix ?? "FantasyFootball").replace(/^\/+|\/+$/g, "")}`;

  function path(leagueId: number, seasonId: number, relative = ""): string {
    const root = `${prefix}/${leagueId}/${seasonId}`;
    return relative ? `${root}/${relative}` : root;
  }

  function authHeaders(): Record<string, string> | undefined {
    const headers: Record<string, string> = {};
    if (options.espnS2) headers["X-Espn-S2"] = options.espnS2;
    if (options.swid) headers["X-Swid"] = options.swid;
    return Object.keys(headers).length === 0 ? undefined : headers;
  }

  async function send<T>(method: "GET" | "POST", url: string, body?: unknown): Promise<T> {
    const response = await api.request<T>({ method, path: url, body, headers: authHeaders() });
    if (response.data === undefined)
      throw new Error(`Empty response from ${method} ${url}`);
    return response.data;
  }

  return {
    refresh: (leagueId, seasonId) => send<FantasyLeague>("POST", path(leagueId, seasonId, "refresh")),
    getLeague: (leagueId, seasonId) => send<FantasyLeague>("GET", path(leagueId, seasonId)),
    listConversations: (leagueId, seasonId) => send<FantasyConversation[]>("GET", path(leagueId, seasonId, "conversations")),
    listMessages: (leagueId, seasonId, topicId) =>
      send<FantasyMessage[]>("GET", path(leagueId, seasonId, `conversations/${encodeURIComponent(topicId)}/messages`)),
    postMessage: (leagueId, seasonId, topicId, content) =>
      send<FantasyMessage>("POST", path(leagueId, seasonId, `conversations/${encodeURIComponent(topicId)}/messages`), { content }),
    postDirect: (leagueId, seasonId, recipient, content) =>
      send<FantasyMessage>("POST", path(leagueId, seasonId, "conversations/direct"), { recipient, content }),
    listTrades: (leagueId, seasonId, pending) => {
      const query = pending === undefined ? "" : `?pending=${pending}`;
      return send<FantasyTrade[]>("GET", `${path(leagueId, seasonId, "trades")}${query}`);
    },
    proposeTrade: (leagueId, seasonId, request) => send<FantasyTrade>("POST", path(leagueId, seasonId, "trades"), request),
    respondTrade: (leagueId, seasonId, transactionId, request) =>
      send<FantasyTrade>("POST", path(leagueId, seasonId, `trades/${encodeURIComponent(transactionId)}/respond`), request),
  };
}
