"use client";

import Box from "@mui/material/Box";
import Paper from "@mui/material/Paper";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";
import type { CatalogSection } from "./catalog.js";

export type CatalogSectionListProps = { sections: readonly CatalogSection[] | null | undefined };

function SectionBody({ section }: { section: CatalogSection }) {
  switch (section.type) {
    case "paragraph":
      return (
        <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: "pre-wrap" }}>
          {section.text}
        </Typography>
      );
    case "markdown":
      return (
        <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: "pre-wrap" }}>
          {section.body}
        </Typography>
      );
    case "list":
      return (
        <Box component={section.ordered ? "ol" : "ul"} sx={{ my: 0, pl: 3 }}>
          {(section.items ?? []).map((item, i) => (
            <Typography key={i} component="li" variant="body2" sx={{ whiteSpace: "pre-wrap" }}>
              {item}
            </Typography>
          ))}
        </Box>
      );
    case "code":
      return (
        <Paper variant="outlined" sx={{ p: 1.5, mb: 1.5, bgcolor: "action.hover" }}>
          <Box component="pre" sx={{ m: 0, overflow: "auto", fontSize: "0.85rem" }}>
            <code>{section.code}</code>
          </Box>
        </Paper>
      );
    case "table":
      return (
        <Box sx={{ mb: 1 }}>
          {section.lead?.trim() ? (
            <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: "pre-wrap", mb: 1 }}>
              {section.lead}
            </Typography>
          ) : null}
          <Table size="small" sx={{ mb: 1 }}>
            <TableHead>
              <TableRow>
                {section.headers.map((header, i) => (
                  <TableCell key={i}>{header}</TableCell>
                ))}
              </TableRow>
            </TableHead>
            <TableBody>
              {section.rows.map((row, r) => (
                <TableRow key={r}>
                  {row.map((cell, c) => (
                    <TableCell key={c} sx={{ whiteSpace: "pre-wrap" }}>
                      {cell}
                    </TableCell>
                  ))}
                </TableRow>
              ))}
            </TableBody>
          </Table>
          {section.trail?.trim() ? (
            <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: "pre-wrap" }}>
              {section.trail}
            </Typography>
          ) : null}
        </Box>
      );
    default:
      return null;
  }
}

/** Renders catalog sections (paragraph, list, code, table, markdown) in order. Markdown bodies are shown as plain text. */
export function CatalogSectionList({ sections }: CatalogSectionListProps) {
  if (!sections?.length) return null;
  return (
    <>
      {sections.map((section, i) => (
        <Box key={i}>
          {section.title?.trim() ? (
            <Typography variant="h6" sx={{ mt: 2, mb: 1 }}>
              {section.title}
            </Typography>
          ) : null}
          <SectionBody section={section} />
        </Box>
      ))}
    </>
  );
}
