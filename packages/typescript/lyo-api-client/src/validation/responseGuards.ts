import type {ProjectedQueryRes, QueryRes} from "../types/results.js";

function isObject(value: unknown): value is Record<string, unknown> {
    return typeof value === "object" && value !== null;
}

export function isQueryRes(value: unknown): value is QueryRes<unknown> {
    if (!isObject(value)) {
        return false;
    }

    return (
        typeof value.isSuccess === "boolean" &&
        "queryRequest" in value &&
        ("items" in value ? Array.isArray(value.items) || value.items === null : true)
    );
}

export function isProjectedQueryRes(value: unknown): value is ProjectedQueryRes<unknown> {
    if (!isObject(value)) {
        return false;
    }

    if (!(typeof value.isSuccess === "boolean" && "queryRequest" in value)) {
        return false;
    }

    if (!("items" in value)) {
        return true;
    }

    if (value.items === null) {
        return true;
    }

    return (
        Array.isArray(value.items) &&
        value.items.every((row) => isObject(row) || row === null)
    );
}

export function isCreateResult(value: unknown): value is {isSuccess: boolean; data?: unknown} {
    return isObject(value) && typeof value.isSuccess === "boolean";
}
