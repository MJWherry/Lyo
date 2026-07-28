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
    | "StartsWith"
    | "EndsWith"
    | "GreaterThan"
    | "GreaterThanOrEqual"
    | "LessThan"
    | "LessThanOrEqual"
    | "Regex";

export interface ConditionClause {
    $type: "condition";
    Field: string;
    Comparison: ComparisonOperator;
    Value: unknown;
    subClause?: WhereClause;
}

export interface GroupClause {
    $type: "group";
    Operator: "And" | "Or";
    Children: WhereClause[];
}

export type WhereClause = ConditionClause | GroupClause;

export const COMPARISON_OPERATORS: readonly ComparisonOperator[] = [
    "Equals",
    "NotEquals",
    "In",
    "NotIn",
    "Contains",
    "StartsWith",
    "EndsWith",
    "GreaterThan",
    "GreaterThanOrEqual",
    "LessThan",
    "LessThanOrEqual",
    "Regex",
] as const;
