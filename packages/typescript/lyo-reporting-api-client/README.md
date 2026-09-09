# lyo-reporting-api-client

Reporting API client package built on top of `lyo-api-client` and `lyo-query`.

## Scope

- Report definition and parameter CRUD + QueryConcrete / QueryProject
- Generate, rerun, download, and delete generations
- `POST Reporting/ResolveParameterOptions`

## Usage

```ts
import { createAsyncApiClient, fetchTransport } from "lyo-api-client";
import { createAsyncReportingApiClient, emptyConcreteQuery } from "lyo-reporting-api-client";

const api = createAsyncApiClient({
  baseUrl: process.env.LYO_API_BASE_URL!,
  transport: fetchTransport,
});
const reports = createAsyncReportingApiClient(api);

const defs = await reports.definitions.queryConcrete(emptyConcreteQuery(20));
const generation = await reports.generations.generate({
  reportDefinitionId: defs.data?.items?.[0]?.id,
  format: "pdf",
});
const file = await reports.generations.download(generation.data!.id);
```
