import type {StoryChoice, StoryEffect, StoryGraph, StoryNode, StoryVariable, StoryWhere} from "lyo-story-api-client";

export const NODE_WIDTH = 220;
export const NODE_HEIGHT = 72;
export const COLUMN_GAP = 96;
export const ROW_GAP = 20;
const EDGE_INSET = 12;

export type StoryGraphNodePlacement = {
    key: string;
    node: StoryNode;
    column: number;
    row: number;
    x: number;
    y: number;
};

export type StoryGraphEdgePlacement = {
    fromKey: string;
    toKey: string;
    choiceKey: string;
    startX: number;
    startY: number;
    endX: number;
    endY: number;
    path: string;
};

export type StoryGraphLayout = {
    nodes: StoryGraphNodePlacement[];
    edges: StoryGraphEdgePlacement[];
    cyclic: string[];
    width: number;
    height: number;
};

export function lastGoto(effects: readonly StoryEffect[] | undefined): string | null {
    const gotos = (effects ?? []).filter((effect) => effect.kind === "gotoNode" && effect.targetNodeKey);
    return gotos.length === 0 ? null : gotos[gotos.length - 1]!.targetNodeKey!;
}

/** Save body. Each choice `targetNodeKey` is the last goto, which is what `StoryChoiceBuilder` writes. */
export function toSaveGraph(draft: StoryGraph): StoryGraph {
    return {
        ...draft,
        nodes: (draft.nodes ?? []).map((node) => ({
            ...node,
            choices: (node.choices ?? []).map((choice, index) => ({...choice, sortOrder: index, targetNodeKey: lastGoto(choice.effects)})),
        })),
    };
}

/** The arrow target and the choice `targetNodeKey` the save body writes. Both are the last goto. */
export function choiceEdge(choice: StoryChoice): {toKey: string | null; targetNodeKey: string | null} {
    const target = lastGoto(choice.effects);
    return {toKey: target, targetNodeKey: target};
}

export function validateStory(draft: StoryGraph): string[] {
    const issues: string[] = [];
    const nodeKeys = keys(draft.nodes?.map((node) => node.key) ?? [], "node", issues);
    keys(draft.scenes?.map((scene) => scene.key) ?? [], "scene", issues);
    keys(draft.variables?.map((variable) => variable.key) ?? [], "variable", issues);
    const actionKeys = keys(draft.actions?.map((action) => action.key) ?? [], "action", issues);
    keys(draft.reactions?.map((reaction) => reaction.key) ?? [], "reaction", issues);
    if ((draft.nodes ?? []).length === 0) issues.push("A story requires at least one node.");
    if (draft.startNodeKey && !nodeKeys.has(draft.startNodeKey)) issues.push(`Start node '${draft.startNodeKey}' is not defined.`);
    for (const node of draft.nodes ?? []) {
        keys((node.choices ?? []).map((choice) => choice.key), `choice on node '${node.key}'`, issues);
        checkEffects(node.choices?.flatMap((choice) => choice.effects ?? []) ?? [], nodeKeys, actionKeys, issues);
    }
    for (const action of draft.actions ?? []) checkEffects(action.effects ?? [], nodeKeys, actionKeys, issues);
    for (const reaction of draft.reactions ?? []) checkEffects(reaction.effects ?? [], nodeKeys, actionKeys, issues);
    return issues;
}

export function layoutStory(draft: StoryGraph): StoryGraphLayout {
    const nodes = uniqueNodes(draft.nodes ?? []);
    const byKey = new Map(nodes.map((node) => [node.key, node]));
    const links = linksOf(nodes, byKey);
    const {levels, cyclic} = layer(nodes, links);
    const placed = place(levels);
    const edges = buildEdges(placed, links);
    return {
        nodes: placed,
        edges,
        cyclic,
        width: placed.length === 0 ? 0 : Math.max(...placed.map((node) => node.x)) + NODE_WIDTH,
        height: placed.length === 0 ? 0 : Math.max(...placed.map((node) => node.y)) + NODE_HEIGHT,
    };
}

export function toStoryBuilderCsharp(draft: StoryGraph): string {
    const lines: string[] = [`var story = StoryBuilder.New(${quote(draft.title)})`];
    if (draft.slug) lines.push(`    .Slug(${quote(draft.slug)})`);
    if (draft.description) lines.push(`    .Description(${quote(draft.description)})`);
    if (draft.language) lines.push(`    .Language(${quote(draft.language)})`);
    for (const variable of draft.variables ?? [])
        lines.push(`    .Variable(${quote(variable.key)}, StoryVariableType.${pascal(variable.type)}, ${defaultLiteral(variable)})`);
    for (const scene of draft.scenes ?? []) lines.push(`    .Scene(${quote(scene.key)}, ${quote(scene.title ?? "")})`);
    for (const action of draft.actions ?? []) {
        lines.push(`    .Action(${quote(action.key)}, a => a`);
        appendDo(lines, "        ", action.effects ?? []);
        lines.push(")");
    }
    const ordered = [...(draft.nodes ?? [])].sort((left, right) => (left.key === draft.startNodeKey ? -1 : right.key === draft.startNodeKey ? 1 : 0));
    for (const node of ordered) appendNode(lines, node, node.key === draft.startNodeKey);
    lines.push("    .Build();");
    return lines.join("\n");
}

