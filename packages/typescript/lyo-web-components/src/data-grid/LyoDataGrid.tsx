"use client";

import { Children, useMemo, useRef, useState, type ReactNode } from "react";
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
import Autorenew from "@mui/icons-material/Autorenew";
import DeveloperMode from "@mui/icons-material/DeveloperMode";
import FilterList from "@mui/icons-material/FilterList";
import MoreVert from "@mui/icons-material/MoreVert";
import Refresh from "@mui/icons-material/Refresh";
import SortIcon from "@mui/icons-material/Sort";
import ViewColumn from "@mui/icons-material/ViewColumn";
import ViewList from "@mui/icons-material/ViewList";
import ViewModule from "@mui/icons-material/ViewModule";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
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
import { FilterChipLabel } from "../query/FilterChipLabel.js";
import { filterChipText } from "../query/filterChipText.js";
import { QueryFilterComponent, type QueryFilterHandle } from "../query/QueryFilterComponent.js";
import { LyoBulkPatchDialog } from "../overlay/LyoBulkPatchDialog.js";
import { LyoToolbar, LyoToolbarButton, LyoToolbarField, LyoToolbarIconButton, LyoToolbarMenu, LyoToolbarRow } from "../shell/LyoToolbar.js";
import { LyoDataGridCards } from "./LyoDataGridCards.js";
import { renderLyoCell } from "./LyoDataGridCell.js";
import { LyoDataGridExportDialog, LyoDataGridExportMenu } from "./LyoDataGridExportMenu.js";
import { responseDevChips } from "./devChips.js";
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
  /** Icon buttons drawn over the row on hover. The kebab menu stays visible. */
  rowQuickActions?: (row: T) => ReactNode;
  /** Pixel width of the trailing row-actions column. Default 56. */
  rowMenuWidth?: number;
  bulkMenuItems?: ReactNode;
  noRecordsContent?: ReactNode;
  /** Shown instead of the empty table while the first page loads. */
  loadingContent?: ReactNode;
  /** Fired when the row body is clicked. Row menus and selection checkboxes do not trigger it. */
  onRowClick?: (row: T) => void;
  /** Extra selected styling, independent of the bulk checkbox. */
  isRowSelected?: (row: T) => boolean;
};

