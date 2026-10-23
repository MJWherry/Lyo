# lyo-config-api-client

Config HTTP client. Async factory over `lyo-api-client`.

Resolve reads `GET`, `HEAD`, or `POST /api/config/{kind}/{id}`. A matching `If-None-Match` or `?version` comes back as outcome `304` with no record. Manage routes live under `/api/config/manage`: definitions, bindings, resolved values, revisions, and revert.

Writes send plaintext plus `isEncrypted` on the definition. This package does not encrypt. Optional `apiKey` is sent as `X-Api-Key`.

```ts
import {createAsyncApiClient, fetchTransport} from "lyo-api-client";
import {createAsyncConfigApiClient} from "lyo-config-api-client";

const api = createAsyncApiClient({baseUrl: "http://localhost:5251", transport: fetchTransport});
const config = createAsyncConfigApiClient(api, {apiKey: process.env.LYO_CONFIG_API_KEY});

const resolved = await config.resolve("App", "gateway:local", {ifNoneMatch: etag});
await config.putDefinition({
  subjectEntityType: "App",
  key: "Theme",
  forValueType: "System.String",
  isEncrypted: true,
  defaultValue: {typeName: "System.String", json: '"light"'},
});
```
