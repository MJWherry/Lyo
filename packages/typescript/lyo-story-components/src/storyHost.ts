"use client";

import {useMemo} from "react";
import type {AsyncApiClient} from "lyo-api-client";
import {createAsyncStoryApiClient, type AsyncStoryApiClient} from "lyo-story-api-client";

export const STORY_BUILDER_STORAGE_KEY = "story_builder_state_v1";

export type StorySurfaceProps = {
    /** Base HTTP client, or a client from `createAsyncStoryApiClient`. */
    apiClient?: AsyncApiClient | AsyncStoryApiClient;
    routePrefix?: string;
    tenantId?: string | null;
    gameKey?: string | null;
};

export function useStoryClient(apiClient: StorySurfaceProps["apiClient"], routePrefix?: string): AsyncStoryApiClient | null {
    return useMemo(() => resolveStoryClient(apiClient, routePrefix), [apiClient, routePrefix]);
}

export function resolveStoryClient(apiClient: StorySurfaceProps["apiClient"], routePrefix?: string): AsyncStoryApiClient | null {
    if (!apiClient) return null;
    if (isStoryClient(apiClient)) return apiClient;
    return createAsyncStoryApiClient(apiClient, {routePrefix});
}

function isStoryClient(client: AsyncApiClient | AsyncStoryApiClient): client is AsyncStoryApiClient {
    return typeof (client as AsyncStoryApiClient).saveGraph === "function";
}

export function slugify(title: string): string {
    return title
        .trim()
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, "-")
        .replace(/^-+|-+$/g, "");
}

export function messageOf(error: unknown): string {
    return error instanceof Error ? error.message : "Request failed.";
}
