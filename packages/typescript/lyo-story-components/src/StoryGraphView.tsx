"use client";

import type {StoryGraph} from "lyo-story-api-client";
import {LyoAlert, LyoPaper} from "lyo-web-components";
import {layoutStory, NODE_HEIGHT, NODE_WIDTH} from "./storyGraph.js";

const SCENE_STROKES = ["#0288d1", "#2e7d32", "#ed6c02", "#9c27b0"];

function pointsOf(path: string): string {
    const tokens = path.split(/\s+/);
    const pairs: string[] = [];
    for (let index = 0; index < tokens.length; index++) {
        if (tokens[index] === "M" || tokens[index] === "L") {
            pairs.push(`${tokens[index + 1]},${tokens[index + 2]}`);
            index += 2;
        }
    }
    return pairs.join(" ");
}

export function StoryGraphView({draft, selectedNodeKey, onNodeSelected}: {draft: StoryGraph; selectedNodeKey: string | null; onNodeSelected: (key: string) => void}) {
    const layout = layoutStory(draft);
    if (layout.nodes.length === 0 && layout.cyclic.length === 0)
        return <LyoPaper sx={{p: 2}}>No nodes yet. Add one to see the graph.</LyoPaper>;

    const scenes = draft.scenes ?? [];
    return (
        <LyoPaper sx={{p: 2}}>
            {layout.nodes.length > 0 ? (
                <div className="story-graph-scroll">
                    <svg className="story-graph-svg" width={layout.width} height={layout.height} viewBox={`0 0 ${layout.width} ${layout.height}`}>
                        {layout.edges.map((edge) => (
                            <g key={`${edge.fromKey}-${edge.choiceKey}`}>
                                <polyline points={pointsOf(edge.path)} className="story-graph-edge" />
                                <polygon points={`${edge.endX},${edge.endY} ${edge.endX - 8},${edge.endY - 4} ${edge.endX - 8},${edge.endY + 4}`} className="story-graph-arrow" />
                            </g>
                        ))}
                        {layout.nodes.map((node) => {
                            const selected = node.key === selectedNodeKey;
                            const sceneIndex = scenes.findIndex((scene) => scene.key === node.node.sceneKey);
                            const stroke = selected ? "currentColor" : sceneIndex >= 0 ? SCENE_STROKES[sceneIndex % SCENE_STROKES.length] : "currentColor";
                            const title = node.node.title?.trim() ? node.node.title : node.key;
                            const caption = [node.key === draft.startNodeKey ? "Start" : "", node.node.isEnding ? "Ending" : "", node.node.presentationKind ?? "none"].filter(Boolean).join(" · ");
                            return (
                                <g key={node.key} className="story-graph-node-clickable" role="button" tabIndex={0} aria-label={`Select node ${node.key}`} onClick={() => onNodeSelected(node.key)} onKeyDown={(event) => { if (event.key === "Enter" || event.key === " ") onNodeSelected(node.key); }}>
                                    <title>{node.key}</title>
                                    <rect x={node.x} y={node.y} width={NODE_WIDTH} height={NODE_HEIGHT} rx={4} className="story-graph-node-shape" stroke={stroke} strokeWidth={selected ? 2 : 1} />
                                    <text x={node.x + NODE_WIDTH / 2} y={node.y + NODE_HEIGHT * 0.42} textAnchor="middle" className="story-graph-label">{title}</text>
                                    <text x={node.x + NODE_WIDTH / 2} y={node.y + NODE_HEIGHT * 0.72} textAnchor="middle" className="story-graph-caption">{caption}</text>
                                </g>
                            );
                        })}
                    </svg>
                </div>
            ) : null}
            {layout.cyclic.length > 0 ? <LyoAlert severity="error">Circular choice path between: {layout.cyclic.join(", ")}. These nodes are left off the graph.</LyoAlert> : null}
        </LyoPaper>
    );
}
