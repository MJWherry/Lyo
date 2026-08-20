"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import type { ConditionClause, FilterPropertyDefinition, SortBy } from "lyo-query";
import type { LyoProblemDetails } from "lyo-api-client";
import type { LyoQueryClient } from "../client/LyoQueryClient.js";
import { clientStore } from "../provider/clientStore.js";
import { buildConcreteQuery, buildProjectedQuery, buildRootQuery } from "./buildQuery.js";
import { nextSorts } from "./sorts.js";
import {
  hasFeature,
  keyEquals,
  LyoDataGridFeatureFlags,
  type FilterState,
  type LyoColumn,
  type LyoDataGridMode,
  type LyoDataGridPersistedState,
} from "./types.js";

export type UseLyoDataGridOptions<T> = {
  apiClient: LyoQueryClient;
  gridKey: string;
  route: string;
  columns: readonly LyoColumn<T>[];
  mode?: LyoDataGridMode;
  features?: number;
  pageSizes?: number[];
  keySelector?: (row: T) => unknown[];
  keyFields?: readonly string[];
  filterPropertyDefinitions?: readonly FilterPropertyDefinition[];
  quickSearchProperties?: readonly string[];
  beforeQuery?: (req: Record<string, unknown>) => Record<string, unknown>;
  /** Map QueryProject dictionaries (often CLR PascalCase) onto the row type used by cells. */
  mapRows?: (items: unknown[]) => T[];
  autoRefreshIntervalsSeconds?: number[];
  searchDebounceMs?: number;
  entityType?: string;
  fromAlias?: string;
};

