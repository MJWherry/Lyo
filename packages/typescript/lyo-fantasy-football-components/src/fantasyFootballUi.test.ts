import { describe, expect, it } from "vitest";
import type { FantasyConversation, FantasyTrade } from "lyo-fantasy-football-api-client";
import { topicTitle, tradeSummary } from "./fantasyFootballUi.js";

describe("fantasy football ui", () => {
  it("topicTitle_DirectMessage_UsesTheOtherMember", () => {
    const conversation: FantasyConversation = {
      espnTopicId: "dm",
      type: "CHAT_DIRECT_MESSAGE",
      unreadCount: 0,
      members: [
        { espnMemberId: "ann", displayName: "Ann" },
        { espnMemberId: "bob", displayName: "Bob" },
      ],
    };
    expect(topicTitle(conversation, "ann")).toBe("Bob");
  });

  it("topicTitle_AllMembers_IsLeagueChat", () => {
    const conversation: FantasyConversation = {
      espnTopicId: "league",
      type: "CHAT_ALL_MEMBERS",
      unreadCount: 1,
      members: [{ espnMemberId: "ann", displayName: "Ann" }],
    };
    expect(topicTitle(conversation, "ann")).toBe("League chat");
  });

  it("tradeSummary_TwoSides_ListsSentAndReceivedPlayers", () => {
    const trade: FantasyTrade = {
      espnTransactionId: "tx",
      type: "TRADE_PROPOSAL",
      status: "PENDING",
      isPending: true,
      scoringPeriodId: 6,
      items: [
        { fromEspnTeamId: 1, toEspnTeamId: 2, playerId: 100, playerName: "Alpha" },
        { fromEspnTeamId: 2, toEspnTeamId: 1, playerId: 200, playerName: "Beta" },
      ],
    };
    expect(tradeSummary(trade)).toBe("Sent Alpha. Received Beta.");
  });
});
