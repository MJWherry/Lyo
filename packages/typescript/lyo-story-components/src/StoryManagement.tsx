"use client";

import {useCallback, useEffect, useState} from "react";
import type {StoryGraph} from "lyo-story-api-client";
import {clientStore, LyoAlert, LyoButton, LyoStack, useLyoSnackbar} from "lyo-web-components";
import {StoryBuilderWorkbench} from "./StoryBuilderWorkbench.js";
import {toSaveGraph} from "./storyGraph.js";
import {STORY_BUILDER_STORAGE_KEY, messageOf, useStoryClient, type StorySurfaceProps} from "./storyHost.js";

export function StoryManagement({apiClient, routePrefix, tenantId, gameKey}: StorySurfaceProps) {
    const client = useStoryClient(apiClient, routePrefix);
    const snackbar = useLyoSnackbar();
    const [stories, setStories] = useState<StoryGraph[]>([]);
    const [open, setOpen] = useState<StoryGraph | null>(null);
    const [session, setSession] = useState(0);
    const [error, setError] = useState<string | null>(null);

    const reload = useCallback(async () => {
        if (!client) return;
        try {
            setStories(await client.listGraphs({tenantId, gameKey}));
            setError(null);
        } catch (caught) {
            setError(messageOf(caught));
        }
    }, [client, tenantId, gameKey]);

    useEffect(() => { void reload(); }, [reload]);

    const openGraph = async (id: string) => {
        if (!client || !id) return;
        try {
            const graph = await client.getGraph(id);
            if (!graph) {
                snackbar.show("That story was not found.", "warning");
                return;
            }
            setOpen(graph);
            setSession((value) => value + 1);
        } catch (caught) {
            snackbar.show(messageOf(caught), "error");
        }
    };

    if (!client) return <LyoAlert severity="warning">Pass apiClient to list stories.</LyoAlert>;

    if (open) {
        return (
            <LyoStack spacing={2} className="lyo-story-management">
                <LyoButton onClick={() => setOpen(null)}>Library</LyoButton>
                <StoryBuilderWorkbench
                    key={session}
                    apiClient={client}
                    routePrefix={routePrefix}
                    tenantId={tenantId}
                    gameKey={gameKey}
                    draft={open}
                    onSaved={(saved) => {
                        setOpen(saved);
                        clientStore.set(STORY_BUILDER_STORAGE_KEY, toSaveGraph(saved));
                        void reload();
                    }}
                />
            </LyoStack>
        );
    }

    return (
        <LyoStack spacing={2} className="lyo-story-management">
            <LyoStack direction="row" spacing={1} sx={{alignItems: "center"}}>
                <h2>Stories</h2>
                <LyoButton variant="contained" onClick={() => {
                    setOpen({title: "Untitled", slug: "", status: "draft", tenantId, gameKey, startNodeKey: "start", nodes: [{key: "start", presentationKind: "none", choices: []}]});
                    setSession((value) => value + 1);
                }}>New story</LyoButton>
            </LyoStack>
            {error ? <LyoAlert severity="error">{error}</LyoAlert> : null}
            {stories.length === 0 ? <span>No stories for this tenant and game.</span> : (
                <table className="story-library-table">
                    <thead><tr><th>Title</th><th>Slug</th><th>Status</th><th></th></tr></thead>
                    <tbody>
                        {stories.map((story) => (
                            <tr key={story.id ?? story.slug}>
                                <td>{story.title}</td>
                                <td>{story.slug}</td>
                                <td>{story.status}</td>
                                <td>
                                    <LyoButton size="small" disabled={!story.id} onClick={() => story.id && void openGraph(story.id)}>Open</LyoButton>
                                    <LyoButton size="small" disabled={!story.id} onClick={() => story.id && void duplicate(story.id)}>Duplicate</LyoButton>
                                    <LyoButton size="small" color="error" disabled={!story.id} onClick={() => story.id && void remove(story.id)}>Delete</LyoButton>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            )}
        </LyoStack>
    );

    async function duplicate(id: string) {
        if (!client) return;
        try {
            const graph = await client.getGraph(id);
            if (!graph) return;
            const copy = toSaveGraph({...graph, id: undefined, title: `${graph.title} copy`, slug: `${graph.slug}-copy`, status: "draft"});
            const saved = await client.saveGraph(copy);
            snackbar.show(`Duplicated ${saved.title}.`, "success");
            await reload();
        } catch (caught) {
            snackbar.show(messageOf(caught), "error");
        }
    }

    async function remove(id: string) {
        if (!client) return;
        try {
            await client.delete(id);
            await reload();
        } catch (caught) {
            snackbar.show(messageOf(caught), "error");
        }
    }
}
