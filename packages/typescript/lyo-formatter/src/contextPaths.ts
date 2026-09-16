/** Dotted paths of leaf values. Null, blank, and empty input yield no paths. */
export function contextPaths(context: unknown, prefix = ""): string[] {
    if (context == null || context === "") return [];
    if (typeof context !== "object") return prefix ? [prefix] : [];
    if (Array.isArray(context)) {
        return context.flatMap((item, index) => contextPaths(item, prefix ? `${prefix}.${index}` : String(index)));
    }
    const paths: string[] = [];
    for (const [key, value] of Object.entries(context as Record<string, unknown>)) {
        const path = prefix ? `${prefix}.${key}` : key;
        if (value != null && typeof value === "object") paths.push(...contextPaths(value, path));
        else paths.push(path);
    }
    return paths;
}

function readPath(context: unknown, path: string): unknown {
    let current = context;
    for (const part of path.split(".")) {
        if (current == null || typeof current !== "object") return undefined;
        current = (current as Record<string, unknown>)[part];
    }
    return current;
}

/** Substitutes `{path}` from `context`. Unresolved tokens stay in the text. */
export function previewTemplate(template: string, context: unknown): string {
    return template.replace(/\{([^{}]+)\}/g, (match, path: string) => {
        const value = readPath(context, path.trim());
        return value == null ? match : String(value);
    });
}

/** Paths that match the text typed inside an open `{`. */
export function suggestPaths(paths: readonly string[], typed: string): string[] {
    const needle = typed.trim().toLowerCase();
    return paths.filter((path) => path.toLowerCase().startsWith(needle)).slice(0, 12);
}

/** Context paths offered when the caret sits inside an unclosed `{`. */
export function suggestionsAtCaret(template: string, caret: number, context: unknown): string[] {
    const before = template.slice(0, caret);
    const open = before.lastIndexOf("{");
    if (open < 0) return [];
    const typed = before.slice(open + 1);
    if (typed.includes("}") || typed.includes("\n")) return [];
    return suggestPaths(contextPaths(context), typed);
}
