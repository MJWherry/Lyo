"use client";

import { useCallback, useEffect, useState } from "react";
import type {
  FantasyConversation,
  FantasyFootballApiClient,
  FantasyLeague,
  FantasyMessage,
  FantasyTrade,
} from "lyo-fantasy-football-api-client";
import { LyoAlert, LyoButton, LyoCheckbox, LyoMenuItem, LyoStack, LyoTabSet, LyoTextField } from "lyo-web-components";
import { topicTitle, tradeSummary } from "./fantasyFootballUi.js";

export interface FantasyFootballWorkbenchProps {
  client: FantasyFootballApiClient;
  leagueId: number;
  seasonId: number;
  viewerEspnId?: string;
}

export function FantasyFootballWorkbench({ client, leagueId, seasonId, viewerEspnId }: FantasyFootballWorkbenchProps) {
  const [league, setLeague] = useState<FantasyLeague | null>(null);
  const [conversations, setConversations] = useState<FantasyConversation[]>([]);
  const [topicId, setTopicId] = useState<string>("");
  const [messages, setMessages] = useState<FantasyMessage[]>([]);
  const [draft, setDraft] = useState("");
  const [trades, setTrades] = useState<FantasyTrade[]>([]);
  const [teamId, setTeamId] = useState(0);
  const [otherTeamId, setOtherTeamId] = useState(0);
  const [sendIds, setSendIds] = useState<number[]>([]);
  const [receiveIds, setReceiveIds] = useState<number[]>([]);
  const [comment, setComment] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    const [stored, groups, recent] = await Promise.all([
      client.getLeague(leagueId, seasonId),
      client.listConversations(leagueId, seasonId),
      client.listTrades(leagueId, seasonId),
    ]);
    setLeague(stored);
    setConversations(groups);
    setTrades(recent);
    setTopicId((current) => current || groups[0]?.espnTopicId || "");
    setTeamId((current) => current || stored.teams[0]?.espnTeamId || 0);
    setOtherTeamId((current) => current || stored.teams[1]?.espnTeamId || 0);
  }, [client, leagueId, seasonId]);

  useEffect(() => {
    void load().catch((reason: unknown) => setError(String(reason)));
  }, [load]);

  useEffect(() => {
    if (!topicId) return;
    void client.listMessages(leagueId, seasonId, topicId).then(setMessages).catch((reason: unknown) => setError(String(reason)));
  }, [client, leagueId, seasonId, topicId]);

  async function refresh() {
    setBusy(true);
    setError(null);
    try {
      await client.refresh(leagueId, seasonId);
      await load();
    } catch (reason) {
      setError(String(reason));
    } finally {
      setBusy(false);
    }
  }

  async function send() {
    if (!topicId || !draft.trim()) return;
    setBusy(true);
    try {
      await client.postMessage(leagueId, seasonId, topicId, draft.trim());
      setDraft("");
      setMessages(await client.listMessages(leagueId, seasonId, topicId));
    } catch (reason) {
      setError(String(reason));
    } finally {
      setBusy(false);
    }
  }

  async function propose() {
    setBusy(true);
    try {
      await client.proposeTrade(leagueId, seasonId, {
        teamId,
        otherTeamId,
        scoringPeriodId: league?.scoringPeriodId ?? 1,
        sendPlayerIds: sendIds,
        receivePlayerIds: receiveIds,
        comment,
      });
      setTrades(await client.listTrades(leagueId, seasonId));
    } catch (reason) {
      setError(String(reason));
    } finally {
      setBusy(false);
    }
  }

  async function respond(trade: FantasyTrade, action: "accept" | "decline" | "uphold" | "veto") {
    setBusy(true);
    try {
      await client.respondTrade(leagueId, seasonId, trade.espnTransactionId, {
        teamId,
        scoringPeriodId: trade.scoringPeriodId,
        action,
      });
      setTrades(await client.listTrades(leagueId, seasonId));
    } catch (reason) {
      setError(String(reason));
    } finally {
      setBusy(false);
    }
  }

  const teams = league?.teams ?? [];
  const home = teams.find((team) => team.espnTeamId === teamId);
  const other = teams.find((team) => team.espnTeamId === otherTeamId);

  return (
    <LyoStack spacing={2}>
      <LyoButton onClick={() => void refresh()} disabled={busy}>Refresh</LyoButton>
      {error ? <LyoAlert severity="error">{error}</LyoAlert> : null}
      <LyoTabSet
        queryParameter={null}
        tabs={[
          {
            id: "chats",
            text: "Chats",
            content: (
              <LyoStack spacing={1}>
                {conversations.map((conversation) => (
                  <LyoButton key={conversation.espnTopicId} onClick={() => setTopicId(conversation.espnTopicId)}>
                    {topicTitle(conversation, viewerEspnId)} ({conversation.unreadCount})
                  </LyoButton>
                ))}
                {messages.map((message) => (
                  <div key={message.espnMessageId}>{message.content || message.kind}</div>
                ))}
                <LyoTextField label="Message" value={draft} onChange={(event) => setDraft(event.target.value)} />
                <LyoButton onClick={() => void send()} disabled={busy || !topicId}>Send</LyoButton>
              </LyoStack>
            ),
          },
          {
            id: "trades",
            text: "Trades",
            content: (
              <LyoStack spacing={1}>
                {trades.map((trade) => (
                  <LyoStack key={trade.espnTransactionId} spacing={0.5}>
                    <div>{trade.isPending ? "Pending" : trade.status}: {tradeSummary(trade)}</div>
                    {trade.isPending ? (
                      <LyoStack direction="row" spacing={1}>
                        <LyoButton onClick={() => void respond(trade, "accept")}>Accept</LyoButton>
                        <LyoButton onClick={() => void respond(trade, "decline")}>Decline</LyoButton>
                        <LyoButton onClick={() => void respond(trade, "uphold")}>Uphold</LyoButton>
                        <LyoButton onClick={() => void respond(trade, "veto")}>Veto</LyoButton>
                      </LyoStack>
                    ) : null}
                  </LyoStack>
                ))}
                <LyoTextField select label="Your team" value={String(teamId)} onChange={(event) => setTeamId(Number(event.target.value))}>
                  {teams.map((team) => (
                    <LyoMenuItem key={team.espnTeamId} value={String(team.espnTeamId)}>{team.name}</LyoMenuItem>
                  ))}
                </LyoTextField>
                <LyoTextField select label="Other team" value={String(otherTeamId)} onChange={(event) => setOtherTeamId(Number(event.target.value))}>
                  {teams.map((team) => (
                    <LyoMenuItem key={team.espnTeamId} value={String(team.espnTeamId)}>{team.name}</LyoMenuItem>
                  ))}
                </LyoTextField>
                {(home?.roster ?? []).map((player) => (
                  <label key={`send-${player.playerId}`}>
                    <LyoCheckbox
                      checked={sendIds.includes(player.playerId)}
                      onChange={(_, checked) => setSendIds(toggle(sendIds, player.playerId, checked))}
                    />
                    Send {player.playerName}
                  </label>
                ))}
                {(other?.roster ?? []).map((player) => (
                  <label key={`receive-${player.playerId}`}>
                    <LyoCheckbox
                      checked={receiveIds.includes(player.playerId)}
                      onChange={(_, checked) => setReceiveIds(toggle(receiveIds, player.playerId, checked))}
                    />
                    Receive {player.playerName}
                  </label>
                ))}
                <LyoTextField label="Comment" value={comment} onChange={(event) => setComment(event.target.value)} />
                <LyoButton onClick={() => void propose()} disabled={busy}>Propose</LyoButton>
              </LyoStack>
            ),
          },
        ]}
      />
    </LyoStack>
  );
}

function toggle(ids: number[], id: number, checked: boolean): number[] {
  return checked ? [...ids, id] : ids.filter((value) => value !== id);
}