export function useLyoDataGrid<T>(options: UseLyoDataGridOptions<T>) {
  const {
    apiClient,
    gridKey,
    route,
    columns,
    mode = "project",
    features = LyoDataGridFeatureFlags.All,
    pageSizes = [25, 50, 100],
    keySelector,
    keyFields,
    filterPropertyDefinitions = [],
    quickSearchProperties,
    beforeQuery,
    mapRows,
    searchDebounceMs = 300,
    entityType = "Person",
    fromAlias,
  } = options;

  const storageKey = `grid:${gridKey}`;
  const restored = useRef(false);

  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(pageSizes[0] ?? 25);
  const [searchInput, setSearchInput] = useState("");
  const [searchText, setSearchText] = useState("");
  const [filterStates, setFilterStates] = useState<FilterState[]>([]);
  const [sorts, setSorts] = useState<SortBy[]>([]);
  const [hidden, setHidden] = useState<Set<string>>(
    () => new Set(columns.filter((c) => c.hiddenByDefault).map((c) => c.field))
  );
  const [columnSizing, setColumnSizing] = useState<Record<string, number>>({});
  const [rows, setRows] = useState<T[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [currentQuery, setCurrentQuery] = useState<unknown>(null);
  const [currentResults, setCurrentResults] = useState<unknown>(null);
  const [selectedKeys, setSelectedKeys] = useState<unknown[][]>([]);
  const [autoRefresh, setAutoRefresh] = useState(false);
  const [refreshSeconds, setRefreshSeconds] = useState(10);
  const [tick, setTick] = useState(0);

  useEffect(() => {
    if (restored.current) return;
    restored.current = true;
    const saved = clientStore.get<LyoDataGridPersistedState>(storageKey);
    if (!saved) return;
    if (saved.page != null) setPage(saved.page);
    if (saved.pageSize != null) setPageSize(saved.pageSize);
    if (saved.searchText != null) {
      setSearchInput(saved.searchText);
      setSearchText(saved.searchText);
    }
    if (saved.filterStates) setFilterStates(saved.filterStates);
    if (saved.sorts) {
      setSorts(
        saved.sorts.map((s) => ({
          PropertyName: s.sortBy,
          Direction: s.descending ? "Desc" : "Asc",
          Priority: s.index,
        }))
      );
    }
    if (saved.hiddenColumnFields) setHidden(new Set(saved.hiddenColumnFields));
    if (saved.selectedItemKeys) setSelectedKeys(saved.selectedItemKeys);
    if (saved.columnSizing) setColumnSizing(saved.columnSizing);
  }, [storageKey]);

  useEffect(() => {
    if (!restored.current) return;
    clientStore.set(storageKey, {
      searchText: searchInput,
      filterStates,
      sorts: sorts.map((s, i) => ({
        sortBy: s.PropertyName,
        descending: s.Direction === "Desc",
        index: s.Priority ?? i,
      })),
      page,
      pageSize,
      hiddenColumnFields: [...hidden],
      selectedItemKeys: selectedKeys,
      columnSizing,
    } satisfies LyoDataGridPersistedState);
  }, [storageKey, searchInput, filterStates, sorts, page, pageSize, hidden, selectedKeys, columnSizing]);

  const quickSearch = useMemo(
    () =>
      quickSearchProperties?.length
        ? quickSearchProperties
        : columns.filter((c) => c.quickSearch).map((c) => c.field),
    [columns, quickSearchProperties]
  );

  const activeFilters = useMemo(
    () => filterStates.filter((f) => f.isEnabled).map((f) => f.condition),
    [filterStates]
  );

  useEffect(() => {
    if (searchInput === searchText) return;
    const id = window.setTimeout(() => {
      setSearchText(searchInput);
      setPage(0);
    }, searchDebounceMs);
    return () => window.clearTimeout(id);
  }, [searchInput, searchText, searchDebounceMs]);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const queryArgs = {
        start: page * pageSize,
        amount: pageSize,
        filters: activeFilters,
        searchText,
        quickSearchProperties: quickSearch,
        sorts,
      };
      let body: unknown =
        mode === "concrete"
          ? buildConcreteQuery(queryArgs)
          : mode === "query"
            ? buildRootQuery({
                ...queryArgs,
                columns,
                hiddenFields: hidden,
                keyFields: keyFields ?? (keySelector ? ["Id"] : []),
                entityType,
                fromAlias,
              })
            : buildProjectedQuery({
                ...queryArgs,
                columns,
                hiddenFields: hidden,
                keyFields: keyFields ?? (keySelector ? ["Id"] : []),
              });
      if (beforeQuery) body = beforeQuery(body as Record<string, unknown>);
      setCurrentQuery(body);
      const res =
        mode === "concrete"
          ? await apiClient.queryConcrete?.(route, body)
          : mode === "query"
            ? await apiClient.query?.(route, body)
            : await apiClient.queryProject(route, body);
      const payload = res?.data as QueryPayload<T> | undefined;
      if (!res?.ok || !payload || payload.isSuccess === false || payload.error) {
        setRows([]);
        setTotal(0);
        setError(formatLoadError(undefined, payload));
        return;
      }
      setCurrentResults(payload);
      const items = payload.items ?? [];
      setRows(mapRows ? mapRows(items) : (items as T[]));
      setTotal(payload.total ?? items.length);
    } catch (err) {
      setError(formatLoadError(err));
      setRows([]);
      setTotal(0);
    } finally {
      setLoading(false);
    }
  }, [
    apiClient,
    route,
    mode,
    page,
    pageSize,
    activeFilters,
    searchText,
    quickSearch,
    sorts,
    columns,
    hidden,
    keyFields,
    keySelector,
    beforeQuery,
    mapRows,
    entityType,
    fromAlias,
    tick,
  ]);

  useEffect(() => {
    void reload();
  }, [reload]);

  useEffect(() => {
    if (!autoRefresh || !hasFeature(features, LyoDataGridFeatureFlags.AutoRefresh)) return;
    const id = window.setInterval(() => setTick((t) => t + 1), refreshSeconds * 1000);
    return () => window.clearInterval(id);
  }, [autoRefresh, refreshSeconds, features]);

  const toggleHidden = (field: string) => {
    const col = columns.find((c) => c.field === field);
    if (col?.hideable === false) return;
    setHidden((prev) => {
      const next = new Set(prev);
      if (next.has(field)) next.delete(field);
      else next.add(field);
      return next;
    });
  };

  const addFilter = (condition: ConditionClause) => {
    setFilterStates((s) => [...s, { condition, isEnabled: true }]);
    setPage(0);
  };

  const removeFilter = (index: number) => {
    setFilterStates((s) => s.filter((_, i) => i !== index));
  };

  const toggleFilter = (index: number) => {
    setFilterStates((s) => s.map((f, i) => (i === index ? { ...f, isEnabled: !f.isEnabled } : f)));
  };

  const toggleSort = (field: string) => {
    setSorts((cur) => nextSorts(cur, field));
  };

  const toggleRow = (row: T) => {
    if (!keySelector) return;
    const key = keySelector(row);
    setSelectedKeys((cur) => {
      const has = cur.some((k) => keyEquals(k, key));
      return has ? cur.filter((k) => !keyEquals(k, key)) : [...cur, key];
    });
  };

  const isSelected = (row: T) => {
    if (!keySelector) return false;
    const key = keySelector(row);
    return selectedKeys.some((k) => keyEquals(k, key));
  };

  const pageSelectedCount = keySelector ? rows.filter((r) => isSelected(r)).length : 0;
  const allPageSelected = rows.length > 0 && pageSelectedCount === rows.length;
  const somePageSelected = pageSelectedCount > 0 && !allPageSelected;

  const togglePage = () => {
    if (!keySelector) return;
    if (allPageSelected) {
      setSelectedKeys((cur) => cur.filter((k) => !rows.some((r) => keyEquals(keySelector(r), k))));
      return;
    }
    setSelectedKeys((cur) => {
      const next = [...cur];
      for (const row of rows) {
        const key = keySelector(row);
        if (!next.some((k) => keyEquals(k, key))) next.push(key);
      }
      return next;
    });
  };

  const visibleColumns = columns.filter((c) => !hidden.has(c.field));

  const exportQuery = useMemo(() => {
    const selected = selectedKeys.length;
    const q = buildProjectedQuery({
      start: 0,
      amount: selected > 0 ? selected : 5000,
      filters: activeFilters,
      searchText,
      quickSearchProperties: quickSearch,
      sorts,
      columns,
      hiddenFields: hidden,
      keyFields: keyFields ?? (keySelector ? ["Id"] : []),
    });
    if (selected > 0) q.Keys = selectedKeys;
    return q;
  }, [
    selectedKeys,
    activeFilters,
    searchText,
    quickSearch,
    sorts,
    columns,
    hidden,
    keyFields,
    keySelector,
  ]);

  return {
    features,
    page,
    setPage,
    pageSize,
    setPageSize,
    pageSizes,
    searchText: searchInput,
    setSearchText: setSearchInput,
    filterStates,
    addFilter,
    removeFilter,
    toggleFilter,
    setFilterStates,
    sorts,
    toggleSort,
    hidden,
    toggleHidden,
    columnSizing,
    setColumnSizing,
    rows,
    total,
    loading,
    error,
    currentQuery,
    currentResults,
    exportQuery,
    selectedKeys,
    setSelectedKeys,
    toggleRow,
    isSelected,
    togglePage,
    allPageSelected,
    somePageSelected,
    clearSelection: () => setSelectedKeys([]),
    visibleColumns,
    columns,
    filterPropertyDefinitions,
    reload,
    autoRefresh,
    setAutoRefresh,
    refreshSeconds,
    setRefreshSeconds,
    mode,
    route,
    apiClient,
    keySelector,
  };
}

