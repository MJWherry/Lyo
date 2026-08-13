# lyo-api-client

Core, reusable API client foundation for Lyo TypeScript consumers.

## Scope

- Generic request/response types
- Shared auth header helpers
- Request execution abstraction via transport adapters (`fetchTransport` forwards `AbortSignal`)
- Normalized API errors (`isAbortError` / `CLIENT_CLOSED_REQUEST_STATUS` for client disconnect)
- Lyo.Api metadata helpers (`getMetadata` / `getCrudMetadata` / `getEntityMetadata`)
- Generic CRUD / QueryProject helpers (`queryProject`, `create`, `update`, `deleteById`)
- Query/CRUD result types (`QueryRes`, `ProjectedQueryRes`, `CreateResult`, …) and response guards

Domain-specific Person endpoints live in `lyo-person-api-client`. Comic-specific
reads (slug, nested lists, tags, files) live in `lyo-comic-api-client`.

## Usage

```ts
import { createApiClient, createAsyncApiClient, fetchTransport } from "lyo-api-client";

// Sync transport (k6):
const client = createApiClient({
  baseUrl: "http://localhost:5251",
  token: "optional-token",
  transport: ({ method, url, body, headers }) => {
    // Plug in k6-http adapter here.
    throw new Error("Provide a transport implementation.");
  },
});

// Async transport (Node / Next.js / browsers):
const asyncClient = createAsyncApiClient({
  baseUrl: "http://localhost:5251",
  transport: async ({ method, url, body, headers, signal }) => {
    const res = await fetch(url, { method, headers, body, signal });
    const rawBody = await res.text();
    let data;
    try { data = JSON.parse(rawBody); } catch { /* ignore */ }
    return { status: res.status, ok: res.ok, data, rawBody };
  },
  // Or: transport: fetchTransport, signal: incomingRequest.signal
});

// Typed CreateBuilder metadata (e.g. Person):
const meta = await asyncClient.getMetadata("person");
// Dynamic CRUD registry / per-entity:
// await asyncClient.getCrudMetadata("Twilio");
// await asyncClient.getEntityMetadata("Twilio", "TwilioSmsLogEntity");

// Generic Lyo.Api CRUD / QueryProject (domain clients wrap these):
// await asyncClient.queryProject("/api/comic/series", projectionReq);
// await asyncClient.create("/api/comic/chapters", body);
// await asyncClient.update("/api/comic/chapters", [id], body);
// await asyncClient.deleteById("/api/comic/series", id);
```

