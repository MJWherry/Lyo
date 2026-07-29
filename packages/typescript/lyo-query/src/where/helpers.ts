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

/** Operators whose Value is a list (chip input in the UI). */
export function isMultiValueComparison(comparison: ComparisonOperator): boolean {
    return comparison === "In" || comparison === "NotIn";
}

/** Normalize condition Value into chip strings (array, CSV, or scalar). */
export function toMultiValueStrings(value: unknown): string[] {
    if (value == null || value === "") return [];
    if (Array.isArray(value)) {
        return value
            .map((v) => (v == null ? "" : String(v).trim()))
            .filter((v) => v.length > 0);
    }
    if (typeof value === "string") {
        return value
            .split(/[,;\t\n\r\uFF0C]+/)
            .map((v) => v.trim())
            .filter((v) => v.length > 0);
    }
    return [String(value)];
}

/** Wire Value for In/NotIn — empty list becomes null. */
export function fromMultiValueStrings(values: readonly string[]): string[] | null {
    const list = values.map((v) => v.trim()).filter((v) => v.length > 0);
    return list.length > 0 ? list : null;
}

/**
 * Coerce Value when the comparison operator changes between multi and scalar.
 */
export function coerceValueForComparison(
    comparison: ComparisonOperator,
    value: unknown
): unknown {
    if (isMultiValueComparison(comparison)) {
        return fromMultiValueStrings(toMultiValueStrings(value));
    }
    if (Array.isArray(value)) {
        return value.length === 0 ? null : String(value[0]);
    }
    return value;
}

export function parseConditionValue(
    comparison: ComparisonOperator,
    raw: string
): unknown {
    const trimmed = raw.trim();
    if (trimmed === "" || trimmed.toLowerCase() === "null") return null;
    if (trimmed.toLowerCase() === "true") return true;
    if (trimmed.toLowerCase() === "false") return false;
    if (isMultiValueComparison(comparison)) {
        return fromMultiValueStrings(toMultiValueStrings(trimmed));
    }
    if (/^-?\d+(\.\d+)?$/.test(trimmed)) return Number(trimmed);
    return trimmed;
}

export function valueToInput(value: unknown): string {
    if (value === null || value === undefined) return "null";
    if (Array.isArray(value)) return value.join(", ");
    if (typeof value === "string") return value;
    return String(value);
}
