"use client";

import type {
  GetByIdReq,
  JoinClause,
  ProjectionQueryReq,
  QueryBuilderMode,
  QueryConcreteReq,
  QueryIncludeFilterMode,
  QueryReq,
  QueryTotalCountMode,
  SortBy,
  SortDirection,
  WhereClause,
} from "lyo-query";
import { defaultGroup, defaultQueryOptions } from "lyo-query";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import Tab from "@mui/material/Tab";
import Tabs from "@mui/material/Tabs";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { ChipInput } from "./ChipInput.js";
import { WhereClauseBuilder } from "./WhereClauseBuilder.js";

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
};

function toRootQuerySelect(select: readonly string[], fromAlias: string): string[] {
  return select
    .filter((f) => {
      const trimmed = f.trim();
      if (!trimmed) return false;
      if (trimmed.includes(".") && !trimmed.startsWith(`${fromAlias}.`)) {
        const parts = trimmed.split(".");
        if (parts.length !== 2) return false;
      }
      if (!trimmed.includes(".")) return true;
      const [, prop] = trimmed.split(".", 2);
      return Boolean(prop) && !prop.includes(".");
    })
    .map((f) => (f.includes(".") ? f : `${fromAlias}.${f}`));
}

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

function patchModeShared(
  value: QueryBuilderValue,
  patch: Partial<Pick<QueryConcreteReq, "Start" | "Amount" | "whereClause" | "SortBy" | "Include">> & {
    Options?: Partial<QueryConcreteReq["Options"]>;
    Select?: string[];
  }
): QueryBuilderValue {
  const applyShared = <T extends QueryConcreteReq | ProjectionQueryReq | QueryReq>(req: T): T => {
    const Options = patch.Options ? { ...req.Options, ...patch.Options } : req.Options;
    return {
      ...req,
      ...(patch.Start !== undefined ? { Start: patch.Start } : {}),
      ...(patch.Amount !== undefined ? { Amount: patch.Amount } : {}),
      ...(patch.whereClause !== undefined ? { whereClause: patch.whereClause } : {}),
      ...(patch.SortBy !== undefined ? { SortBy: patch.SortBy } : {}),
      ...(patch.Include !== undefined ? { Include: patch.Include } : {}),
      Options,
    };
  };
  const next = { ...value };
  next.concrete = applyShared(value.concrete);
  next.project = applyShared(value.project);
  next.query = applyShared(value.query);
  if (patch.Select !== undefined) {
    next.project = { ...next.project, Select: patch.Select };
    next.query = { ...next.query, Select: patch.Select };
  }
  return next;
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
}: QueryBuilderProps) {
  const mode = value.mode;
  const activeListReq =
    mode === "concrete" ? value.concrete : mode === "project" ? value.project : value.query;

  return (
    <Box>
      <Tabs value={mode} onChange={(_, v) => onChange({ ...value, mode: v })} variant="scrollable">
        {modes.map((m) => (
          <Tab key={m} value={m} label={MODE_LABELS[m]} />
        ))}
      </Tabs>

      {mode === "get" ? (
        <Stack spacing={2} sx={{ mt: 2 }}>
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
      ) : (
        <Stack spacing={2} sx={{ mt: 2 }}>
          <Stack direction="row" spacing={2}>
            <TextField
              size="small"
              type="number"
              label="Start"
              value={activeListReq.Start ?? 0}
              onChange={(e) =>
                onChange(patchModeShared(value, { Start: Math.max(0, Number(e.target.value) || 0) }))
              }
            />
            <TextField
              size="small"
              type="number"
              label="Amount"
              value={activeListReq.Amount ?? 10}
              onChange={(e) =>
                onChange(
                  patchModeShared(value, {
                    Amount: Math.min(50, Math.max(1, Number(e.target.value) || 1)),
                  })
                )
              }
            />
            <TextField
              size="small"
              select
              label="Total count"
              value={activeListReq.Options.TotalCountMode}
              onChange={(e) =>
                onChange(
                  patchModeShared(value, {
                    Options: { TotalCountMode: e.target.value as QueryTotalCountMode },
                  })
                )
              }
            >
              <MenuItem value="Exact">Exact</MenuItem>
              <MenuItem value="None">None</MenuItem>
              <MenuItem value="HasMore">HasMore</MenuItem>
            </TextField>
            {mode === "concrete" || mode === "project" ? (
              <TextField
                size="small"
                select
                label="Include filter"
                value={activeListReq.Options.IncludeFilterMode}
                onChange={(e) =>
                  onChange(
                    patchModeShared(value, {
                      Options: { IncludeFilterMode: e.target.value as QueryIncludeFilterMode },
                    })
                  )
                }
              >
                <MenuItem value="Full">Full</MenuItem>
                <MenuItem value="MatchedOnly">MatchedOnly</MenuItem>
              </TextField>
            ) : null}
          </Stack>

          {mode === "concrete" ? (
            <>
              <Typography variant="subtitle2">Include</Typography>
              <ChipInput
                values={value.concrete.Include ?? []}
                onChange={(Include) =>
                  onChange({
                    ...value,
                    concrete: { ...value.concrete, Include },
                    get: { ...value.get, Include },
                  })
                }
                placeholder={includePresets[0] ?? "navigation"}
              />
            </>
          ) : null}

          {mode === "project" || mode === "query" ? (
            <>
              <Typography variant="subtitle2">Select</Typography>
              <ChipInput
                values={mode === "project" ? value.project.Select : value.query.Select}
                onChange={(Select) => {
                  if (mode === "project") {
                    onChange({ ...value, project: { ...value.project, Select } });
                  } else {
                    const alias = value.query.From.Alias || "p";
                    onChange({
                      ...value,
                      query: { ...value.query, Select: toRootQuerySelect(Select, alias) },
                    });
                  }
                }}
                placeholder={selectPresets[0] ?? "Field"}
              />
            </>
          ) : null}

          {mode === "query" ? (
            <RootFromJoinsForm
              value={value.query}
              entityTypePresets={entityTypePresets}
              onChange={(query) => onChange({ ...value, query })}
            />
          ) : null}

          <Typography variant="subtitle2">Sort By</Typography>
          <SortByEditor
            items={activeListReq.SortBy ?? []}
            fieldPresets={fieldPresets}
            onChange={(SortBy) => onChange(patchModeShared(value, { SortBy }))}
          />

          <Typography variant="subtitle2">Filter</Typography>
          <WhereClauseBuilder
            value={activeListReq.whereClause ?? defaultGroup(defaultField)}
            onChange={(whereClause) => onChange(patchModeShared(value, { whereClause }))}
            fieldPresets={fieldPresets}
            defaultField={defaultField}
          />
        </Stack>
      )}
    </Box>
  );
}

