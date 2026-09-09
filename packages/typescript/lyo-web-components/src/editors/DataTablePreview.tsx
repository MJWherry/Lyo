"use client";

import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";

export type DataTablePreviewModel = {
  columns?: string[];
  rows: Array<Record<string, unknown> | unknown[]>;
};

export function DataTablePreview({ table }: { table: DataTablePreviewModel | null | undefined }) {
  if (!table) {
    return (
      <Typography variant="body2" color="text.secondary">
        No table loaded.
      </Typography>
    );
  }
  const cols =
    table.columns ??
    (table.rows[0] && !Array.isArray(table.rows[0])
      ? Object.keys(table.rows[0] as Record<string, unknown>)
      : table.rows[0]
        ? (table.rows[0] as unknown[]).map((_, i) => String(i))
        : []);
  if (cols.length === 0 && table.rows.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary">
        Empty table.
      </Typography>
    );
  }
  return (
    <>
      <Table size="small">
        <TableHead>
          <TableRow>
            {cols.map((c) => (
              <TableCell key={c}>{c}</TableCell>
            ))}
          </TableRow>
        </TableHead>
        <TableBody>
          {table.rows.map((row, i) => (
            <TableRow key={i}>
              {cols.map((c, ci) => (
                <TableCell key={c}>
                  {String(Array.isArray(row) ? row[ci] : (row as Record<string, unknown>)[c] ?? "")}
                </TableCell>
              ))}
            </TableRow>
          ))}
        </TableBody>
      </Table>
      <Typography variant="caption" color="text.secondary">
        {table.rows.length.toLocaleString()} row(s)
      </Typography>
    </>
  );
}
