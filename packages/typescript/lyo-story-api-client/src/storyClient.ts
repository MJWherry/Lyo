import {ApiClientError, type AsyncApiClient} from "lyo-api-client";
import type {
    StoryChooseRequest,
    StoryGraph,
    StoryListRequest,
    StoryMove,
    StoryPlayEvent,
    StoryPlayState,
    StorySetVariablesRequest,
    StoryStartRequest,
} from "./models.js";
import {
    joinRoutePrefix,
    storyAdvancePath,
    storyCausesPath,
    storyChoosePath,
    storyDeletePath,
    storyEventsPath,
    storyGraphById,
    storyGraphBySlug,
    storyMovesPath,
    storyPlayPath,
    storySetVariablesPath,
    StoryRoutes,
} from "./routes.js";

export interface StoryClientOptions {
    routePrefix?: string;
}

export interface AsyncStoryApiClient {
    saveGraph(request: StoryGraph): Promise<StoryGraph>;
    getGraph(id: string): Promise<StoryGraph | null>;
    getGraphBySlug(slug: string): Promise<StoryGraph | null>;
    listGraphs(request: StoryListRequest): Promise<StoryGraph[]>;
    delete(id: string): Promise<void>;
    start(request: StoryStartRequest): Promise<StoryPlayState>;
    getPlaythrough(playthroughId: string): Promise<StoryPlayState | null>;
    getMoves(playthroughId: string): Promise<StoryMove[]>;
    choose(playthroughId: string, request: StoryChooseRequest): Promise<StoryPlayState>;
    advance(playthroughId: string): Promise<StoryPlayState>;
    setVariables(playthroughId: string, request: StorySetVariablesRequest): Promise<StoryPlayState>;
    getEvents(playthroughId: string): Promise<StoryPlayEvent[]>;
    getCauses(playthroughId: string, variableKey: string): Promise<StoryPlayEvent[]>;
}

export function createAsyncStoryApiClient(api: AsyncApiClient, options?: StoryClientOptions): AsyncStoryApiClient {
    const path = (relative: string) => joinRoutePrefix(options?.routePrefix, relative);
    return {
        async saveGraph(request) {
            const response = await api.request<StoryGraph, StoryGraph>({method: "POST", path: path(StoryRoutes.graph), body: request});
            return required(response.data);
        },
        getGraph(id) {
            return readOrNull(api, path(storyGraphById(id)));
        },
        getGraphBySlug(slug) {
            return readOrNull(api, path(storyGraphBySlug(slug)));
        },
        async listGraphs(request) {
            const response = await api.request<StoryGraph[], StoryListRequest>({method: "POST", path: path(StoryRoutes.graphList), body: request});
            return response.data ?? [];
        },
        async delete(id) {
            await api.request({method: "DELETE", path: path(storyDeletePath(id))});
        },
        async start(request) {
            const response = await api.request<StoryPlayState, StoryStartRequest>({method: "POST", path: path(StoryRoutes.playStart), body: request});
            return required(response.data);
        },
        getPlaythrough(playthroughId) {
            return readOrNull<StoryPlayState>(api, path(storyPlayPath(playthroughId)));
        },
        async getMoves(playthroughId) {
            const response = await api.request<StoryMove[]>({method: "GET", path: path(storyMovesPath(playthroughId))});
            return response.data ?? [];
        },
        async choose(playthroughId, request) {
            const response = await api.request<StoryPlayState, StoryChooseRequest>({
                method: "POST",
                path: path(storyChoosePath(playthroughId)),
                body: request,
            });
            return required(response.data);
        },
        async advance(playthroughId) {
            const response = await api.request<StoryPlayState>({method: "POST", path: path(storyAdvancePath(playthroughId))});
            return required(response.data);
        },
        async setVariables(playthroughId, request) {
            const response = await api.request<StoryPlayState, StorySetVariablesRequest>({
                method: "POST",
                path: path(storySetVariablesPath(playthroughId)),
                body: request,
            });
            return required(response.data);
        },
        async getEvents(playthroughId) {
            const response = await api.request<StoryPlayEvent[]>({method: "GET", path: path(storyEventsPath(playthroughId))});
            return response.data ?? [];
        },
        async getCauses(playthroughId, variableKey) {
            const response = await api.request<StoryPlayEvent[]>({method: "GET", path: path(storyCausesPath(playthroughId, variableKey))});
            return response.data ?? [];
        },
    };
}

function required<T>(value: T | undefined): T {
    if (value === undefined) throw new ApiClientError("The Story API returned an empty body.");
    return value;
}

async function readOrNull<T>(api: AsyncApiClient, path: string): Promise<T | null> {
    try {
        const response = await api.request<T>({method: "GET", path});
        return response.data ?? null;
    } catch (error) {
        if (error instanceof ApiClientError && error.status === 404) return null;
        throw error;
    }
}
