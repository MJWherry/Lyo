"use client";

import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import LinearProgress from "@mui/material/LinearProgress";
import Link from "@mui/material/Link";
import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { LyoChip } from "../display/LyoChip.js";
import { LyoCodeBlock } from "../display/LyoCodeBlock.js";
import type { CatalogFetch } from "./catalog.js";
import { CatalogSectionList } from "./CatalogSectionList.js";
import { useCatalogPackage } from "./useCatalogPackage.js";

export type PackageDocProps = {
  /** Package id, for example `Lyo.Cache`. */
  packageId: string;
  /** Root URL of the catalog assets (the folder holding `index.json` and `packages/`). */
  catalogBasePath: string;
  /** Custom fetch, for example one that adds auth headers. Keep it stable across renders. */
  fetchFn?: CatalogFetch;
};

/** One package's catalog page: header, description, features, examples, benchmarks, sections, and links. */
export function PackageDoc({ packageId, catalogBasePath, fetchFn }: PackageDocProps) {
  const { doc, loading, error } = useCatalogPackage(catalogBasePath, packageId, fetchFn);

  if (loading) return <LinearProgress />;
  if (error) return <Alert severity="warning">{error}</Alert>;
  if (!doc)
    return (
      <Alert severity="info">
        No catalog entry for <code>{packageId}</code>.
      </Alert>
    );

  return (
    <Stack spacing={3}>
      <Box>
        <Typography variant="overline">{doc.area}</Typography>
        <Typography variant="h4">{doc.name}</Typography>
        <Typography variant="subtitle1" color="text.secondary">
          {doc.tagline}
        </Typography>
      </Box>

      <Typography variant="body1" sx={{ whiteSpace: "pre-wrap" }}>
        {doc.description}
      </Typography>

      {doc.features.length > 0 ? (
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Typography variant="h6" sx={{ mb: 1 }}>
            Features
          </Typography>
          <Box component="ul" sx={{ my: 0, pl: 3 }}>
            {doc.features.map((f, i) => (
              <Typography key={i} component="li" variant="body2">
                {f}
              </Typography>
            ))}
          </Box>
        </Paper>
      ) : null}

      {doc.examples.length > 0 ? (
        <Stack spacing={2}>
          <Typography variant="h5">Examples</Typography>
          {doc.examples.map((ex, i) => (
            <Box key={i}>
              <Typography variant="h6" sx={{ mb: 1 }}>
                {ex.title}
              </Typography>
              <LyoCodeBlock code={ex.code} language={ex.language} lineNumbers={false} />
            </Box>
          ))}
        </Stack>
      ) : null}

      {doc.benchmarks ? (
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Typography variant="h6" sx={{ mb: 1 }}>
            Benchmarks
          </Typography>
          {doc.benchmarks.headline?.trim() ? (
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
              {doc.benchmarks.headline}
            </Typography>
          ) : null}
          {doc.benchmarks.suite?.trim() ? <LyoChip variant="outlined" label={`suite: ${doc.benchmarks.suite}`} /> : null}
          <Box component="ul" sx={{ my: 0, pl: 3 }}>
            {doc.benchmarks.items.map((item, i) => (
              <Box component="li" key={i}>
                <Typography variant="body2" component="span" sx={{ fontWeight: 600 }}>
                  {item.label}
                </Typography>
                {item.note?.trim() ? <Typography variant="body2" component="span">{` — ${item.note}`}</Typography> : null}
                <Typography variant="caption" color="text.secondary" component="div">
                  {item.href}
                </Typography>
              </Box>
            ))}
          </Box>
        </Paper>
      ) : null}

      <CatalogSectionList sections={doc.sections} />

      {doc.links.length > 0 ? (
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Typography variant="h6" sx={{ mb: 1 }}>
            Links
          </Typography>
          <Box component="ul" sx={{ my: 0, pl: 3 }}>
            {doc.links.map((link, i) => (
              <li key={i}>
                <Link href={link.href} target="_blank" rel="noopener noreferrer">
                  {link.label}
                </Link>
              </li>
            ))}
          </Box>
        </Paper>
      ) : null}
    </Stack>
  );
}
