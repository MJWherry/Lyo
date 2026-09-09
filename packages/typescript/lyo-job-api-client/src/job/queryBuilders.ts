import {defaultQueryOptions} from "lyo-query";
import type {ProjectionQueryReq, QueryConcreteReq} from "lyo-query";
import {
    JOB_DEFINITION_EDITOR_INCLUDES,
    JOB_DEFINITION_GRID_SELECT,
    JOB_RUN_DETAIL_INCLUDES,
    JOB_RUN_GRID_SELECT,
} from "../includes.js";

export function emptyConcreteQuery(amount = 50): QueryConcreteReq {
    return {
        Amount: amount,
        Options: defaultQueryOptions({TotalCountMode: "Exact"}),
    };
}

export function definitionEditorQuery(id: string): QueryConcreteReq {
    return {
        Keys: [[id]],
        Amount: 1,
        Include: [...JOB_DEFINITION_EDITOR_INCLUDES],
        Options: defaultQueryOptions(),
    };
}

export function runDetailQuery(id: string): QueryConcreteReq {
    return {
        Keys: [[id]],
        Amount: 1,
        Include: [...JOB_RUN_DETAIL_INCLUDES],
        Options: defaultQueryOptions(),
    };
}

export function definitionGridQuery(amount = 50): ProjectionQueryReq {
    return {
        Amount: amount,
        Select: [...JOB_DEFINITION_GRID_SELECT],
        Options: defaultQueryOptions({TotalCountMode: "Exact"}),
    };
}

export function runGridQuery(amount = 50): ProjectionQueryReq {
    return {
        Amount: amount,
        Select: [...JOB_RUN_GRID_SELECT],
        Options: defaultQueryOptions({TotalCountMode: "Exact"}),
    };
}