function GridInner<T>({
  grid,
  elementId,
  leftControls,
  rowMenu,
  rowQuickActions,
  rowMenuWidth = 56,
  bulkMenuItems,
  noRecordsContent,
  loadingContent,
  onRowClick,
  isRowSelected,
}: {
  grid: LyoDataGridController<T>;
  elementId?: string;
  leftControls?: ReactNode;
  rowMenu?: (row: T) => ReactNode;
  rowQuickActions?: (row: T) => ReactNode;
  rowMenuWidth?: number;
  bulkMenuItems?: ReactNode;
  noRecordsContent?: ReactNode;
  loadingContent?: ReactNode;
  onRowClick?: (row: T) => void;
  isRowSelected?: (row: T) => boolean;
}) {
  const id =
    grid.mode === "project"
      ? resolveElementId(elementId, dataGridProjectedElementId(grid.route))
      : resolveElementId(elementId, dataGridElementId(grid.route));
  const json = useJsonViewDialog();
  const snackbar = useLyoSnackbar();
  const [filterAnchor, setFilterAnchor] = useState<null | HTMLElement>(null);
  const filterRef = useRef<QueryFilterHandle>(null);
  const [exportFormat, setExportFormat] = useState<ExportFormat | null>(null);
  const [patchOpen, setPatchOpen] = useState(false);

  const selectable = hasFeature(grid.features, LyoDataGridFeatureFlags.BulkMenu) && Boolean(grid.keySelector);
  const selectedCount = grid.selectedKeys.length;
  const sortableCardColumns = grid.cardColumns.map((c) => c.column).filter((c) => c.sortable !== false);

  const deleteSelected = async () => {
    const count = selectedCount;
    try {
      await grid.apiClient.bulkDelete?.(grid.route, grid.selectedKeys);
      snackbar.show(`Deleted ${count.toLocaleString("en-US")} item${count === 1 ? "" : "s"}.`, "success");
      grid.clearSelection();
    } catch (err) {
      snackbar.show(err instanceof Error ? err.message : "Delete failed.", "error");
    }
    await grid.reload();
  };

  const rowActions = (row: T) => (
    <>
      {rowMenu ? <RowKebab>{rowMenu(row)}</RowKebab> : null}
      <RowDevMenu onViewJson={() => json.show(row, "Row", { path: grid.route })} />
    </>
  );

  return (
    <Box id={id} data-lyo-id={id} sx={{ width: "100%", minWidth: 0, overflow: "hidden" }}>
      <Box sx={{ px: 2, py: 1 }}>
        <LyoToolbar
          rows={
            <>
              <LyoToolbarRow>
                {selectable ? (
                  <LyoToolbarMenu label={`Bulk ${grid.bulkLabel}`.trim()}>
                    {(close) => [
                      <MenuItem
                        key="deselect"
                        disabled={selectedCount === 0}
                        onClick={() => {
                          grid.clearSelection();
                          close();
                        }}
                      >
                        Deselect ({selectedCount.toLocaleString("en-US")})
                      </MenuItem>,
                      hasFeature(grid.features, LyoDataGridFeatureFlags.BulkExport) && grid.apiClient.export ? (
                        <LyoDataGridExportMenu
                          key="export"
                          disabled={!grid.canExport}
                          onPickFormat={(format) => {
                            close();
                            setExportFormat(format);
                          }}
                        />
                      ) : null,
                      hasFeature(grid.features, LyoDataGridFeatureFlags.BulkPatch) && grid.apiClient.bulkPatch ? (
                        <MenuItem
                          key="patch"
                          disabled={!grid.canBulkEdit}
                          onClick={() => {
                            close();
                            setPatchOpen(true);
                          }}
                        >
                          Patch
                        </MenuItem>
                      ) : null,
                      hasFeature(grid.features, LyoDataGridFeatureFlags.BulkDelete) && grid.apiClient.bulkDelete ? (
                        <MenuItem
                          key="delete"
                          disabled={!grid.canBulkEdit}
                          onClick={() => {
                            close();
                            void deleteSelected();
                          }}
                        >
                          Delete
                        </MenuItem>
                      ) : null,
                      ...Children.toArray(bulkMenuItems),
                    ]}
                  </LyoToolbarMenu>
                ) : null}
                {leftControls}
                <Box sx={{ flex: 1 }} />
                <LyoToolbarMenu label="Dev" icon={<DeveloperMode />}>
                  {(close) => [
                    <MenuItem
                      key="req"
                      onClick={() => {
                        json.show(grid.currentQuery, "Request", { path: grid.queryPath });
                        close();
                      }}
                    >
                      View Request
                    </MenuItem>,
                    <MenuItem
                      key="res"
                      onClick={() => {
                        json.show(grid.currentResults, "Response", { path: grid.queryPath, chips: responseDevChips(grid.queryMetrics) });
                        close();
                      }}
                    >
                      View Response
                    </MenuItem>,
                  ]}
                </LyoToolbarMenu>
                <LyoToolbarButton icon={<Refresh />} onClick={() => void grid.reload()} disabled={grid.loading}>
                  Refresh
                </LyoToolbarButton>
                {grid.autoRefreshEnabled ? (
                  <LyoToolbarMenu label={grid.autoRefresh ? `Auto (${grid.refreshSeconds}s)` : "Auto Refresh"} icon={<Autorenew color={grid.autoRefresh ? "success" : "inherit"} />}>
                    {(close) => [
                      <MenuItem
                        key="toggle"
                        onClick={() => {
                          grid.setAutoRefresh(!grid.autoRefresh);
                          close();
                        }}
                      >
                        {grid.autoRefresh ? "Stop" : "Start"}
                      </MenuItem>,
                      ...grid.autoRefreshIntervalsSeconds.map((seconds) => (
                        <MenuItem
                          key={seconds}
                          selected={seconds === grid.refreshSeconds}
                          onClick={() => {
                            grid.setRefreshSeconds(seconds);
                            grid.setAutoRefresh(true);
                            close();
                          }}
                        >
                          Every {seconds}s
                        </MenuItem>
                      )),
                    ]}
                  </LyoToolbarMenu>
                ) : null}
                {grid.cardMode && sortableCardColumns.length > 0 ? (
                  <LyoToolbarMenu label="Sort" icon={<SortIcon />}>
                    {sortableCardColumns.map((c) => {
                      const sort = grid.sorts.find((s) => s.PropertyName === c.field);
                      return (
                        <MenuItem key={c.field} selected={Boolean(sort)} onClick={() => grid.toggleSort(c.field)}>
                          {c.header}
                          {sort ? (sort.Direction === "Asc" ? <ArrowUpward sx={{ fontSize: 14, ml: 1 }} /> : <ArrowDownward sx={{ fontSize: 14, ml: 1 }} />) : null}
                        </MenuItem>
                      );
                    })}
                  </LyoToolbarMenu>
                ) : null}
                {grid.allowLayoutToggle ? (
                  <LyoToolbarIconButton
                    tooltip={grid.cardMode ? "Show as table" : "Show as cards"}
                    icon={grid.cardMode ? <ViewList /> : <ViewModule />}
                    onClick={() => grid.setLayout(grid.cardMode ? "Table" : "Cards")}
                  />
                ) : null}
                <LyoToolbarMenu tooltip="Columns" icon={<ViewColumn />}>
                  {grid.columns
                    .filter((c) => c.hideable !== false)
                    .map((c) => (
                      <MenuItem key={c.field} onClick={() => grid.toggleHidden(c.field)}>
                        <Checkbox checked={!grid.hidden.has(c.field)} size="small" />
                        {c.header}
                      </MenuItem>
                    ))}
                </LyoToolbarMenu>
              </LyoToolbarRow>
              <LyoToolbarRow>
                {hasFeature(grid.features, LyoDataGridFeatureFlags.Searchable) ? (
                  <LyoToolbarField width={260}>
                    <TextField
                      size="small"
                      fullWidth
                      placeholder={grid.searchPlaceholder}
                      value={grid.searchText}
                      onChange={(e) => grid.setSearchText(e.target.value)}
                    />
                  </LyoToolbarField>
                ) : null}
                {hasFeature(grid.features, LyoDataGridFeatureFlags.Filterable) ? (
                  <>
                    <LyoToolbarButton variant="text" icon={<FilterList />} onClick={(e) => setFilterAnchor(e.currentTarget)}>
                      Filters
                    </LyoToolbarButton>
                    {grid.filterStates.length === 0 ? (
                      <Typography variant="body2" color="text.secondary">
                        None
                      </Typography>
                    ) : null}
                  </>
                ) : null}
                {grid.filterStates.map((f, i) => (
                  <FilterChipLabel
                    key={i}
                    text={filterChipText(f.condition, grid.filterPropertyDefinitions)}
                    detailText={filterChipText(f.condition, grid.filterPropertyDefinitions, false)}
                    disabled={!f.isEnabled}
                    onClick={() => grid.toggleFilter(i)}
                    onDelete={() => grid.removeFilter(i)}
                  />
                ))}
                {grid.loading ? <CircularProgress size={18} /> : null}
              </LyoToolbarRow>
            </>
          }
          showRowToggle={false}
        />
        {grid.error ? <Alert severity="error">{grid.error}</Alert> : null}
      </Box>

      {grid.cardMode ? (
        <LyoDataGridCards
          grid={grid}
          selectable={selectable}
          actions={(row) => (
            <>
              {rowQuickActions?.(row)}
              {rowActions(row)}
            </>
          )}
          onRowClick={onRowClick}
          isRowSelected={isRowSelected}
          noRecordsContent={noRecordsContent}
          loadingContent={loadingContent}
        />
      ) : (
        <GridTable
          grid={grid}
          selectable={selectable}
          rowActions={rowActions}
          rowQuickActions={rowQuickActions}
          rowMenuWidth={rowMenu || rowQuickActions ? rowMenuWidth + ROW_DEV_MENU_SIZE : ROW_DEV_MENU_SIZE}
          onRowClick={onRowClick}
          isRowSelected={isRowSelected}
          noRecordsContent={noRecordsContent}
          loadingContent={loadingContent}
        />
      )}
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
      {patchOpen ? (
        <LyoBulkPatchDialog
          open
          onClose={() => setPatchOpen(false)}
          keys={grid.selectedKeys}
          properties={grid.filterPropertyDefinitions}
          route={grid.route}
          patchRoute={grid.patchRoute}
          client={grid.apiClient}
          onPatched={() => void grid.reload()}
        />
      ) : null}
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

const SELECT_COL_SIZE = 36;
const ROW_DEV_MENU_SIZE = 36;

function RowKebab({ children }: { children: ReactNode }) {
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  return (
    <>
      <IconButton
        size="small"
        aria-label="Row actions"
        onClick={(e) => {
          e.stopPropagation();
          setAnchor(e.currentTarget);
        }}
      >
        <MoreVert sx={{ fontSize: 18 }} />
      </IconButton>
      <Menu open={Boolean(anchor)} anchorEl={anchor} onClose={() => setAnchor(null)} onClick={() => setAnchor(null)}>
        {Children.toArray(children)}
      </Menu>
    </>
  );
}

function RowDevMenu({ onViewJson }: { onViewJson: () => void }) {
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  return (
    <>
      <IconButton size="small" onClick={(e) => setAnchor(e.currentTarget)} aria-label="Row dev menu">
        <DeveloperMode sx={{ fontSize: 16 }} />
      </IconButton>
      <Menu open={Boolean(anchor)} anchorEl={anchor} onClose={() => setAnchor(null)}>
        <MenuItem
          onClick={() => {
            setAnchor(null);
            onViewJson();
          }}
        >
          View Json
        </MenuItem>
      </Menu>
    </>
  );
}
const checkboxSx = { p: 0, m: 0, "& .MuiSvgIcon-root": { fontSize: 18 } };

const stickyCell = (side: "left" | "right", head: boolean) => ({
  position: "sticky" as const,
  [side]: 0,
  zIndex: head ? 3 : 2,
  bgcolor: "background.paper",
  ...(head
    ? {}
    : {
        ".MuiTableRow-root:hover &": { bgcolor: "action.hover" },
        ".MuiTableRow-root.Mui-selected &": { bgcolor: "action.selected" },
      }),
});

function GridTable<T>({
  grid,
  selectable,
  rowActions,
  rowQuickActions,
  rowMenuWidth,
  onRowClick,
  isRowSelected,
  noRecordsContent,
  loadingContent,
}: {
  grid: LyoDataGridController<T>;
  selectable: boolean;
  rowActions: (row: T) => ReactNode;
  rowQuickActions?: (row: T) => ReactNode;
  rowMenuWidth: number;
  onRowClick?: (row: T) => void;
  isRowSelected?: (row: T) => boolean;
  noRecordsContent?: ReactNode;
  loadingContent?: ReactNode;
}) {
  const columns = useMemo<ColumnDef<T>[]>(() => {
    const defs: ColumnDef<T>[] = [];
    if (selectable) {
      defs.push({
        id: "__select",
        enableSorting: false,
        enableResizing: false,
        size: SELECT_COL_SIZE,
        minSize: SELECT_COL_SIZE,
        maxSize: SELECT_COL_SIZE,
        header: () => (
          <Checkbox
            size="small"
            sx={checkboxSx}
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
            sx={checkboxSx}
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
        maxSize: col.maxSize,
        cell: ({ row }) => renderLyoCell(col, row.original, grid),
      });
    }
    defs.push({
      id: "__menu",
      enableSorting: false,
      enableResizing: false,
      size: rowMenuWidth,
      minSize: rowMenuWidth,
      maxSize: rowMenuWidth,
      header: "",
      cell: ({ row }) => (
        <Box sx={{ position: "relative", display: "flex", alignItems: "center", justifyContent: "flex-end" }}>
          {rowQuickActions ? (
            <Box
              className="lyo-row-quick"
              sx={{
                position: "absolute",
                right: "100%",
                top: "50%",
                transform: "translateY(-50%)",
                display: "flex",
                alignItems: "center",
                opacity: 0,
                pointerEvents: "none",
                ".MuiTableRow-root:hover &": { opacity: 1, pointerEvents: "auto" },
              }}
            >
              {rowQuickActions(row.original)}
            </Box>
          ) : null}
          <Stack direction="row" alignItems="center" spacing={0.25}>
            {rowActions(row.original)}
          </Stack>
        </Box>
      ),
    });
    return defs;
  }, [grid, selectable, rowActions, rowQuickActions, rowMenuWidth]);

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
    <Box sx={{ width: "100%", minWidth: 0, overflow: "auto" }}>
      <Table size="small" sx={{ width: tableWidth, minWidth: "100%", tableLayout: "fixed" }}>
        <colgroup>
          {table.getAllLeafColumns().map((col) => (
            <col key={col.id} style={{ width: col.getSize() }} />
          ))}
        </colgroup>
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
                const isMenu = header.column.id === "__menu";
                const fixed = isSelect || isMenu;
                const align = grid.visibleColumns.find((c) => c.field === header.column.id)?.align;
                return (
                  <TableCell
                    key={header.id}
                    padding={fixed ? "none" : undefined}
                    onClick={() => {
                      if (header.column.getIsResizing() || isSelect) return;
                      if (sortable) grid.toggleSort(header.column.id);
                    }}
                    sx={{
                      position: "relative",
                      cursor: sortable && !isSelect ? "pointer" : "default",
                      fontWeight: 600,
                      width: header.getSize(),
                      minWidth: fixed ? header.getSize() : (header.column.columnDef.minSize ?? header.getSize()),
                      maxWidth: header.column.columnDef.maxSize ?? (fixed ? header.getSize() : undefined),
                      overflow: "hidden",
                      textAlign: isSelect ? "center" : (align ?? "inherit"),
                      px: isSelect ? 0 : isMenu ? 0.5 : undefined,
                      userSelect: "none",
                      ...(isSelect ? stickyCell("left", true) : isMenu ? stickyCell("right", true) : {}),
                    }}
                  >
                    <Stack direction="row" spacing={0.5} alignItems="center" sx={{ pr: isSelect ? 0 : 1, minWidth: 0, justifyContent: isSelect ? "center" : undefined }}>
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
          {rows.length === 0 && grid.loading && loadingContent ? (
            <TableRow>
              <TableCell colSpan={colSpan}>{loadingContent}</TableCell>
            </TableRow>
          ) : rows.length === 0 && !grid.loading ? (
            <TableRow>
              <TableCell colSpan={colSpan}>
                {noRecordsContent ?? (
                  <Typography variant="body2" color="text.secondary">
                    {grid.error ? "Unable to connect to the database" : "No records."}
                  </Typography>
                )}
              </TableCell>
            </TableRow>
          ) : (
            rows.map((row) => (
              <TableRow
                key={row.id}
                hover
                selected={grid.isSelected(row.original) || Boolean(isRowSelected?.(row.original))}
                onClick={onRowClick ? () => onRowClick(row.original) : undefined}
                sx={onRowClick ? { cursor: "pointer" } : undefined}
              >
                {row.getVisibleCells().map((cell) => {
                  const isSelect = cell.column.id === "__select";
                  const isMenu = cell.column.id === "__menu";
                  const fixed = isSelect || isMenu;
                  const align = grid.visibleColumns.find((c) => c.field === cell.column.id)?.align;
                  return (
                    <TableCell
                      key={cell.id}
                      padding={fixed ? "none" : undefined}
                      sx={{
                        width: cell.column.getSize(),
                        minWidth: fixed ? cell.column.getSize() : (cell.column.columnDef.minSize ?? cell.column.getSize()),
                        maxWidth: cell.column.columnDef.maxSize ?? (fixed ? cell.column.getSize() : undefined),
                        overflow: isMenu ? "visible" : "hidden",
                        textOverflow: "ellipsis",
                        whiteSpace: "nowrap",
                        textAlign: isSelect ? "center" : (align ?? "inherit"),
                        px: isSelect ? 0 : isMenu ? 0.5 : undefined,
                        ...(isSelect ? stickyCell("left", false) : isMenu ? stickyCell("right", false) : {}),
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

export function LyoDataGrid<T>(props: LyoDataGridProps<T>) {
  const { elementId, leftControls, rowMenu, rowQuickActions, rowMenuWidth, bulkMenuItems, noRecordsContent, loadingContent, onRowClick, isRowSelected, ...opts } = props;
  const grid = useLyoDataGrid({ ...opts, mode: opts.mode ?? "concrete" });
  return (
    <GridInner
      grid={grid}
      elementId={elementId}
      leftControls={leftControls}
      rowMenu={rowMenu}
      rowQuickActions={rowQuickActions}
      rowMenuWidth={rowMenuWidth}
      bulkMenuItems={bulkMenuItems}
      noRecordsContent={noRecordsContent}
      loadingContent={loadingContent}
      onRowClick={onRowClick}
      isRowSelected={isRowSelected}
    />
  );
}

export function LyoDataGridProjected<T>(props: LyoDataGridProps<T>) {
  const { elementId, leftControls, rowMenu, rowQuickActions, rowMenuWidth, bulkMenuItems, noRecordsContent, loadingContent, onRowClick, isRowSelected, ...opts } = props;
  const grid = useLyoDataGrid({ ...opts, mode: "project" });
  return (
    <GridInner
      grid={grid}
      elementId={elementId}
      leftControls={leftControls}
      rowMenu={rowMenu}
      rowQuickActions={rowQuickActions}
      rowMenuWidth={rowMenuWidth}
      bulkMenuItems={bulkMenuItems}
      noRecordsContent={noRecordsContent}
      loadingContent={loadingContent}
      onRowClick={onRowClick}
      isRowSelected={isRowSelected}
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
      id: "recentAddr",
      field: "ContactAddresses.Address.FullAddress",
      header: "Most recent address",
      filterable: false,
      sortable: false,
      cell: (row) => mostRecentAddressText(row),
      size: 220,
    }),
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

function mostRecentAddressText(row: unknown): string {
  const raw = projectedValue(row, "ContactAddresses.Address") ?? projectedValue(row, "ContactAddresses");
  const list = Array.isArray(raw) ? raw : raw == null ? [] : [raw];
  const newest = list
    .map((item) => {
      const rec = item != null && typeof item === "object" ? (item as Record<string, unknown>) : null;
      const addr = (rec?.Address ?? rec?.address ?? rec) as Record<string, unknown> | undefined;
      const text = addr?.FullAddress ?? addr?.fullAddress;
      if (typeof text !== "string" || text.length === 0) return null;
      const stamp = addr?.UpdatedTimestamp ?? addr?.updatedTimestamp ?? addr?.CreatedTimestamp ?? addr?.createdTimestamp;
      const ts = typeof stamp === "string" || stamp instanceof Date ? Date.parse(String(stamp)) : 0;
      return { text, ts: Number.isNaN(ts) ? 0 : ts };
    })
    .filter((item): item is { text: string; ts: number } => item != null)
    .sort((a, b) => b.ts - a.ts)[0];
  return newest?.text ?? "—";
}
