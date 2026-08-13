"use client";

import { useMemo, useRef, useState, type ReactNode } from "react";
import type { FilterPropertyDefinition } from "lyo-query";
import {
  flexRender,
  getCoreRowModel,
  useReactTable,
  type ColumnDef,
  type ColumnSizingState,
  type SortingState,
} from "@tanstack/react-table";
import ArrowDownward from "@mui/icons-material/ArrowDownward";
import ArrowUpward from "@mui/icons-material/ArrowUpward";
import DeveloperMode from "@mui/icons-material/DeveloperMode";
import FilterList from "@mui/icons-material/FilterList";
import Refresh from "@mui/icons-material/Refresh";
import ViewColumn from "@mui/icons-material/ViewColumn";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Checkbox from "@mui/material/Checkbox";
import CircularProgress from "@mui/material/CircularProgress";
import IconButton from "@mui/material/IconButton";
import Menu from "@mui/material/Menu";
import MenuItem from "@mui/material/MenuItem";
import Popover from "@mui/material/Popover";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableHead from "@mui/material/TableHead";
import TablePagination from "@mui/material/TablePagination";
import TableRow from "@mui/material/TableRow";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import type { ExportFormat } from "lyo-api-client";
import { useLyoSnackbar } from "../provider/LyoSnackbar.js";
import { dataGridElementId, dataGridProjectedElementId, resolveElementId } from "../provider/elementId.js";
import { useJsonViewDialog } from "../overlay/LyoJsonViewDialog.js";
import { FilterChipLabel, formatFilterChip } from "../query/FilterChipLabel.js";
import { QueryFilterComponent, type QueryFilterHandle } from "../query/QueryFilterComponent.js";
import { LyoDataGridExportDialog, LyoDataGridExportMenu } from "./LyoDataGridExportMenu.js";
import {
  hasFeature,
  LyoDataGridFeatureFlags,
  projectedValue,
  createLyoColumn,
  type LyoColumn,
} from "./types.js";
import { useLyoDataGrid, type LyoDataGridController, type UseLyoDataGridOptions } from "./useLyoDataGrid.js";

export type LyoDataGridProps<T> = UseLyoDataGridOptions<T> & {
  elementId?: string;
  leftControls?: ReactNode;
  rowMenu?: (row: T) => ReactNode;
  bulkMenuItems?: ReactNode;
  noRecordsContent?: ReactNode;
};

