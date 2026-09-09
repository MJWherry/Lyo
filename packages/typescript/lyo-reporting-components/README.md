# lyo-reporting-components

React Reporting management UI. Mirrors `Lyo.Reporting.Web.Components` (`ReportManagement`) using `lyo-web-components` and `lyo-reporting-api-client`.

Does **not** include the report design canvas (follow-on).

Peer React 19 + Emotion. Wrap the tree in `LyoProvider`.

## Usage

```tsx
"use client";
import { createAsyncApiClient, fetchTransport } from "lyo-api-client";
import { ReportManagement } from "lyo-reporting-components";
import "lyo-reporting-components/styles.css";

const api = createAsyncApiClient({
  baseUrl: "/api",
  transport: fetchTransport,
});

export function ReportsPage() {
  return <ReportManagement apiClient={api} createdBy="ui" />;
}
```

Optional `downloadFile` / `viewFileUrl` callbacks match the Blazor host FileStorage pattern. Without them, Download uses `GET Reporting/Generation/{id}/Download`.
