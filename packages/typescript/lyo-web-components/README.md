# lyo-web-components

MUI-based React component library for Lyo Next.js apps. Mirrors `Lyo.Web.Components` (MudBlazor): theme, query builders, DataGrid wired to Lyo Query, change-tracking forms, and editors.

Peer React 19 + `@emotion/react` / `@emotion/styled`. Depends on `lyo-query` and `lyo-api-client`.

## Setup (Next.js App Router)

```tsx
// app/layout.tsx
import { AppRouterCacheProvider } from "@mui/material-nextjs/v15-appRouter";
import { LyoProvider } from "lyo-web-components";
import "lyo-web-components/styles.css";

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html>
      <body>
        <AppRouterCacheProvider>
          <LyoProvider>{children}</LyoProvider>
        </AppRouterCacheProvider>
      </body>
    </html>
  );
}
```

`LyoProvider` reads CSS variables (`--accent`, `--bg`, `--ink`, `--danger`, `--ok`) and `data-theme` so it matches existing comic/portfolio tokens. Pass `theme` to override.

Do **not** point the grid at TestApi from the browser. Use a BFF:

```tsx
"use client";
import {
  LyoDataGrid,
  LyoDataGridProjected,
  createBffQueryClient,
  defaultPersonGridColumns,
  LyoDataGridFeatureFlags,
} from "lyo-web-components";
import { filterProperty } from "lyo-query";

const client = createBffQueryClient({
  projectPath: "/api/person/grid?mode=project",
  concretePath: "/api/person/grid?mode=concrete",
  queryPath: "/api/person/grid?mode=query",
  exportPath: "/api/person/export",
});

export function PersonGridDemo() {
  return (
    <LyoDataGrid
      mode="project"
      apiClient={client}
      gridKey="PersonGridDemo"
      route="person"
      columns={defaultPersonGridColumns()}
      keySelector={(row) => [row.Id ?? row.id]}
      quickSearchProperties={["FirstName", "LastName"]}
      filterPropertyDefinitions={[
        filterProperty("FirstName"),
        filterProperty("LastName"),
        filterProperty("SourceEntityType"),
      ]}
      features={
        LyoDataGridFeatureFlags.Filterable |
        LyoDataGridFeatureFlags.Searchable |
        LyoDataGridFeatureFlags.AutoRefresh |
        LyoDataGridFeatureFlags.BulkMenu |
        LyoDataGridFeatureFlags.BulkExport
      }
    />
  );
}
```

How the grid maps to Lyo Query:

- Search → OR of `Contains` across `quickSearchProperties`
- Column filters → `ConditionClause` chips ANDed into `whereClause`
- Sort → `SortBy[]`
- Page → `Start` / `Amount`
- Projected grid → `POST {route}/QueryProject` with `Select` from visible columns
- Root Query → `POST /Query` with `From` + alias.property `Select`
- Concrete → `POST {route}/QueryConcrete`

## Query builders

`WhereClauseBuilder`, `QueryBuilder`, and `QueryWorkbench` live here. `lyo-query-components` re-exports them for existing imports.
