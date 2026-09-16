/** Wire JSON enum names (`JsonStringEnumConverter` + camelCase). */

export type StoryStatus = "unknown" | "draft" | "published" | "archived";

export type StoryPresentationKind = "none" | "text" | "cutscene";

export type StoryVariableType = "unknown" | "boolean" | "integer" | "string";

export type StoryEffectKind = "unknown" | "set" | "unset" | "increment" | "gotoNode" | "endPlaythrough" | "invokeAction";

export type StoryTrigger =
    | "unknown"
    | "enterNode"
    | "leaveNode"
    | "choiceTaken"
    | "advanced"
    | "variableChanged"
    | "storyStarted";

export type StoryMoveKind = "unknown" | "choice" | "advance";

export type StoryPlaythroughStatus = "unknown" | "inProgress" | "completed" | "abandoned";

export type StoryPlaythroughEventKind =
    | "unknown"
    | "storyStarted"
    | "nodeEntered"
    | "choiceTaken"
    | "advanced"
    | "triggerFired"
    | "actionInvoked"
    | "reactionFired"
    | "variableChanged"
    | "playthroughEnded";

export interface StoryWhere {
    $type?: string;
    Field?: string;
    Comparison?: string;
    Value?: unknown;
    Operator?: string;
    Children?: StoryWhere[];
    Description?: string;
}

export interface StoryEffect {
    kind: StoryEffectKind;
    sortOrder?: number;
    variableKey?: string | null;
    value?: string | null;
    targetNodeKey?: string | null;
    invokeActionKey?: string | null;
}

export interface StoryChoice {
    key: string;
    prompt?: string | null;
    sortOrder?: number;
    targetNodeKey?: string | null;
    visibleWhen?: StoryWhere | null;
    enabledWhen?: StoryWhere | null;
    effects?: StoryEffect[];
}

export interface StoryNode {
    key: string;
    title?: string | null;
    sceneKey?: string | null;
    isEnding?: boolean;
    presentationKind?: StoryPresentationKind;
    body?: string | null;
    contentType?: string | null;
    mediaRef?: string | null;
    choices?: StoryChoice[];
}

export interface StoryScene {
    key: string;
    title?: string | null;
    sortOrder?: number;
}

export interface StoryVariable {
    key: string;
    name?: string | null;
    type: StoryVariableType;
    defaultValue?: string | null;
    description?: string | null;
}

export interface StoryAction {
    key: string;
    name?: string | null;
    effects?: StoryEffect[];
}

export interface StoryReaction {
    key: string;
    trigger: StoryTrigger;
    scopeNodeKey?: string | null;
    scopeVariableKey?: string | null;
    once?: boolean;
    when?: StoryWhere | null;
    effects?: StoryEffect[];
}

export interface StoryGraph {
    id?: string;
    title: string;
    slug: string;
    tenantId?: string | null;
    gameKey?: string | null;
    description?: string | null;
    language?: string | null;
    status?: StoryStatus;
    startNodeKey?: string | null;
    coverImageRef?: string | null;
    createdTimestamp?: string;
    updatedTimestamp?: string | null;
    scenes?: StoryScene[];
    nodes?: StoryNode[];
    variables?: StoryVariable[];
    actions?: StoryAction[];
    reactions?: StoryReaction[];
}

export interface StoryListRequest {
    tenantId?: string | null;
    gameKey?: string | null;
}

export interface StoryStartRequest {
    storyId: string;
    playerRef?: string | null;
}

export interface StoryChooseRequest {
    choiceKey: string;
}

export interface StorySetVariablesRequest {
    values: Record<string, string | null>;
}

export interface StoryPlayVariable {
    variableKey: string;
    value?: string | null;
    updatedAt?: string;
}

export interface StoryPlaySnapshot {
    id: string;
    storyId: string;
    playerRef?: string | null;
    status: StoryPlaythroughStatus;
    currentNodeKey?: string | null;
    startedAt?: string;
    updatedAt?: string | null;
    completedAt?: string | null;
    variables?: StoryPlayVariable[];
}

export interface StoryPlayNode {
    key: string;
    title?: string | null;
    isEnding?: boolean;
    presentationKind?: StoryPresentationKind;
    body?: string | null;
    contentType?: string | null;
    mediaRef?: string | null;
}

export interface StoryMove {
    kind: StoryMoveKind;
    choiceKey?: string | null;
    prompt?: string | null;
    enabled?: boolean;
}

export interface StoryPlayState {
    playthrough: StoryPlaySnapshot;
    currentNode?: StoryPlayNode | null;
    moves?: StoryMove[];
}

export interface StoryPlayEvent {
    id: string;
    sequence: number;
    kind: StoryPlaythroughEventKind;
    nodeKey?: string | null;
    choiceKey?: string | null;
    reactionKey?: string | null;
    actionKey?: string | null;
    variableKey?: string | null;
    oldValue?: string | null;
    newValue?: string | null;
    causedByEventId?: string | null;
    occurredAt?: string;
}