type QueryPayload<T> = {
  isSuccess?: boolean;
  error?: LyoProblemDetails | null;
  items?: T[] | null;
  total?: number | null;
  detail?: string;
  title?: string;
  errors?: LyoProblemDetails["errors"];
};

function isConnectivityText(text: string): boolean {
  return /failed to fetch|networkerror|econnrefused|connection refused|could not connect|failed to connect|timeout|npgsql|postgres|database/i.test(
    text
  );
}

function problemDetail(problem: LyoProblemDetails | null | undefined): string | null {
  if (!problem) return null;
  const fromErrors = (problem.errors ?? []).map((e) => e.description).find(Boolean);
  return problem.detail || problem.title || fromErrors || null;
}

function asProblem(data: QueryPayload<unknown> | null | undefined): LyoProblemDetails | null {
  if (!data) return null;
  if (data.error) return data.error;
  if (data.detail || data.title || data.errors)
    return { title: data.title, detail: data.detail, errors: data.errors };
  return null;
}

function formatLoadError(err?: unknown, data?: QueryPayload<unknown> | null): string {
  if (err instanceof TypeError)
    return "Unable to connect.";
  const detail = problemDetail(asProblem(data));
  const raw = detail ?? (err instanceof Error ? err.message : null) ?? "The query failed.";
  if (isConnectivityText(raw))
    return raw.toLowerCase().startsWith("unable to connect") ? raw : `Unable to connect. ${raw}`;
  return raw;
}

export type LyoDataGridController<T> = ReturnType<typeof useLyoDataGrid<T>>;
