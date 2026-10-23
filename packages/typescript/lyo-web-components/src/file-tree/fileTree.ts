export interface FileTreeNode {
    path: string;
    name: string;
    isDirectory: boolean;
}

export interface FileTreeLoader {
    expand(path: string): Promise<readonly FileTreeNode[]>;
}

/** Loads each folder once. A second expand of the same path does not call `listChildren`. */
export function createTreeLoader(listChildren: (path: string) => Promise<readonly FileTreeNode[]>): FileTreeLoader {
    const loaded = new Map<string, readonly FileTreeNode[]>();
    return {
        async expand(path: string) {
            const cached = loaded.get(path);
            if (cached) return cached;
            const children = await listChildren(path);
            loaded.set(path, children);
            return children;
        },
    };
}

export function filterNodes(nodes: readonly FileTreeNode[], query: string): FileTreeNode[] {
    const needle = query.trim().toLowerCase();
    if (!needle) return [...nodes];
    return nodes.filter((node) => node.name.toLowerCase().includes(needle));
}
