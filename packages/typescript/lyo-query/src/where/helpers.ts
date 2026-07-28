import type { ComparisonOperator, WhereClause } from "../models/whereClause.js";

/** Runtime guard for BFF / client validation of builder output. */
export function isWhereClause(value: unknown): value is WhereClause {
    if (!value || typeof value !== "object") return false;
    const v = value as Record<string, unknown>;
    if (v.$type === "condition") {
        return typeof v.Field === "string" && typeof v.Comparison === "string";
    }
    if (v.$type === "group") {
        return (
            (v.Operator === "And" || v.Operator === "Or") &&
            Array.isArray(v.Children) &&
            v.Children.every(isWhereClause)
        );
    }
    return false;
}

export function defaultCondition(field = "Id"): WhereClause {
    return {
        $type: "condition",
        Field: field,
        Comparison: "NotEquals",
        Value: null,
    };
}

export function defaultGroup(field = "Id"): WhereClause {
    return {
        $type: "group",
        Operator: "And",
        Children: [defaultCondition(field)],
    };
}

export function parseConditionValue(
    comparison: ComparisonOperator,
    raw: string
): unknown {
    const trimmed = raw.trim();
    if (trimmed === "" || trimmed.toLowerCase() === "null") return null;
    if (trimmed.toLowerCase() === "true") return true;
    if (trimmed.toLowerCase() === "false") return false;
    if (comparison === "In" || comparison === "NotIn") return trimmed;
    if (/^-?\d+(\.\d+)?$/.test(trimmed)) return Number(trimmed);
    return trimmed;
}

export function valueToInput(value: unknown): string {
    if (value === null || value === undefined) return "null";
    if (typeof value === "string") return value;
    return String(value);
}
