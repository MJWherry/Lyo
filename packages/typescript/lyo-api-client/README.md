# lyo-api-client

Core, reusable API client foundation for Lyo TypeScript consumers.

## Scope

- Generic request/response types
- Shared auth header helpers
- Request execution abstraction via transport adapters
- Normalized API errors

This package intentionally has no domain-specific endpoints.

## Usage

```ts
import { createApiClient, createAsyncApiClient } from "lyo-api-client";

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
  transport: async ({ method, url, body, headers }) => {
    const res = await fetch(url, { method, headers, body });
    const rawBody = await res.text();
    let data;
    try { data = JSON.parse(rawBody); } catch { /* ignore */ }
    return { status: res.status, ok: res.ok, data, rawBody };
  },
});
```
