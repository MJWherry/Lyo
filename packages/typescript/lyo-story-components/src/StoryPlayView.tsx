"use client";

import {useState} from "react";
import type {StoryPlayEvent, StoryPlayState} from "lyo-story-api-client";
import {LyoAlert, LyoButton, LyoStack, LyoTextField, useLyoSnackbar} from "lyo-web-components";
import {messageOf, useStoryClient, type StorySurfaceProps} from "./storyHost.js";

export function StoryPlayView({apiClient, routePrefix, storyId}: StorySurfaceProps & {storyId?: string | null}) {
    const client = useStoryClient(apiClient, routePrefix);
    const snackbar = useLyoSnackbar();
    const [playerRef, setPlayerRef] = useState("");
    const [busy, setBusy] = useState(false);
    const [state, setState] = useState<StoryPlayState | null>(null);
    const [events, setEvents] = useState<StoryPlayEvent[]>([]);
    const [causes, setCauses] = useState<StoryPlayEvent[] | null>(null);
    const [causeKey, setCauseKey] = useState<string | null>(null);

    if (!storyId) return <span>Save the story to play it.</span>;
    if (!client) return <LyoAlert severity="warning">Pass apiClient to play a story.</LyoAlert>;

    const run = async (work: () => Promise<void>) => {
        setBusy(true);
        try {
            await work();
        } catch (error) {
            snackbar.show(messageOf(error), "error");
        } finally {
            setBusy(false);
        }
    };

    const loadEvents = async (playthroughId: string) => setEvents(await client.getEvents(playthroughId));

    const start = () => run(async () => {
        const next = await client.start({storyId, playerRef: playerRef || null});
        setState(next);
        setCauses(null);
        await loadEvents(next.playthrough.id);
    });

    return (
        <LyoStack spacing={2}>
            {state === null ? (
                <LyoStack direction="row" spacing={1} sx={{alignItems: "center"}}>
                    <LyoTextField size="small" label="Player ref" value={playerRef} onChange={(event) => setPlayerRef(event.target.value)} sx={{minWidth: 220}} />
                    <LyoButton variant="contained" disabled={busy} onClick={start}>Start</LyoButton>
                </LyoStack>
            ) : (
                <>
                    <LyoStack direction="row" spacing={1} sx={{alignItems: "center"}}>
                        <strong>{state.playthrough.status}</strong>
                        <span>{state.playthrough.currentNodeKey}</span>
                        <LyoButton size="small" onClick={() => { setState(null); setEvents([]); setCauses(null); void start(); }}>Restart</LyoButton>
                    </LyoStack>
                    {state.currentNode ? (
                        <>
                            <h3>{state.currentNode.title?.trim() ? state.currentNode.title : state.currentNode.key}</h3>
                            {state.currentNode.presentationKind === "text" && state.currentNode.body ? <p>{state.currentNode.body}</p> : null}
                            {state.currentNode.presentationKind === "cutscene" ? <LyoAlert severity="info">Cutscene {state.currentNode.mediaRef}</LyoAlert> : null}
                        </>
                    ) : null}
                    <LyoStack direction="row" spacing={1}>
                        {(state.moves ?? []).map((move) => move.kind === "advance" ? (
                            <LyoButton key="advance" variant="contained" disabled={busy || move.enabled === false} onClick={() => run(async () => {
                                const next = await client.advance(state.playthrough.id);
                                setState(next);
                                await loadEvents(next.playthrough.id);
                            })}>Advance</LyoButton>
                        ) : (
                            <LyoButton key={move.choiceKey ?? move.prompt} variant="outlined" disabled={busy || move.enabled === false || !move.choiceKey} onClick={() => run(async () => {
                                const next = await client.choose(state.playthrough.id, {choiceKey: move.choiceKey!});
                                setState(next);
                                await loadEvents(next.playthrough.id);
                            })}>{move.prompt ?? move.choiceKey}</LyoButton>
                        ))}
                    </LyoStack>
                    <strong>Variables</strong>
                    {(state.playthrough.variables ?? []).length === 0 ? <span>No variable values yet.</span> : (
                        <table className="story-library-table">
                            <thead><tr><th>Key</th><th>Value</th><th></th></tr></thead>
                            <tbody>
                                {(state.playthrough.variables ?? []).map((variable) => (
                                    <tr key={variable.variableKey}>
                                        <td>{variable.variableKey}</td>
                                        <td>{variable.value}</td>
                                        <td><LyoButton size="small" onClick={() => run(async () => { setCauseKey(variable.variableKey); setCauses(await client.getCauses(state.playthrough.id, variable.variableKey)); })}>Causes</LyoButton></td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    )}
                    {causes ? (
                        <>
                            <strong>Causes for {causeKey}</strong>
                            {causes.map((cause) => <div key={cause.id}>#{cause.sequence} {cause.kind} {cause.nodeKey} {cause.choiceKey} {cause.oldValue} → {cause.newValue}</div>)}
                        </>
                    ) : null}
                    <strong>Event log</strong>
                    {events.length === 0 ? <span>No events yet.</span> : events.map((row) => <div key={row.id}>#{row.sequence} {row.kind} {row.nodeKey} {row.choiceKey} {row.variableKey}</div>)}
                </>
            )}
        </LyoStack>
    );
}