function SortByEditor({
  items,
  onChange,
  fieldPresets,
}: {
  items: SortBy[];
  onChange: (next: SortBy[]) => void;
  fieldPresets: readonly string[];
}) {
  return (
    <Stack spacing={1}>
      {items.map((item, index) => (
        <Stack key={index} direction="row" spacing={1}>
          <TextField
            size="small"
            label="Property"
            value={item.PropertyName}
            onChange={(e) => {
              const next = items.slice();
              next[index] = { ...item, PropertyName: e.target.value };
              onChange(next);
            }}
            select={fieldPresets.length > 0}
            sx={{ flex: 1 }}
          >
            {fieldPresets.map((f) => (
              <MenuItem key={f} value={f}>
                {f}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            size="small"
            select
            label="Dir"
            value={item.Direction}
            onChange={(e) => {
              const next = items.slice();
              next[index] = { ...item, Direction: e.target.value as SortDirection };
              onChange(next);
            }}
            sx={{ width: 100 }}
          >
            <MenuItem value="Asc">Asc</MenuItem>
            <MenuItem value="Desc">Desc</MenuItem>
          </TextField>
          <Button onClick={() => onChange(items.filter((_, i) => i !== index))}>Remove</Button>
        </Stack>
      ))}
      <Button
        onClick={() =>
          onChange([
            ...items,
            { PropertyName: fieldPresets[0] ?? "Id", Direction: "Asc", Priority: items.length },
          ])
        }
      >
        + Sort
      </Button>
    </Stack>
  );
}

function nextJoinAlias(existing: readonly JoinClause[]): string {
  const used = new Set(existing.map((j) => j.Alias.toLowerCase()));
  if (!used.has("ca")) return "ca";
  let i = 2;
  while (used.has(`j${i}`)) i += 1;
  return `j${i}`;
}

function RootFromJoinsForm({
  value,
  onChange,
  entityTypePresets,
}: {
  value: QueryReq;
  onChange: (next: QueryReq) => void;
  entityTypePresets: readonly string[];
}) {
  const joins = value.Joins ?? [];
  return (
    <Stack spacing={1}>
      <Typography variant="subtitle2">From</Typography>
      <Stack direction="row" spacing={1}>
        <TextField
          size="small"
          label="Alias"
          value={value.From.Alias}
          onChange={(e) => onChange({ ...value, From: { ...value.From, Alias: e.target.value } })}
        />
        <TextField
          size="small"
          label="EntityType"
          value={value.From.EntityType}
          onChange={(e) => onChange({ ...value, From: { ...value.From, EntityType: e.target.value } })}
          select={entityTypePresets.length > 0}
          sx={{ flex: 1 }}
        >
          {entityTypePresets.map((t) => (
            <MenuItem key={t} value={t}>
              {t}
            </MenuItem>
          ))}
        </TextField>
      </Stack>
      <Stack direction="row" justifyContent="space-between" alignItems="center">
        <Typography variant="subtitle2">Joins</Typography>
        <Button
          size="small"
          onClick={() => {
            const alias = nextJoinAlias(joins);
            const fromAlias = value.From.Alias || "p";
            const join: JoinClause = {
              Alias: alias,
              EntityType: "ContactAddressEntity",
              Type: "Left",
              On: [{ From: `${fromAlias}.Id`, To: `${alias}.PersonId` }],
            };
            const select = [...(value.Select ?? [])];
            for (const path of [`${alias}.Id`, `${alias}.PersonId`]) {
              if (!select.includes(path)) select.push(path);
            }
            onChange({ ...value, Joins: [...joins, join], Select: select });
          }}
        >
          + Join
        </Button>
      </Stack>
      {joins.map((join, index) => (
        <Stack key={index} spacing={1} sx={{ p: 1, border: 1, borderColor: "divider", borderRadius: 1 }}>
          <Stack direction="row" spacing={1}>
            <TextField
              size="small"
              label="Alias"
              value={join.Alias}
              onChange={(e) => {
                const Joins = joins.slice();
                Joins[index] = { ...join, Alias: e.target.value };
                onChange({ ...value, Joins });
              }}
            />
            <TextField
              size="small"
              label="EntityType"
              value={join.EntityType}
              onChange={(e) => {
                const Joins = joins.slice();
                Joins[index] = { ...join, EntityType: e.target.value };
                onChange({ ...value, Joins });
              }}
              sx={{ flex: 1 }}
            />
            <Button onClick={() => onChange({ ...value, Joins: joins.filter((_, i) => i !== index) })}>
              Remove
            </Button>
          </Stack>
        </Stack>
      ))}
    </Stack>
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
