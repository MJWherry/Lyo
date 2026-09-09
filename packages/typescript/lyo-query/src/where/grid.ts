import type { ConditionClause, WhereClause } from "../models/whereClause.js";

/** Combine clauses with And. Empty → null; single → that node. */
export function mergeWhere(parts: readonly WhereClause[]): WhereClause | null {
    const filtered = parts.filter(Boolean);
    if (filtered.length === 0) return null;
    if (filtered.length === 1) return filtered[0];
    return { $type: "group", Operator: "And", Children: [...filtered] };
}

export function fromConditions(
    conditions: readonly ConditionClause[],
    searchProperty?: string,
    searchText?: string
): WhereClause | null {
    const nodes: WhereClause[] = [];
    for (const c of conditions) {
        if (!c.Field?.trim()) continue;
        nodes.push(c);
    }
    if (searchProperty && searchText) {
        nodes.push({
            $type: "condition",
            Field: searchProperty,
            Comparison: "Contains",
            Value: searchText,
        });
    }
    if (nodes.length === 0) return null;
    if (nodes.length === 1) return nodes[0];
    return { $type: "group", Operator: "And", Children: nodes };
}

export function buildQuickSearchWhere(
    searchText: string,
    properties: readonly string[]
): WhereClause | null {
    const q = searchText.trim();
    if (!q || properties.length === 0) return null;
    const children: WhereClause[] = properties.map((Field) => ({
        $type: "condition",
        Field,
        Comparison: "Contains",
        Value: q,
    }));
    if (children.length === 1) return children[0];
    return { $type: "group", Operator: "Or", Children: children };
}

/**
 * Grid query where: enabled filter chips AND (quick-search OR across fields),
 * matching Blazor {@code LyoDataGrid.GetQuery}.
 */
export function buildGridWhere(options: {
    filters?: readonly ConditionClause[];
    searchText?: string | null;
    quickSearchProperties?: readonly string[];
}): WhereClause | null {
    const active = (options.filters ?? []).filter((c) => c.Field?.trim());
    const search = options.searchText?.trim();
    const props = options.quickSearchProperties ?? [];
    if (search && props.length > 0) {
        const orChildren = props
            .map((prop) => fromConditions(active, prop, search))
            .filter((n): n is WhereClause => n != null);
        if (orChildren.length === 0) return fromConditions(active);
        if (orChildren.length === 1) return orChildren[0];
        return { $type: "group", Operator: "Or", Children: orChildren };
    }
    return fromConditions(active);
}
