/** Moves `from` to `to`. Out of range or a no-op returns a copy. */
export function moveItem<T>(items: readonly T[], from: number, to: number): T[] {
    if (from < 0 || to < 0 || from >= items.length || to >= items.length || from === to) return [...items];
    const next = [...items];
    const [item] = next.splice(from, 1);
    next.splice(to, 0, item as T);
    return next;
}

/** Swaps an item with the neighbor below it. */
export function moveDown<T>(items: readonly T[], index: number): T[] {
    return moveItem(items, index, index + 1);
}

export function moveUp<T>(items: readonly T[], index: number): T[] {
    return moveItem(items, index, index - 1);
}

export function showSectionLabel<T>(items: readonly T[], index: number, sectionLabel?: (item: T) => string | null): string | null {
    if (!sectionLabel || index < 0 || index >= items.length) return null;
    const label = sectionLabel(items[index] as T);
    if (!label || !label.trim()) return null;
    if (index === 0) return label;
    return label === sectionLabel(items[index - 1] as T) ? null : label;
}
