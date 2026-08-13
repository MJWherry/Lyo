"use client";

import { useState } from "react";
import { filterProperty } from "lyo-query";
import {
  LyoDataGrid,
  LyoDataGridFeatureFlags,
  LyoTab,
  LyoTabs,
  createBffQueryClient,
  defaultPersonGridColumns,
  type LyoDataGridMode,
} from "lyo-web-components";
import { ClientCodeBlock } from "@/components/ClientCodeBlock";

const client = createBffQueryClient({
  projectPath: "/api/person/grid?mode=project",
  concretePath: "/api/person/grid?mode=concrete",
  queryPath: "/api/person/grid?mode=query",
  exportPath: "/api/person/export",
});

const FEATURES =
  LyoDataGridFeatureFlags.Filterable |
  LyoDataGridFeatureFlags.Searchable |
  LyoDataGridFeatureFlags.AutoRefresh |
  LyoDataGridFeatureFlags.BulkMenu |
  LyoDataGridFeatureFlags.BulkExport;

const FILTERS = [
  filterProperty("FirstName"),
  filterProperty("LastName"),
  filterProperty("SourceEntityType"),
];

const MODE_COPY: Record<LyoDataGridMode, string> = {
  query:
    "Root Query posts a From/Select QueryReq through the BFF (POST /Query). Select is alias.property (p.FirstName).",
  project:
    "Projection posts a ProjectionQueryReq (POST person/QueryProject). Select comes from visible columns.",
  concrete:
    "Concrete posts a QueryConcreteReq (POST person/QueryConcrete) and returns full Person graphs.",
};

export function PersonDataGridDemo() {
  const [mode, setMode] = useState<LyoDataGridMode>("project");

  return (
    <div style={{ minWidth: 0, maxWidth: "100%" }}>
      <p className="muted" style={{ marginTop: 0 }}>
        Search is debounced (300ms) and becomes an OR of <code>Contains</code> on FirstName /
        LastName. Column filters are ANDed into <code>whereClause</code>. Multi-column sort maps to{" "}
        <code>SortBy</code> with priority. Paging is <code>Start</code> / <code>Amount</code>. The
        browser never talks to TestApi. Bulk delete/patch are off; Bulk includes CSV/XLSX export.
      </p>
      <LyoTabs
        value={mode}
        onChange={(_, next) => setMode(next as LyoDataGridMode)}
        sx={{ mb: 2 }}
      >
        <LyoTab value="query" label="Root Query" />
        <LyoTab value="project" label="Projection" />
        <LyoTab value="concrete" label="Concrete" />
      </LyoTabs>
      <p className="muted">{MODE_COPY[mode]}</p>
      <ClientCodeBlock
        language="tsx"
        code={`<LyoDataGrid
  mode="${mode}"
  apiClient={client}
  gridKey="PersonGridDemo:${mode}"
  route="person"
  columns={defaultPersonGridColumns()}
  keySelector={(row) => [row.Id ?? row.id]}
  quickSearchProperties={["FirstName", "LastName"]}
/>`}
      />
      <LyoDataGrid
        key={mode}
        mode={mode}
        apiClient={client}
        gridKey={`PersonGridDemo:${mode}`}
        route="person"
        columns={defaultPersonGridColumns()}
        keySelector={(row) => [row.Id ?? row.id]}
        quickSearchProperties={["FirstName", "LastName"]}
        filterPropertyDefinitions={FILTERS}
        entityType="Person"
        features={FEATURES}
      />
    </div>
  );
}
