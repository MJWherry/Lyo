"use client";

import type { GetByIdReq, ProjectionQueryReq, QueryBuilderMode, QueryConcreteReq, QueryReq, SortBy, WhereClause } from "lyo-query";
import { defaultGroup, defaultQueryOptions } from "lyo-query";
import Box from "@mui/material/Box";
import Stack from "@mui/material/Stack";
import Tab from "@mui/material/Tab";
import Tabs from "@mui/material/Tabs";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { ChipInput } from "./ChipInput.js";
import { QueryConcreteForm, QueryProjectionForm, QueryRootForm, toRootQuerySelect } from "./QueryForms.js";

export type QueryBuilderValue = {
  mode: QueryBuilderMode;
  concrete: QueryConcreteReq;
  project: ProjectionQueryReq;
  query: QueryReq;
  get: GetByIdReq;
};

export type QueryBuilderProps = {
  value: QueryBuilderValue;
  onChange: (next: QueryBuilderValue) => void;
  fieldPresets?: readonly string[];
  defaultField?: string;
  includePresets?: readonly string[];
  selectPresets?: readonly string[];
  entityTypePresets?: readonly string[];
  classPrefix?: string;
  modes?: readonly QueryBuilderMode[];
  allowDragDrop?: boolean;
  autoSelectNewFilterNode?: boolean;
  onNewFilterSelected?: (label: string) => void;
};

const MODE_LABELS: Record<QueryBuilderMode, string> = {
  concrete: "Concrete",
  project: "Projection",
  query: "Query",
  get: "Get",
};

const ALL_MODES: QueryBuilderMode[] = ["concrete", "project", "query", "get"];

export function createDefaultQueryBuilderValue(options?: {
  defaultField?: string;
  entityType?: string;
  select?: string[];
  include?: string[];
  amount?: number;
}): QueryBuilderValue {
  const defaultField = options?.defaultField ?? "FirstName";
  const amount = options?.amount ?? 10;
  const opts = defaultQueryOptions({ TotalCountMode: "Exact" });
  const where = defaultGroup(defaultField);
  const select = options?.select ?? ["Id", "FirstName", "LastName"];
  const include = options?.include ?? [];
  const entityType = options?.entityType ?? "Person";
  const shared = {
    Start: 0,
    Amount: amount,
    Include: include,
    SortBy: [] as SortBy[],
    whereClause: where as WhereClause,
  };
  const fromAlias = "p";
  return {
    mode: "concrete",
    concrete: { Options: { ...opts }, ...shared, Include: [...include] },
    project: { Options: { ...opts }, ...shared, Include: [], Select: [...select] },
    query: {
      Options: { ...opts },
      ...shared,
      Include: [],
      From: { Alias: fromAlias, EntityType: entityType },
      Joins: [],
      Select: toRootQuerySelect(select, fromAlias),
    },
    get: { id: "", Include: [...include] },
  };
}

type ListReq = QueryConcreteReq | ProjectionQueryReq | QueryReq;

/** Copies the fields every list mode shares from the edited request into the others, so switching tabs keeps paging, sort, and filter. */
function syncShared(value: QueryBuilderValue, mode: Exclude<QueryBuilderMode, "get">, edited: ListReq): QueryBuilderValue {
  const apply = <T extends ListReq>(req: T): T => ({
    ...req,
    Start: edited.Start,
    Amount: edited.Amount,
    whereClause: edited.whereClause,
    SortBy: edited.SortBy,
    Options: { ...req.Options, TotalCountMode: edited.Options.TotalCountMode, IncludeFilterMode: edited.Options.IncludeFilterMode },
  });
  const next: QueryBuilderValue = { ...value, concrete: apply(value.concrete), project: apply(value.project), query: apply(value.query) };
  if (mode === "concrete") {
    const concrete = edited as QueryConcreteReq;
    return { ...next, concrete, get: { ...value.get, Include: concrete.Include } };
  }
  if (mode === "project") return { ...next, project: edited as ProjectionQueryReq };
  return { ...next, query: edited as QueryReq };
}

export function QueryBuilder({
  value,
  onChange,
  fieldPresets = [],
  defaultField = "FirstName",
  includePresets = [],
  selectPresets = [],
  entityTypePresets = [],
  modes = ALL_MODES,
  allowDragDrop,
  autoSelectNewFilterNode,
  onNewFilterSelected,
}: QueryBuilderProps) {
  const mode = value.mode;
  const presets = {
    fieldPresets,
    defaultField,
    includePresets,
    selectPresets,
    entityTypePresets,
    allowDragDrop,
    autoSelectNewFilterNode,
    onNewFilterSelected,
  };

  return (
    <Box>
      <Tabs value={mode} onChange={(_, v) => onChange({ ...value, mode: v })} variant="scrollable">
        {modes.map((m) => (
          <Tab key={m} value={m} label={MODE_LABELS[m]} />
        ))}
      </Tabs>
      <Box sx={{ mt: 2 }}>
        {mode === "get" ? (
          <Stack spacing={2}>
            <Typography variant="subtitle2">Get by Id</Typography>
            <TextField
              size="small"
              label="Id"
              value={value.get.id}
              onChange={(e) => onChange({ ...value, get: { ...value.get, id: e.target.value.trim() } })}
            />
            <Typography variant="subtitle2">Include</Typography>
            <ChipInput
              values={value.get.Include ?? []}
              onChange={(Include) => onChange({ ...value, get: { ...value.get, Include } })}
              placeholder={includePresets[0] ?? "include path"}
            />
          </Stack>
        ) : mode === "concrete" ? (
          <QueryConcreteForm value={value.concrete} presets={presets} onChange={(req) => onChange(syncShared(value, "concrete", req))} />
        ) : mode === "project" ? (
          <QueryProjectionForm value={value.project} presets={presets} onChange={(req) => onChange(syncShared(value, "project", req))} />
        ) : (
          <QueryRootForm value={value.query} presets={presets} onChange={(req) => onChange(syncShared(value, "query", req))} />
        )}
      </Box>
    </Box>
  );
}

export function activeRequestPreview(value: QueryBuilderValue): unknown {
  switch (value.mode) {
    case "concrete":
      return value.concrete;
    case "project":
      return value.project;
    case "query":
      return value.query;
    case "get":
      return {
        method: "GET",
        path: `/person/${value.get.id || "{id}"}`,
        include: value.get.Include ?? [],
      };
  }
}