export function conditionFields(draft: StoryGraph): string[] {
    const fields: string[] = [];
    for (const variable of draft.variables ?? []) if (variable.key) fields.push(`var.${variable.key}`);
    for (const node of draft.nodes ?? []) {
        if (!node.key) continue;
        fields.push(`visited.${node.key}`);
        for (const choice of node.choices ?? []) if (choice.key) fields.push(`chose.${choice.key}`);
    }
    return fields;
}

function keys(values: string[], kind: string, issues: string[]): Set<string> {
    const seen = new Set<string>();
    for (const key of values) {
        if (!key?.trim()) {
            issues.push(`A ${kind} key is required.`);
            continue;
        }
        if (seen.has(key)) issues.push(`Duplicate ${kind} key '${key}'.`);
        seen.add(key);
    }
    return seen;
}

function checkEffects(effects: StoryEffect[], nodeKeys: Set<string>, actionKeys: Set<string>, issues: string[]) {
    for (const effect of effects) {
        if (effect.kind === "gotoNode" && effect.targetNodeKey && !nodeKeys.has(effect.targetNodeKey))
            issues.push(`Goto targets unknown node '${effect.targetNodeKey}'.`);
        if (effect.kind === "invokeAction" && effect.invokeActionKey && !actionKeys.has(effect.invokeActionKey))
            issues.push(`Invoke targets unknown action '${effect.invokeActionKey}'.`);
    }
}

function uniqueNodes(nodes: StoryNode[]): StoryNode[] {
    const seen = new Set<string>();
    const result: StoryNode[] = [];
    for (const node of nodes) {
        if (!node.key || seen.has(node.key)) continue;
        seen.add(node.key);
        result.push(node);
    }
    return result;
}

function linksOf(nodes: StoryNode[], byKey: Map<string, StoryNode>): Array<{from: string; to: string; choiceKey: string}> {
    const links: Array<{from: string; to: string; choiceKey: string}> = [];
    for (const node of nodes) {
        for (const choice of node.choices ?? []) {
            const target = lastGoto(choice.effects);
            if (target && byKey.has(target)) links.push({from: node.key, to: target, choiceKey: choice.key});
        }
    }
    return links;
}

function layer(nodes: StoryNode[], links: Array<{from: string; to: string}>) {
    const incoming = new Map(nodes.map((node) => [node.key, new Set<string>()]));
    for (const link of links) incoming.get(link.to)?.add(link.from);
    const levels: StoryNode[][] = [];
    const placed = new Set<string>();
    let remaining = [...nodes];
    while (remaining.length > 0) {
        const ready = remaining.filter((node) => [...(incoming.get(node.key) ?? [])].every((key) => placed.has(key)));
        if (ready.length === 0) return {levels, cyclic: remaining.map((node) => node.key)};
        levels.push(ready);
        for (const node of ready) {
            placed.add(node.key);
            remaining = remaining.filter((item) => item.key !== node.key);
        }
    }
    return {levels, cyclic: [] as string[]};
}

function place(levels: StoryNode[][]): StoryGraphNodePlacement[] {
    const nodes: StoryGraphNodePlacement[] = [];
    levels.forEach((level, column) => {
        level.forEach((node, row) => {
            nodes.push({key: node.key, node, column, row, x: column * (NODE_WIDTH + COLUMN_GAP), y: row * (NODE_HEIGHT + ROW_GAP)});
        });
    });
    return nodes;
}

function buildEdges(nodes: StoryGraphNodePlacement[], links: Array<{from: string; to: string; choiceKey: string}>): StoryGraphEdgePlacement[] {
    const placed = new Map(nodes.map((node) => [node.key, node]));
    const pairs = links
        .filter((link) => placed.has(link.from) && placed.has(link.to))
        .map((link) => ({from: placed.get(link.from)!, to: placed.get(link.to)!, choiceKey: link.choiceKey}));
    return pairs.map((pair) => {
        const exits = pairs.filter((item) => item.from === pair.from);
        const entries = pairs.filter((item) => item.to === pair.to);
        return connect(pair.from, pair.to, pair.choiceKey, exits.indexOf(pair), exits.length, entries.indexOf(pair), entries.length);
    });
}

function connect(
    from: StoryGraphNodePlacement,
    to: StoryGraphNodePlacement,
    choiceKey: string,
    exitIndex: number,
    exitCount: number,
    entryIndex: number,
    entryCount: number
): StoryGraphEdgePlacement {
    const startX = from.x + NODE_WIDTH;
    const startY = spread(from.y, exitCount, exitIndex);
    const endX = to.x;
    const endY = spread(to.y, entryCount, entryIndex);
    let channelX = endX - COLUMN_GAP / 2 - entryIndex * 8;
    if (channelX <= startX) channelX = startX + Math.max((endX - startX) / 2, 1);
    return {fromKey: from.key, toKey: to.key, choiceKey, startX, startY, endX, endY, path: `M ${startX} ${startY} L ${channelX} ${startY} L ${channelX} ${endY} L ${endX} ${endY}`};
}

