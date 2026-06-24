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
import { createApiClient } from "lyo-api-client";

const client = createApiClient({
  baseUrl: "http://localhost:5251",
  token: "optional-token",
  transport: ({ method, url, body, headers }) => {
    // Plug in fetch/axios/k6-http adapter here.
    throw new Error("Provide a transport implementation.");
  },
});
```
