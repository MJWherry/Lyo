# lyo-person-api-client

Person API client package built on top of `lyo-api-client`.

## Scope

- Typed contracts for `/person/query` and `/person/QueryProject`
- Typed query builders used by frontend apps and k6 tests
- Source entity type constants
- Runtime response guards for lightweight contract validation

## Usage

```ts
import { createApiClient, createAsyncApiClient } from "lyo-api-client";
import {
  baselineQuery,
  createPersonApiClient,
  createAsyncPersonApiClient,
  isQueryRes,
} from "lyo-person-api-client";

// Sync (k6):
const api = createApiClient({
  baseUrl: "http://localhost:5251",
  transport: ({ method, url, body, headers }) => {
    throw new Error("Provide a transport implementation.");
  },
});
const personApi = createPersonApiClient(api);
const res = personApi.queryPerson(baselineQuery({ start: 0, amount: 10 }));
if (!isQueryRes(res.data)) throw new Error("Unexpected response shape.");

// Async (Next.js BFF):
const asyncApi = createAsyncApiClient({
  baseUrl: process.env.LYO_API_BASE_URL!,
  transport: fetchTransport,
});
const asyncPersonApi = createAsyncPersonApiClient(asyncApi);
const page = await asyncPersonApi.queryPerson(baselineQuery({ amount: 10 }));
```
