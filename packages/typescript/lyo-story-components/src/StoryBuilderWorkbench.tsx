"use client";

import {useEffect, useState} from "react";
import type {StoryGraph, StoryNode, StoryStatus} from "lyo-story-api-client";
import {clientStore, JsonEditor, LyoAlert, LyoButton, LyoMenuItem, LyoStack, LyoTextField, useLyoSnackbar} from "lyo-web-components";
import {StoryActionEditor, StoryChoiceEditor, StoryNodeEditor, StoryReactionEditor, StoryVariableEditor} from "./editors.js";
import {StoryGraphView} from "./StoryGraphView.js";
import {StoryPlayView} from "./StoryPlayView.js";
import {conditionFields, toSaveGraph, toStoryBuilderCsharp, validateStory} from "./storyGraph.js";
import {STORY_BUILDER_STORAGE_KEY, messageOf, slugify, useStoryClient, type StorySurfaceProps} from "./storyHost.js";

const STATUSES: StoryStatus[] = ["draft", "published", "archived"];

export function StoryBuilderWorkbench({apiClient, routePrefix, tenantId, gameKey, draft: initial, onSaved}: StorySurfaceProps & {draft: StoryGraph; onSaved?: (saved: StoryGraph) => void}) {
    const client = useStoryClient(apiClient, routePrefix);
    const snackbar = useLyoSnackbar();
    const [draft, setDraft] = useState<StoryGraph>(initial);
    const [selectedKey, setSelectedKey] = useState<string | null>(initial.startNodeKey ?? initial.nodes?.[0]?.key ?? null);
    const [choiceIndex, setChoiceIndex] = useState(0);
    const [tab, setTab] = useState(0);
    const [saving, setSaving] = useState(false);

    useEffect(() => {
        const stored = clientStore.get<StoryGraph>(STORY_BUILDER_STORAGE_KEY);
        if (stored && (stored.id ?? "") === (initial.id ?? "")) setDraft(stored);
    }, [initial.id]);

    useEffect(() => {
        const handle = window.setTimeout(() => clientStore.set(STORY_BUILDER_STORAGE_KEY, toSaveGraph(draft)), 450);
        return () => window.clearTimeout(handle);
    }, [draft]);

    const issues = validateStory(draft);
    const request = toSaveGraph({...draft, tenantId: draft.tenantId ?? tenantId, gameKey: draft.gameKey ?? gameKey});
    const snippet = toStoryBuilderCsharp(draft);
    const fields = conditionFields(draft);
    const nodes = draft.nodes ?? [];
    const selected = nodes.find((node) => node.key === selectedKey) ?? null;
    const scenes = draft.scenes ?? [];
    const variables = draft.variables ?? [];
    const actions = draft.actions ?? [];
    const reactions = draft.reactions ?? [];

    const patchNode = (next: StoryNode) => {
        setDraft({...draft, nodes: nodes.map((node) => (node.key === selectedKey ? next : node)), startNodeKey: draft.startNodeKey === selectedKey ? next.key : draft.startNodeKey});
        if (next.key !== selectedKey) setSelectedKey(next.key);
    };

    const save = async () => {
        if (!client) {
            snackbar.show("Pass apiClient to save a story.", "warning");
            return;
        }
        const slug = draft.slug?.trim() ? draft.slug : slugify(draft.title);
        const body = toSaveGraph({...draft, slug, tenantId: draft.tenantId ?? tenantId, gameKey: draft.gameKey ?? gameKey});
        if (validateStory(body).length > 0) return;
        setSaving(true);
        try {
            const saved = await client.saveGraph(body);
            setDraft(saved);
            onSaved?.(saved);
            snackbar.show("Story saved.", "success");
        } catch (error) {
            snackbar.show(messageOf(error), "error");
        } finally {
            setSaving(false);
        }
    };

    const copySnippet = async () => {
        try {
            await navigator.clipboard.writeText(snippet);
            snackbar.show("Copied the StoryBuilder snippet.", "success");
        } catch {
            snackbar.show("Could not copy. Select the snippet instead.", "warning");
        }
    };

    return (
        <LyoStack spacing={2} className="lyo-story-management">
            <LyoStack direction="row" spacing={1} sx={{alignItems: "center", flexWrap: "wrap"}}>
                <h2>{draft.title?.trim() ? draft.title : "Untitled story"}</h2>
                <LyoButton variant="outlined" size="small" onClick={() => {
                    const key = `node_${nodes.length + 1}`;
                    const node: StoryNode = {key, presentationKind: "none", choices: []};
                    setDraft({...draft, startNodeKey: draft.startNodeKey ?? key, nodes: [...nodes, node]});
                    setSelectedKey(key);
                }}>Add node</LyoButton>
                <LyoButton variant="outlined" size="small" disabled={!selected} onClick={() => {
                    if (!selected) return;
                    const key = `choice_${(selected.choices ?? []).length + 1}`;
                    patchNode({...selected, choices: [...(selected.choices ?? []), {key, prompt: key, effects: []}]});
                    setChoiceIndex((selected.choices ?? []).length);
                }}>Add choice</LyoButton>
                <LyoButton variant="outlined" size="small" disabled={!selected} onClick={() => selected && setDraft({...draft, startNodeKey: selected.key})}>Set start</LyoButton>
                <LyoButton variant="contained" size="small" disabled={saving || issues.length > 0} onClick={() => void save()}>Save</LyoButton>
            </LyoStack>
            <LyoStack direction="row" spacing={1} sx={{flexWrap: "wrap"}}>
                <LyoTextField size="small" label="Title" value={draft.title} onChange={(event) => setDraft({...draft, title: event.target.value})} sx={{minWidth: 220}} />
                <LyoTextField size="small" label="Slug" value={draft.slug} onChange={(event) => setDraft({...draft, slug: event.target.value})} sx={{minWidth: 180}} />
                <LyoTextField select size="small" label="Status" value={draft.status ?? "draft"} onChange={(event) => setDraft({...draft, status: event.target.value as StoryStatus})}>
                    {STATUSES.map((status) => <LyoMenuItem key={status} value={status}>{status}</LyoMenuItem>)}
                </LyoTextField>
                <LyoTextField size="small" label="Language" value={draft.language ?? ""} onChange={(event) => setDraft({...draft, language: event.target.value})} />
            </LyoStack>
            <LyoTextField size="small" label="Description" value={draft.description ?? ""} onChange={(event) => setDraft({...draft, description: event.target.value})} />
            {issues.length > 0 ? <LyoAlert severity="warning">{issues.map((issue) => <div key={issue}>{issue}</div>)}</LyoAlert> : null}
            <LyoStack direction="row" spacing={1}>
                <LyoButton variant={tab === 0 ? "contained" : "text"} onClick={() => setTab(0)}>Visual</LyoButton>
                <LyoButton variant={tab === 1 ? "contained" : "text"} onClick={() => setTab(1)}>Request JSON</LyoButton>
                <LyoButton variant={tab === 2 ? "contained" : "text"} onClick={() => setTab(2)}>Play</LyoButton>
            </LyoStack>
            {tab === 0 ? (
                <div className="story-workbench-split">
                    <StoryGraphView draft={draft} selectedNodeKey={selectedKey} onNodeSelected={(key) => { setSelectedKey(key); setChoiceIndex(0); }} />
                    <LyoStack spacing={2}>
                        {selected ? (
                            <>
                                <strong>Node</strong>
                                <StoryNodeEditor node={selected} scenes={scenes} onChange={patchNode} />
                                <strong>Choices</strong>
                                {(selected.choices ?? []).length === 0 ? <span>This node has no choices.</span> : (
                                    <>
                                        <LyoTextField select size="small" label="Choice" value={String(choiceIndex)} onChange={(event) => setChoiceIndex(Number(event.target.value))}>
                                            {(selected.choices ?? []).map((choice, index) => <LyoMenuItem key={choice.key} value={String(index)}>{choice.prompt ?? choice.key}</LyoMenuItem>)}
                                        </LyoTextField>
                                        {selected.choices?.[choiceIndex] ? (
                                            <>
                                                <StoryChoiceEditor
                                                    choice={selected.choices[choiceIndex]}
                                                    fields={fields}
                                                    variables={variables}
                                                    nodes={nodes}
                                                    actions={actions}
                                                    onChange={(choice) => patchNode({...selected, choices: (selected.choices ?? []).map((item, index) => (index === choiceIndex ? choice : item))})}
                                                />
                                                <LyoButton color="error" size="small" onClick={() => patchNode({...selected, choices: (selected.choices ?? []).filter((_, index) => index !== choiceIndex)})}>Remove choice</LyoButton>
                                            </>
                                        ) : null}
                                    </>
                                )}
                                <LyoButton color="error" size="small" onClick={() => {
                                    const next = nodes.filter((node) => node.key !== selected.key);
                                    const start = draft.startNodeKey === selected.key ? next[0]?.key ?? null : draft.startNodeKey;
                                    setDraft({...draft, nodes: next, startNodeKey: start});
                                    setSelectedKey(start ?? null);
                                }}>Remove node</LyoButton>
                            </>
                        ) : <span>Select a node, or add one.</span>}
                        <details>
                            <summary>Scenes</summary>
                            <LyoStack spacing={1}>
                                {scenes.map((scene) => <div key={scene.key}>{scene.key} — {scene.title}</div>)}
                                <LyoButton variant="outlined" size="small" onClick={() => {
                                    const key = `scene_${scenes.length + 1}`;
                                    setDraft({...draft, scenes: [...scenes, {key, title: key, sortOrder: scenes.length}]});
                                }}>Add scene</LyoButton>
                            </LyoStack>
                        </details>
                        <details>
                            <summary>Variables</summary>
                            <StoryVariableEditor variables={variables} onChange={(next) => setDraft({...draft, variables: next})} />
                        </details>
                        <details>
                            <summary>Actions</summary>
                            <StoryActionEditor actions={actions} variables={variables} nodes={nodes} onChange={(next) => setDraft({...draft, actions: next})} />
                        </details>
                        <details>
                            <summary>Reactions</summary>
                            <StoryReactionEditor reactions={reactions} fields={fields} variables={variables} nodes={nodes} actions={actions} onChange={(next) => setDraft({...draft, reactions: next})} />
                        </details>
                    </LyoStack>
                </div>
            ) : null}
            {tab === 1 ? (
                <LyoStack spacing={1}>
                    <JsonEditor value={request} onValueChange={(value) => {
                        if (!value || typeof value !== "object") return;
                        const next = value as StoryGraph;
                        setDraft({...next, id: next.id || draft.id, tenantId: next.tenantId ?? draft.tenantId ?? tenantId, gameKey: next.gameKey ?? draft.gameKey ?? gameKey});
                    }} />
                    <span>The request the save button posts. Choice targets follow the last goto step.</span>
                    <LyoTextField label="StoryBuilder" value={snippet} multiline minRows={8} slotProps={{input: {readOnly: true}}} />
                    <LyoButton variant="outlined" size="small" onClick={() => void copySnippet()}>Copy C#</LyoButton>
                </LyoStack>
            ) : null}
            {tab === 2 ? <StoryPlayView apiClient={apiClient} routePrefix={routePrefix} storyId={draft.id} /> : null}
        </LyoStack>
    );
}
