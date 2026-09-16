export type {CrudResource, SyncCrudResource} from "lyo-api-client";
export {createAsyncCrudResource, createSyncCrudResource} from "lyo-api-client";
import {joinRoutePrefix} from "../routes.js";

export interface JobClientOptions {
    routePrefix?: string;
}

export function jobPath(options: JobClientOptions | undefined, relative: string): string {
    return joinRoutePrefix(options?.routePrefix, relative);
}
