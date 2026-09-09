# lyo-job-api-client

Job API client package built on top of `lyo-api-client` and `lyo-query`.

## Scope

- Typed contracts for Job definitions, runs, workers, schedules, triggers, blackouts, and workflows
- QueryConcrete / QueryProject plus special lifecycle routes (`Create`, `Cancel`, `Rerun`, Stats, …)
- Include-path constants used by the Blazor job editor

## Usage

```ts
import { createAsyncApiClient, fetchTransport } from "lyo-api-client";
import {
  createAsyncJobApiClient,
  emptyConcreteQuery,
  JOB_DEFINITION_EDITOR_INCLUDES,
} from "lyo-job-api-client";

const api = createAsyncApiClient({
  baseUrl: process.env.LYO_API_BASE_URL!,
  transport: fetchTransport,
});
const jobs = createAsyncJobApiClient(api);

const page = await jobs.definitions.queryConcrete(emptyConcreteQuery(20));
const run = await jobs.runs.create({
  jobDefinitionId: "…",
  createdBy: "ui",
  allowTriggers: true,
});
await jobs.runs.cancel(run.data!.data!.id);
```

Query request bodies keep PascalCase (`Amount`, `Include`) from `lyo-query`. Domain DTOs are camelCase to match `LyoJsonSerializerOptions`.
