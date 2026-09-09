import type { ComparisonOperator } from "./whereClause.js";

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

export function filterProperty(
    propertyName: string,
    options: Omit<FilterPropertyDefinition, "propertyName"> = {}
): FilterPropertyDefinition {
    return { propertyName, type: "String", ...options };
}
