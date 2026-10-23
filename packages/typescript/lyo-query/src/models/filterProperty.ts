import { COMPARISON_OPERATORS, type ComparisonOperator } from "./whereClause.js";

/** Picker group of a `LyoTypeInfo` row (mirrors the server `LyoTypeCategory` enum). */
export type LyoTypeCategory =
    | "Unknown"
    | "Text"
    | "Boolean"
    | "Integer"
    | "Number"
    | "Temporal"
    | "Identifier"
    | "Enum"
    | "Json"
    | "Collection"
    | "Binary";

export type FilterPropertyType =
    | "String"
    | "Number"
    | "DateTime"
    | "DateOnly"
    | "TimeOnly"
    | "Enum"
    | "Bool";

export interface SpUniqueValueCount {
    value: string | null;
    count: number;
}

export interface FilterPropertyDefinition {
    propertyName: string;
    displayName?: string | null;
    type?: FilterPropertyType;
    enumValues?: Record<string, string> | null;
    uniqueValues?: readonly SpUniqueValueCount[] | null;
    schema?: string | null;
    table?: string | null;
    column?: string | null;
}

export function operatorsFor(type: FilterPropertyType = "String"): ComparisonOperator[] {
    switch (type) {
        case "String":
            return [
                "Contains",
                "NotContains",
                "Equals",
                "NotEquals",
                "StartsWith",
                "NotStartsWith",
                "EndsWith",
                "NotEndsWith",
                "In",
                "NotIn",
                "Fuzzy",
                "NotFuzzy",
            ];
        case "Number":
            return [
                "Equals",
                "NotEquals",
                "GreaterThan",
                "GreaterThanOrEqual",
                "LessThan",
                "LessThanOrEqual",
                "In",
                "NotIn",
            ];
        case "Enum":
            return ["Equals", "NotEquals", "In", "NotIn"];
        case "DateTime":
        case "DateOnly":
        case "TimeOnly":
            return [
                "Equals",
                "NotEquals",
                "GreaterThan",
                "GreaterThanOrEqual",
                "LessThan",
                "LessThanOrEqual",
            ];
        case "Bool":
            return ["Equals", "NotEquals"];
        default:
            return [...operatorsFor("String")];
    }
}

/** Operators for a LyoType category. Identifier ids filter like text (fuzzy included); categories without a rule allow every operator. */
export function operatorsForCategory(category: LyoTypeCategory): ComparisonOperator[] {
    switch (category) {
        case "Text":
        case "Identifier":
            return operatorsFor("String");
        case "Integer":
        case "Number":
            return operatorsFor("Number");
        case "Enum":
            return operatorsFor("Enum");
        case "Temporal":
            return operatorsFor("DateTime");
        case "Boolean":
            return operatorsFor("Bool");
        default:
            return [...COMPARISON_OPERATORS];
    }
}

export function filterProperty(
    propertyName: string,
    options: Omit<FilterPropertyDefinition, "propertyName"> = {}
): FilterPropertyDefinition {
    return { propertyName, type: "String", ...options };
}
