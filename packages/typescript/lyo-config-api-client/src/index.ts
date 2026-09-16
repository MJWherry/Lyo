export type {
    ConfigBindingRecord,
    ConfigBindingRevisionRecord,
    ConfigDefinitionRecord,
    ConfigDefinitionRevisionRecord,
    ConfigResolveResult,
    ConfigRevertRevisionRequest,
    ConfigValue,
    ResolvedConfigItemRecord,
    ResolvedConfigRecord,
} from "./models.js";
export {ConfigQueryRoutes, configPrefix} from "./routes.js";
export {createAsyncConfigApiClient} from "./configClient.js";
export type {AsyncConfigApiClient, ConfigClientOptions, ConfigResolveOptions} from "./configClient.js";
