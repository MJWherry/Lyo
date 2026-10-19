export interface FantasyMember {
  espnMemberId: string;
  displayName: string;
}

export interface FantasyTeam {
  espnTeamId: number;
  name: string;
  roster: FantasyRosterEntry[];
}

export interface FantasyRosterEntry {
  scoringPeriodId: number;
  playerId: number;
  playerName: string;
}

export interface FantasyLeague {
  espnLeagueId: number;
  seasonId: number;
  scoringPeriodId: number;
  name: string;
  members: FantasyMember[];
  teams: FantasyTeam[];
}

export interface FantasyConversation {
  espnTopicId: string;
  type: string;
  unreadCount: number;
  members: FantasyMember[];
}

export interface FantasyMessagePlayer {
  side: number;
  direction: string;
  playerId: number;
  playerName: string;
}

export interface FantasyMessage {
  espnMessageId: string;
  kind: string;
  content: string;
  sentAt: string;
  authorEspnId: string;
  players: FantasyMessagePlayer[];
}

export interface FantasyTradeItem {
  fromEspnTeamId: number;
  toEspnTeamId: number;
  playerId: number;
  playerName: string;
}

export interface FantasyTrade {
  espnTransactionId: string;
  type: string;
  status: string;
  isPending: boolean;
  scoringPeriodId: number;
  items: FantasyTradeItem[];
}

export interface ProposeTradeRequest {
  teamId: number;
  otherTeamId: number;
  scoringPeriodId: number;
  sendPlayerIds: number[];
  receivePlayerIds: number[];
  comment?: string;
}

export interface RespondTradeRequest {
  teamId: number;
  scoringPeriodId: number;
  action: "accept" | "decline" | "uphold" | "veto";
  comment?: string;
}
