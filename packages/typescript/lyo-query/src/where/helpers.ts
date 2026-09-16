import type { ComparisonOperator, ConditionClause, WhereClause } from "../models/whereClause.js";

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

export function isFuzzyComparison(comparison: ComparisonOperator): boolean {
    return comparison === "Fuzzy" || comparison === "NotFuzzy";
}

/** Server default similarity threshold when a Fuzzy condition omits `Threshold`. */
export const DEFAULT_FUZZY_THRESHOLD = 0.3;

/** Clamps a threshold input to 0..1. Blank, null, or non-numeric input means "use the server default" (undefined). */
export function normalizeThreshold(value: number | string | null | undefined): number | undefined {
    if (value == null || (typeof value === "string" && value.trim() === "")) return undefined;
    const n = typeof value === "number" ? value : Number(value);
    if (!Number.isFinite(n)) return undefined;
    return Math.min(1, Math.max(0, n));
}

function valueEquals(a: unknown, b: unknown): boolean {
    if (a === b) return true;
    if (Array.isArray(a) && Array.isArray(b)) return a.length === b.length && a.every((x, i) => valueEquals(x, b[i]));
    return false;
}

/** Structural equality matching the server `ConditionClause.Equals`: field, comparison, threshold, and value (lists item by item). */
export function conditionEquals(a: ConditionClause, b: ConditionClause): boolean {
    return a.Field === b.Field && a.Comparison === b.Comparison && a.Threshold === b.Threshold && valueEquals(a.Value, b.Value);
}

const COMPARISON_DESCRIPTIONS: Record<ComparisonOperator, string> = {
    Equals: "=",
    NotEquals: "≠",
    Contains: "Contains",
    NotContains: "Not Contains",
    StartsWith: "Starts With",
    NotStartsWith: "Not Starts With",
    EndsWith: "Ends With",
    NotEndsWith: "Not Ends With",
    GreaterThan: ">",
    GreaterThanOrEqual: "≥",
    LessThan: "<",
    LessThanOrEqual: "≤",
    In: "In",
    NotIn: "Not In",
    Regex: "Regex",
    NotRegex: "Not Regex",
    Fuzzy: "Fuzzy",
    NotFuzzy: "Not Fuzzy",
};

/** Short operator label used in chips (`=`, `≥`, `Not Contains`), matching the server enum descriptions. */
export function comparisonDescription(comparison: ComparisonOperator): string {
    return COMPARISON_DESCRIPTIONS[comparison] ?? comparison;
}

/** ` threshold=0.4` for fuzzy comparisons with an explicit threshold, otherwise "". */
export function thresholdSuffix(condition: Pick<ConditionClause, "Comparison" | "Threshold">): string {
    return isFuzzyComparison(condition.Comparison) && condition.Threshold != null ? ` threshold=${condition.Threshold}` : "";
}

/** Leaf Fuzzy condition. Omit `threshold` to use the server default (0.3). */
export function fuzzy(field: string, value: string, threshold?: number): ConditionClause {
    return {
        $type: "condition",
        Field: field,
        Comparison: "Fuzzy",
        Value: value,
        ...(threshold !== undefined ? { Threshold: threshold } : {}),
    };
}

/** Leaf NotFuzzy condition. Omit `threshold` to use the server default (0.3). */
export function notFuzzy(field: string, value: string, threshold?: number): ConditionClause {
    return {
        $type: "condition",
        Field: field,
        Comparison: "NotFuzzy",
        Value: value,
        ...(threshold !== undefined ? { Threshold: threshold } : {}),
    };
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
