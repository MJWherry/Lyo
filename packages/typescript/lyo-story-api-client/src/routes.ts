/** REST paths from `Lyo.Story.Models.Constants.Rest.Story` (no leading slash). */
export const StoryRoutes = {
    route: "Story",
    stories: "Story/Stories",
    graph: "Story/graph",
    graphList: "Story/graph/list",
    playStart: "Story/playthrough/start",
} as const;

export function storyGraphById(id: string): string {
    return `${StoryRoutes.graph}/${id}`;
}

export function storyGraphBySlug(slug: string): string {
    return `${StoryRoutes.graph}/by-slug/${encodeURIComponent(slug)}`;
}

export function storyDeletePath(id: string): string {
    return `${StoryRoutes.stories}/${id}`;
}

export function storyPlayPath(id: string): string {
    return `Story/playthrough/${id}`;
}

export function storyMovesPath(id: string): string {
    return `Story/playthrough/${id}/moves`;
}

export function storyChoosePath(id: string): string {
    return `Story/playthrough/${id}/choose`;
}

export function storyAdvancePath(id: string): string {
    return `Story/playthrough/${id}/advance`;
}

export function storySetVariablesPath(id: string): string {
    return `Story/playthrough/${id}/variables`;
}

export function storyEventsPath(id: string): string {
    return `Story/playthrough/${id}/events`;
}

export function storyCausesPath(id: string, variableKey: string): string {
    return `Story/playthrough/${id}/causes/${encodeURIComponent(variableKey)}`;
}

export function joinRoutePrefix(routePrefix: string | undefined, relativePath: string): string {
    const relative = relativePath.replace(/^\/+/, "");
    const prefix = (routePrefix ?? "").replace(/\/+$/, "").replace(/^\/+/, "");
    return prefix ? `/${prefix}/${relative}` : `/${relative}`;
}
