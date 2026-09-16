/**
 * Wire-format where clause models for Lyo.Query (camelCase discriminators, PascalCase fields
 * matching the TypeScript Person client builders and typical JSON payloads).
 */

export type ComparisonOperator =
    | "Equals"
    | "NotEquals"
    | "In"
    | "NotIn"
    | "Contains"
    | "NotContains"
    | "StartsWith"
    | "NotStartsWith"
    | "EndsWith"
    | "NotEndsWith"
    | "GreaterThan"
    | "GreaterThanOrEqual"
    | "LessThan"
    | "LessThanOrEqual"
    | "Regex"
    | "NotRegex"
    | "Fuzzy"
    | "NotFuzzy";

export interface ConditionClause {
    $type: "condition";
    Field: string;
    Comparison: ComparisonOperator;
    Value: unknown;
    Threshold?: number;
    /** Nested clause applied after this one (two-phase: this clause in the database, the sub clause in memory). */
    subClause?: WhereClause;
    /** Human-readable label for logs and UI; never evaluated. */
    Description?: string;
}

export interface GroupClause {
    $type: "group";
    Operator: "And" | "Or";
    Children: WhereClause[];
    /** Nested clause applied after this group (see `ConditionClause.subClause`). */
    subClause?: WhereClause;
    /** Human-readable label for logs and UI; never evaluated. */
    Description?: string;
}

export type WhereClause = ConditionClause | GroupClause;

export const COMPARISON_OPERATORS: readonly ComparisonOperator[] = [
    "Equals",
    "NotEquals",
    "In",
    "NotIn",
    "Contains",
    "NotContains",
    "StartsWith",
    "NotStartsWith",
    "EndsWith",
    "NotEndsWith",
    "GreaterThan",
    "GreaterThanOrEqual",
    "LessThan",
    "LessThanOrEqual",
    "Regex",
    "NotRegex",
    "Fuzzy",
    "NotFuzzy",
] as const;
