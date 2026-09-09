# lyo-job-components

React Job management UI. Mirrors `Lyo.Job.Web.Components` (`JobManagement`) using `lyo-web-components` and `lyo-job-api-client`.

Peer React 19 + Emotion. Wrap the tree in `LyoProvider`.

## Usage

```tsx
"use client";
import { createAsyncApiClient, fetchTransport } from "lyo-api-client";
import { JobManagement } from "lyo-job-components";
import "lyo-job-components/styles.css";

const api = createAsyncApiClient({
  baseUrl: "/api",
  transport: fetchTransport,
});

export function JobsPage() {
  return <JobManagement apiClient={api} createdBy="ui" />;
}
```

Tabs: Statistics (optional `statisticsPath`), Definitions, Schedules, Runs, Workers, Workflows.
Do not point the grid at the API from the browser without a BFF unless CORS and cookies are already set.