function spread(origin: number, count: number, index: number): number {
    if (count <= 1) return origin + NODE_HEIGHT / 2;
    const span = NODE_HEIGHT - 2 * EDGE_INSET;
    return origin + EDGE_INSET + Math.round(span * (index / (count - 1)));
}

function appendNode(lines: string[], node: StoryNode, start: boolean) {
    lines.push(`    .${start ? "StartNode" : "Node"}(${quote(node.key)}, n => n`);
    if (node.title) lines.push(`        .Titled(${quote(node.title)})`);
    if (node.sceneKey) lines.push(`        .InScene(${quote(node.sceneKey)})`);
    if (node.isEnding) lines.push("        .Ending()");
    if (node.presentationKind === "text" && node.body) lines.push(`        .Text(${quote(node.body)})`);
    if (node.presentationKind === "cutscene" && node.mediaRef) lines.push(`        .Cutscene(${quote(node.mediaRef)})`);
    for (const choice of node.choices ?? []) appendChoice(lines, choice);
    lines.push(")");
}

function appendChoice(lines: string[], choice: StoryChoice) {
    lines.push(`        .Choice(${quote(choice.key)}, ${quote(choice.prompt ?? choice.key)}, c => c`);
    appendWhen(lines, "            ", ".WhenAll", ".WhenOneOf", choice.enabledWhen);
    appendWhen(lines, "            ", ".VisibleWhenAll", ".VisibleWhenOneOf", choice.visibleWhen);
    if ((choice.effects ?? []).length > 0) appendDo(lines, "            ", choice.effects ?? []);
    lines.push(")");
}

function appendDo(lines: string[], indent: string, effects: StoryEffect[]) {
    if (effects.length === 0) return;
    lines.push(`${indent}.Do(d => d`);
    for (const effect of effects) lines.push(`${indent}    ${effectCall(effect)}`);
    lines.push(`${indent})`);
}

function effectCall(effect: StoryEffect): string {
    switch (effect.kind) {
        case "set":
            return `.Set(${quote(effect.variableKey)}, ${valueLiteral(effect.value)})`;
        case "unset":
            return `.Unset(${quote(effect.variableKey)})`;
        case "increment":
            return `.Increment(${quote(effect.variableKey)}${effect.value && effect.value !== "1" ? `, ${effect.value}` : ""})`;
        case "gotoNode":
            return `.GoesTo(${quote(effect.targetNodeKey)})`;
        case "invokeAction":
            return `.Invoke(${quote(effect.invokeActionKey)})`;
        default:
            return ".End()";
    }
}

function appendWhen(lines: string[], indent: string, all: string, one: string, clause: StoryWhere | null | undefined) {
    if (!clause) return;
    const method = clause.$type === "group" && clause.Operator === "Or" ? one : all;
    const leaves: string[] = [];
    const children = clause.$type === "group" ? clause.Children ?? [] : [clause];
    for (const child of children) {
        const leaf = leafCall(child);
        if (leaf) leaves.push(leaf);
    }
    lines.push(`${indent}${method}(w => w`);
    for (const leaf of leaves) lines.push(`                ${leaf}`);
    lines.push(")");
}

function leafCall(clause: StoryWhere): string | null {
    if (clause.$type === "group" || clause.Comparison !== "Equals" || !clause.Field) return null;
    if (clause.Field.startsWith("var.")) {
        const key = quote(clause.Field.slice(4));
        if (isTrue(clause.Value)) return `.Flag(${key})`;
        if (isFalse(clause.Value)) return `.NotFlag(${key})`;
        return `.Eq(${key}, ${valueLiteral(clause.Value)})`;
    }
    if (clause.Field.startsWith("chose.") && isTrue(clause.Value)) return `.Chose(${quote(clause.Field.slice(6))})`;
    if (clause.Field.startsWith("visited.") && isTrue(clause.Value)) return `.Visited(${quote(clause.Field.slice(8))})`;
    return null;
}

function defaultLiteral(variable: StoryVariable): string {
    if (variable.type === "boolean") return isTrue(variable.defaultValue) ? "true" : "false";
    if (variable.type === "integer") return Number.isInteger(Number(variable.defaultValue)) ? String(Number(variable.defaultValue)) : "0";
    return quote(variable.defaultValue ?? "");
}

function valueLiteral(value: unknown): string {
    if (value == null) return "null";
    if (isTrue(value)) return "true";
    if (isFalse(value)) return "false";
    if (typeof value === "number") return String(value);
    return quote(String(value));
}

function isTrue(value: unknown): boolean {
    return value === true || String(value).toLowerCase() === "true";
}

function isFalse(value: unknown): boolean {
    return value === false || String(value).toLowerCase() === "false";
}

function pascal(value: string): string {
    return value.length === 0 ? value : value[0]!.toUpperCase() + value.slice(1);
}

function quote(value: string | null | undefined): string {
    return `"${(value ?? "").replace(/\\/g, "\\\\").replace(/"/g, '\\"')}"`;
}
