# lyo-person-api-client

Person API client package built on top of `lyo-api-client`.

## Scope

- Typed contracts for `/person/query` and `/person/QueryProject`
- Typed query builders used by frontend apps and k6 tests
- Source entity type constants
- Runtime response guards for lightweight contract validation

## Usage

```ts
import { createApiClient } from "lyo-api-client";
import {
  baselineQuery,
  createPersonApiClient,
  isQueryRes,
} from "lyo-person-api-client";

const api = createApiClient({
  baseUrl: "http://localhost:5251",
  transport: ({ method, url, body, headers }) => {
    // Plug in runtime transport implementation.
    throw new Error("Provide a transport implementation.");
  },
});

const personApi = createPersonApiClient(api);
const res = personApi.queryPerson(baselineQuery({ start: 0, amount: 10 }));
if (!isQueryRes(res.data)) {
  throw new Error("Unexpected response shape.");
}
```
