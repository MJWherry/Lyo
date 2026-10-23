import type {AsyncApiClient} from "lyo-api-client";

/** `POST {route}/Clear`. Default route is `Cache`. */
export function clearCache(api: AsyncApiClient, route = "Cache") {
    const base = route.replace(/^\/+|\/+$/g, "");
    return api.request({method: "POST", path: `${base}/Clear`});
}