function GridInner<T>({
  grid,
  elementId,
  leftControls,
  rowMenu,
  bulkMenuItems,
  noRecordsContent,
}: {
  grid: LyoDataGridController<T>;
  elementId?: string;
  leftControls?: ReactNode;
  rowMenu?: (row: T) => ReactNode;
  bulkMenuItems?: ReactNode;
  noRecordsContent?: ReactNode;
}) {
  const id =
    grid.mode === "project"
      ? resolveElementId(elementId, dataGridProjectedElementId(grid.route))
      : resolveElementId(elementId, dataGridElementId(grid.route));
  const json = useJsonViewDialog();
  const snackbar = useLyoSnackbar();
  const [colAnchor, setColAnchor] = useState<null | HTMLElement>(null);
  const [devAnchor, setDevAnchor] = useState<null | HTMLElement>(null);
  const [bulkAnchor, setBulkAnchor] = useState<null | HTMLElement>(null);
  const [filterAnchor, setFilterAnchor] = useState<null | HTMLElement>(null);
  const filterRef = useRef<QueryFilterHandle>(null);
  const [exportFormat, setExportFormat] = useState<ExportFormat | null>(null);

  const selectable = hasFeature(grid.features, LyoDataGridFeatureFlags.BulkMenu) && Boolean(grid.keySelector);

  return (
    <Box id={id} data-lyo-id={id} sx={{ width: "100%", minWidth: 0 }}>
      <Stack spacing={1} sx={{ px: 2, py: 1 }}>
        <Stack direction="row" spacing={1} alignItems="center">
          {selectable ? (
            <>
              <Button size="small" variant="outlined" onClick={(e) => setBulkAnchor(e.currentTarget)}>
                Bulk ({grid.selectedKeys.length})
              </Button>
              <Menu open={Boolean(bulkAnchor)} anchorEl={bulkAnchor} onClose={() => setBulkAnchor(null)}>
                <MenuItem
                  onClick={() => {
                    grid.clearSelection();
                    setBulkAnchor(null);
                  }}
                >
                  Deselect
                </MenuItem>
                {hasFeature(grid.features, LyoDataGridFeatureFlags.BulkExport) && grid.apiClient.export ? (
                  <LyoDataGridExportMenu
                    onPickFormat={(format) => {
                      setBulkAnchor(null);
                      setExportFormat(format);
                    }}
                  />
                ) : null}
                {bulkMenuItems}
                {hasFeature(grid.features, LyoDataGridFeatureFlags.BulkDelete) && grid.apiClient.bulkDelete ? (
                  <MenuItem
                    onClick={async () => {
                      setBulkAnchor(null);
                      await grid.apiClient.bulkDelete?.(grid.route, grid.selectedKeys);
                      await grid.reload();
                    }}
                  >
                    Delete
                  </MenuItem>
                ) : null}
              </Menu>
            </>
          ) : null}
          {leftControls}
          <Box sx={{ flex: 1 }} />
          <Button size="small" startIcon={<DeveloperMode />} variant="outlined" onClick={(e) => setDevAnchor(e.currentTarget)}>
            Dev
          </Button>
          <Menu open={Boolean(devAnchor)} anchorEl={devAnchor} onClose={() => setDevAnchor(null)}>
            <MenuItem
              onClick={() => {
                json.show(grid.currentQuery, "Request");
                setDevAnchor(null);
              }}
            >
              View Request
            </MenuItem>
            <MenuItem
              onClick={() => {
                json.show(grid.currentResults, "Result");
                setDevAnchor(null);
              }}
            >
              Show Result
            </MenuItem>
          </Menu>
          <Button size="small" startIcon={<Refresh />} variant="outlined" onClick={() => void grid.reload()} disabled={grid.loading}>
            Refresh
          </Button>
          {hasFeature(grid.features, LyoDataGridFeatureFlags.AutoRefresh) ? (
            <Button
              size="small"
              variant="outlined"
              color={grid.autoRefresh ? "success" : "inherit"}
              onClick={() => grid.setAutoRefresh(!grid.autoRefresh)}
            >
              {grid.autoRefresh ? `Auto (${grid.refreshSeconds}s)` : "Auto Refresh"}
            </Button>
          ) : null}
          <IconButton size="small" onClick={(e) => setColAnchor(e.currentTarget)} aria-label="Columns">
            <ViewColumn />
          </IconButton>
          <Menu open={Boolean(colAnchor)} anchorEl={colAnchor} onClose={() => setColAnchor(null)}>
            {grid.columns
              .filter((c) => c.hideable !== false)
              .map((c) => (
                <MenuItem key={c.field} onClick={() => grid.toggleHidden(c.field)}>
                  <Checkbox checked={!grid.hidden.has(c.field)} size="small" />
                  {c.header}
                </MenuItem>
              ))}
          </Menu>
        </Stack>
        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
          {hasFeature(grid.features, LyoDataGridFeatureFlags.Searchable) ? (
            <TextField
              size="small"
              label="Search"
              value={grid.searchText}
              onChange={(e) => grid.setSearchText(e.target.value)}
              sx={{ minWidth: 200 }}
            />
          ) : null}
          {hasFeature(grid.features, LyoDataGridFeatureFlags.Filterable) ? (
            <Button
              size="small"
              startIcon={<FilterList />}
              onClick={(e) => setFilterAnchor(e.currentTarget)}
            >
              Filters
            </Button>
          ) : null}
          {grid.filterStates.map((f, i) => (
            <FilterChipLabel
              key={i}
              text={formatFilterChip(f.condition.Field, f.condition.Comparison, f.condition.Value)}
              disabled={!f.isEnabled}
              onClick={() => grid.toggleFilter(i)}
              onDelete={() => grid.removeFilter(i)}
            />
          ))}
          {grid.loading ? <CircularProgress size={18} /> : null}
        </Stack>
        {grid.error ? <Alert severity="error">{grid.error}</Alert> : null}
      </Stack>

      <GridTable
        grid={grid}
        selectable={selectable}
        rowMenu={rowMenu}
        noRecordsContent={noRecordsContent}
      />
      <TablePagination
        component="div"
        count={grid.total}
        page={grid.page}
        onPageChange={(_, p) => grid.setPage(p)}
        rowsPerPage={grid.pageSize}
        onRowsPerPageChange={(e) => {
          grid.setPageSize(Number(e.target.value));
          grid.setPage(0);
        }}
        rowsPerPageOptions={grid.pageSizes}
      />
      {json.dialog}
      <LyoDataGridExportDialog
        grid={grid}
        format={exportFormat}
        onClose={() => setExportFormat(null)}
        onError={(msg) => snackbar.show(msg, "error")}
      />
      <Popover
        open={Boolean(filterAnchor)}
        anchorEl={filterAnchor}
        onClose={() => {
          filterRef.current?.commit();
          setFilterAnchor(null);
        }}
        anchorOrigin={{ vertical: "bottom", horizontal: "left" }}
        transformOrigin={{ vertical: "top", horizontal: "left" }}
        disableScrollLock
        slotProps={{
          paper: {
            sx: { p: 1.5, overflow: "visible" },
          },
        }}
      >
        <QueryFilterComponent
          ref={filterRef}
          definitions={grid.filterPropertyDefinitions as FilterPropertyDefinition[]}
          onAdd={(condition) => {
            grid.addFilter(condition);
          }}
        />
      </Popover>
    </Box>
  );
}

