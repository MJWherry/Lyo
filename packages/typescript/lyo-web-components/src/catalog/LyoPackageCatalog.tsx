"use client";

import { useEffect, useMemo, useState } from "react";
import AccountTree from "@mui/icons-material/AccountTree";
import Description from "@mui/icons-material/Description";
import Inventory2 from "@mui/icons-material/Inventory2";
import MenuBook from "@mui/icons-material/MenuBook";
import Search from "@mui/icons-material/Search";
import SearchOff from "@mui/icons-material/SearchOff";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Chip from "@mui/material/Chip";
import InputAdornment from "@mui/material/InputAdornment";
import List from "@mui/material/List";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemText from "@mui/material/ListItemText";
import Paper from "@mui/material/Paper";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { LyoPageHeader } from "../shell/LyoPage.js";
import { LyoTabSet } from "../shell/LyoTabSet.js";
import { LyoToolbar } from "../shell/LyoToolbar.js";
import { LyoEmptyState, LyoSkeletonPanel } from "../state/LyoState.js";
import { catalogAreas, fetchCatalogIndex, filterCatalogPackages, type CatalogFetch, type CatalogIndex } from "./catalog.js";
import { LyoDependencyGraph } from "./LyoDependencyGraph.js";
import { PackageDoc } from "./PackageDoc.js";

export type LyoPackageCatalogProps = {
  /** Root URL of the catalog assets (the folder holding `index.json` and `packages/`). */
  catalogBasePath: string;
  /** Custom fetch, for example one that adds auth headers. Keep it stable across renders. */
  fetchFn?: CatalogFetch;
};

/** Browse the generated package catalog: search, area chips, then Docs and Dependencies tabs for the selected package. */
export function LyoPackageCatalog({ catalogBasePath, fetchFn }: LyoPackageCatalogProps) {
  const [index, setIndex] = useState<CatalogIndex | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [area, setArea] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  useEffect(() => {
    let live = true;
    setLoading(true);
    setError(null);
    fetchCatalogIndex(catalogBasePath, fetchFn)
      .then((next) => live && setIndex(next))
      .catch(() => live && setError("Could not load the package catalog."))
      .finally(() => live && setLoading(false));
    return () => {
      live = false;
    };
  }, [catalogBasePath, fetchFn]);

  const areas = useMemo(() => catalogAreas(index?.packages ?? []), [index]);
  const filtered = useMemo(() => filterCatalogPackages(index?.packages ?? [], { search, area }), [index, search, area]);
  const selected = filtered.find((p) => p.id === selectedId) ?? null;

  const description = index
    ? `${index.packageCount} packages generated ${index.generatedAt ? new Date(index.generatedAt).toLocaleString(undefined, { dateStyle: "short", timeStyle: "short" }) : "recently"}.`
    : "Loading catalog…";

  return (
    <>
      <LyoPageHeader
        title="Package catalog"
        description={description}
        icon={<Inventory2 />}
        actions={
          <TextField
            size="small"
            placeholder="Search packages"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            sx={{ minWidth: "16rem" }}
            slotProps={{
              input: {
                startAdornment: (
                  <InputAdornment position="start">
                    <Search fontSize="small" />
                  </InputAdornment>
                ),
              },
            }}
          />
        }
      />
      {loading ? (
        <LyoSkeletonPanel rows={8} showHeader />
      ) : error ? (
        <Alert severity="warning">{error}</Alert>
      ) : (
        <>
          <LyoToolbar
            start={
              <Box sx={{ display: "flex", flexWrap: "wrap", gap: 0.5 }}>
                <Chip size="small" label="All" color="primary" variant={area === null ? "filled" : "outlined"} onClick={() => setArea(null)} />
                {areas.map((a) => (
                  <Chip key={a} size="small" label={a} variant={area?.toLowerCase() === a.toLowerCase() ? "filled" : "outlined"} onClick={() => setArea(a)} />
                ))}
              </Box>
            }
            end={
              <Typography variant="caption" color="text.secondary">
                {`${filtered.length} packages`}
              </Typography>
            }
          />
          <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", md: "minmax(14rem, 22rem) minmax(0, 1fr)" }, gap: 2, alignItems: "start", mt: 1 }}>
            <Paper variant="outlined" sx={{ maxHeight: "70vh", overflow: "auto" }}>
              {filtered.length === 0 ? (
                <LyoEmptyState title="No packages match" description="Try a different search or area." icon={<SearchOff />} />
              ) : (
                <List dense>
                  {filtered.map((p) => (
                    <ListItemButton key={p.id} selected={p.id === selectedId} onClick={() => setSelectedId(p.id)}>
                      <ListItemText primary={p.name} secondary={p.area} />
                    </ListItemButton>
                  ))}
                </List>
              )}
            </Paper>
            <Box sx={{ minWidth: 0 }}>
              {selected ? (
                <LyoTabSet
                  key={selected.id}
                  persistKey={`catalog-${selected.id}`}
                  defaultTab="docs"
                  queryParameter={null}
                  tabs={[
                    { id: "docs", text: "Docs", icon: <Description />, content: <PackageDoc packageId={selected.id} catalogBasePath={catalogBasePath} fetchFn={fetchFn} /> },
                    {
                      id: "graph",
                      text: "Dependencies",
                      icon: <AccountTree />,
                      content: <LyoDependencyGraph packageId={selected.id} catalogBasePath={catalogBasePath} fetchFn={fetchFn} />,
                    },
                  ]}
                />
              ) : (
                <LyoEmptyState title="Select a package" description="Search or filter the list, then pick a package to read its docs." icon={<MenuBook sx={{ fontSize: 48, opacity: 0.6 }} />} />
              )}
            </Box>
          </Box>
        </>
      )}
    </>
  );
}
