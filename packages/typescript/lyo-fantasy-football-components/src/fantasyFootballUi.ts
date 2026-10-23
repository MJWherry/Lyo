import type { FantasyConversation, FantasyTrade } from "lyo-fantasy-football-api-client";

/** League chat, or the other person in a direct message. */
export function topicTitle(conversation: FantasyConversation, viewerEspnId?: string): string {
  if (conversation.type === "CHAT_ALL_MEMBERS") return "League chat";
  const other = conversation.members.find((member) => member.espnMemberId !== viewerEspnId);
  return other?.displayName || other?.espnMemberId || "Direct message";
}

/** Sent and received player names, using the first item's team as home. */
export function tradeSummary(trade: FantasyTrade): string {
  if (trade.items.length === 0) return "No players";
  const home = trade.items[0]!.fromEspnTeamId;
  const sent = trade.items.filter((item) => item.fromEspnTeamId === home).map(nameOf);
  const received = trade.items.filter((item) => item.fromEspnTeamId !== home).map(nameOf);
  return `Sent ${sent.join(", ") || "nobody"}. Received ${received.join(", ") || "nobody"}.`;
}

function nameOf(item: FantasyTrade["items"][number]): string {
  return item.playerName || String(item.playerId);
}