function GridTable<T>({
  grid,
  selectable,
  rowMenu,
  noRecordsContent,
}: {
  grid: LyoDataGridController<T>;
  selectable: boolean;
  rowMenu?: (row: T) => ReactNode;
  noRecordsContent?: ReactNode;
}) {
  const columns = useMemo<ColumnDef<T>[]>(() => {
    const defs: ColumnDef<T>[] = [];
    if (selectable) {
      defs.push({
        id: "__select",
        enableSorting: false,
        enableResizing: false,
        size: 52,
        minSize: 52,
        maxSize: 52,
        header: () => (
          <Checkbox
            size="small"
            checked={grid.allPageSelected}
            indeterminate={grid.somePageSelected}
            onChange={() => grid.togglePage()}
            onClick={(e) => e.stopPropagation()}
            inputProps={{ "aria-label": "Select all on this page" }}
          />
        ),
        cell: ({ row }) => (
          <Checkbox
            size="small"
            checked={grid.isSelected(row.original)}
            onChange={() => grid.toggleRow(row.original)}
            inputProps={{ "aria-label": "Select row" }}
          />
        ),
      });
    }
    for (const col of grid.visibleColumns) {
      defs.push({
        id: col.field,
        accessorFn: (row) => (col.accessor ? col.accessor(row) : projectedValue(row, col.field)),
        header: col.header,
        enableSorting: col.sortable !== false,
        size: col.size ?? 140,
        minSize: col.minSize ?? 64,
        maxSize: col.maxSize ?? 640,
        cell: ({ row, getValue }) => (col.cell ? col.cell(row.original) : formatCell(getValue())),
      });
    }
    if (rowMenu) {
      defs.push({
        id: "__menu",
        enableSorting: false,
        enableResizing: false,
        size: 56,
        header: "",
        cell: ({ row }) => rowMenu(row.original),
      });
    }
    return defs;
  }, [grid, selectable, rowMenu]);

  const sorting = useMemo<SortingState>(
    () =>
      grid.sorts.map((s) => ({
        id: s.PropertyName,
        desc: s.Direction === "Desc",
      })),
    [grid.sorts]
  );

  const table = useReactTable({
    data: grid.rows,
    columns,
    getCoreRowModel: getCoreRowModel(),
    getRowId: (row, index) => {
      if (!grid.keySelector) return String(index);
      return JSON.stringify(grid.keySelector(row));
    },
    manualPagination: true,
    manualSorting: true,
    enableColumnResizing: true,
    columnResizeMode: "onChange",
    pageCount: Math.max(1, Math.ceil(grid.total / grid.pageSize) || 1),
    state: {
      pagination: { pageIndex: grid.page, pageSize: grid.pageSize },
      sorting,
      columnSizing: grid.columnSizing,
    },
    onColumnSizingChange: (updater) => {
      const next =
        typeof updater === "function"
          ? updater(grid.columnSizing as ColumnSizingState)
          : updater;
      grid.setColumnSizing(next);
    },
  });

  const headerGroups = table.getHeaderGroups();
  const rows = table.getRowModel().rows;
  const colSpan = Math.max(1, columns.length);
  const tableWidth = table.getTotalSize();

  return (
    <Box sx={{ width: "100%", overflowX: "auto" }}>
      <Table size="small" sx={{ width: tableWidth, minWidth: "100%", tableLayout: "fixed" }}>
        <TableHead>
          {headerGroups.map((hg) => (
            <TableRow key={hg.id}>
              {hg.headers.map((header) => {
                const sortable = header.column.getCanSort();
                const sortIndex = sortable
                  ? grid.sorts.findIndex((s) => s.PropertyName === header.column.id)
                  : -1;
                const sort = sortIndex >= 0 ? grid.sorts[sortIndex] : undefined;
                const isSelect = header.column.id === "__select";
                return (
                  <TableCell
                    key={header.id}
                    padding={isSelect ? "none" : undefined}
                    onClick={() => {
                      if (header.column.getIsResizing() || isSelect) return;
                      if (sortable) grid.toggleSort(header.column.id);
                    }}
                    sx={{
                      position: "relative",
                      cursor: sortable && !isSelect ? "pointer" : "default",
                      fontWeight: 600,
                      width: header.getSize(),
                      minWidth: header.column.columnDef.minSize ?? header.getSize(),
                      maxWidth: header.getSize(),
                      overflow: isSelect ? "visible" : "hidden",
                      textAlign: isSelect ? "center" : "inherit",
                      px: isSelect ? 0.5 : undefined,
                      userSelect: "none",
                    }}
                  >
                    <Stack direction="row" spacing={0.5} alignItems="center" sx={{ pr: 1, minWidth: 0 }}>
                      <Box component="span" sx={{ overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                        {flexRender(header.column.columnDef.header, header.getContext())}
                      </Box>
                      {sort ? (
                        <Stack direction="row" spacing={0.25} alignItems="center" sx={{ flexShrink: 0 }}>
                          {sort.Direction === "Asc" ? <ArrowUpward sx={{ fontSize: 14 }} /> : <ArrowDownward sx={{ fontSize: 14 }} />}
                          <Typography variant="caption" color="text.secondary" sx={{ lineHeight: 1 }}>
                            {sortIndex + 1}
                          </Typography>
                        </Stack>
                      ) : null}
                    </Stack>
                    {header.column.getCanResize() ? (
                      <Box
                        onMouseDown={header.getResizeHandler()}
                        onTouchStart={header.getResizeHandler()}
                        onClick={(e) => e.stopPropagation()}
                        className="lyo-col-resizer"
                        sx={{
                          position: "absolute",
                          right: 0,
                          top: 0,
                          height: "100%",
                          width: 8,
                          cursor: "col-resize",
                          userSelect: "none",
                          touchAction: "none",
                          "&:hover, &.lyo-col-resizer--active": {
                            bgcolor: "primary.main",
                            opacity: 0.45,
                          },
                          ...(header.column.getIsResizing() ? { bgcolor: "primary.main", opacity: 0.6 } : {}),
                        }}
                      />
                    ) : null}
                  </TableCell>
                );
              })}
            </TableRow>
          ))}
        </TableHead>
        <TableBody>
          {rows.length === 0 && !grid.loading ? (
            <TableRow>
              <TableCell colSpan={colSpan}>
                {noRecordsContent ?? (
                  <Typography variant="body2" color="text.secondary">
                    No records.
                  </Typography>
                )}
              </TableCell>
            </TableRow>
          ) : (
            rows.map((row) => (
              <TableRow key={row.id} hover selected={grid.isSelected(row.original)}>
                {row.getVisibleCells().map((cell) => {
                  const isSelect = cell.column.id === "__select";
                  return (
                    <TableCell
                      key={cell.id}
                      padding={isSelect ? "none" : undefined}
                      sx={{
                        width: cell.column.getSize(),
                        minWidth: cell.column.columnDef.minSize ?? cell.column.getSize(),
                        maxWidth: cell.column.getSize(),
                        overflow: isSelect ? "visible" : "hidden",
                        textOverflow: isSelect ? "clip" : "ellipsis",
                        whiteSpace: "nowrap",
                        textAlign: isSelect ? "center" : "inherit",
                        px: isSelect ? 0.5 : undefined,
                      }}
                    >
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </TableCell>
                  );
                })}
              </TableRow>
            ))
          )}
        </TableBody>
      </Table>
    </Box>
  );
}

function formatCell(value: unknown): string {
  if (value == null) return "—";
  if (typeof value === "object") return JSON.stringify(value);
  return String(value);
}

export function LyoDataGrid<T>(props: LyoDataGridProps<T>) {
  const { elementId, leftControls, rowMenu, bulkMenuItems, noRecordsContent, ...opts } = props;
  const grid = useLyoDataGrid({ ...opts, mode: opts.mode ?? "concrete" });
  return (
    <GridInner
      grid={grid}
      elementId={elementId}
      leftControls={leftControls}
      rowMenu={rowMenu}
      bulkMenuItems={bulkMenuItems}
      noRecordsContent={noRecordsContent}
    />
  );
}

export function LyoDataGridProjected<T>(props: LyoDataGridProps<T>) {
  const { elementId, leftControls, rowMenu, bulkMenuItems, noRecordsContent, ...opts } = props;
  const grid = useLyoDataGrid({ ...opts, mode: "project" });
  return (
    <GridInner
      grid={grid}
      elementId={elementId}
      leftControls={leftControls}
      rowMenu={rowMenu}
      bulkMenuItems={bulkMenuItems}
      noRecordsContent={noRecordsContent}
    />
  );
}

export function defaultPersonGridColumns(): LyoColumn<Record<string, unknown>>[] {
  return [
    createLyoColumn({ id: "id", field: "Id", header: "ID", size: 140, minSize: 80 }),
    createLyoColumn({ id: "first", field: "FirstName", header: "Firstname", quickSearch: true, size: 120 }),
    createLyoColumn({ id: "middle", field: "MiddleName", header: "Middlename", size: 110 }),
    createLyoColumn({ id: "last", field: "LastName", header: "Lastname", quickSearch: true, size: 120 }),
    createLyoColumn({ id: "dob", field: "DateOfBirth", header: "DoB", type: "DateTime", size: 110 }),
    createLyoColumn({
      id: "addr",
      field: "ContactAddresses.Count",
      header: "Addresses",
      filterable: false,
      size: 96,
    }),
    createLyoColumn({
      id: "email",
      field: "ContactEmailAddresses.Count",
      header: "Email Addresses",
      filterable: false,
      size: 120,
    }),
    createLyoColumn({
      id: "phone",
      field: "ContactPhoneNumbers.Count",
      header: "Phone Numbers",
      filterable: false,
      size: 120,
    }),
    createLyoColumn({ id: "source", field: "SourceEntityType", header: "Source", size: 160, minSize: 80 }),
  ];
}

export function usePersonGridColumns() {
  return useMemo(() => defaultPersonGridColumns(), []);
}
