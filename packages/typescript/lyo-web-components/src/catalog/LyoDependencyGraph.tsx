"use client";

import AccountTree from "@mui/icons-material/AccountTree";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Paper from "@mui/material/Paper";
import Typography from "@mui/material/Typography";
import { LyoChip } from "../display/LyoChip.js";
import { LyoEmptyState, LyoSkeletonPanel } from "../state/LyoState.js";
import { dependencyLabel, groupDependencies, type CatalogFetch } from "./catalog.js";
import { useCatalogPackage } from "./useCatalogPackage.js";

export type LyoDependencyGraphProps = {
  packageId: string;
  /** Root URL of the catalog assets (the folder holding `index.json` and `packages/`). */
  catalogBasePath: string;
  /** Custom fetch. Keep it stable across renders. */
  fetchFn?: CatalogFetch;
};

/** Direct dependencies of one catalog package, grouped by kind. Lyo packages are highlighted. Reads the same JSON as `PackageDoc`. */
export function LyoDependencyGraph({ packageId, catalogBasePath, fetchFn }: LyoDependencyGraphProps) {
  const { doc, loading, error } = useCatalogPackage(catalogBasePath, packageId, fetchFn);
  const icon = <AccountTree sx={{ fontSize: 48, opacity: 0.6 }} />;

  if (loading) return <LyoSkeletonPanel rows={4} />;
  if (error) return <Alert severity="warning">{`Could not load dependencies for ${packageId}.`}</Alert>;
  if (!doc) return <LyoEmptyState title="No package selected" icon={icon} />;
  if (doc.dependencies.length === 0) return <LyoEmptyState title="No dependencies listed" description={`${doc.name} has an empty dependency list in the catalog.`} icon={icon} />;

  return (
    <Box sx={{ display: "flex", flexDirection: "column", alignItems: "flex-start", gap: 0.5 }}>
      <Paper variant="outlined" sx={{ p: 1.5, minWidth: "12rem" }}>
        <Typography variant="subtitle1">{doc.name}</Typography>
        <Typography variant="caption" color="text.secondary">
          {doc.area}
        </Typography>
      </Paper>
      <Box aria-hidden sx={{ width: 2, height: "1rem", ml: 3, bgcolor: "divider" }} />
      <Box sx={{ display: "flex", flexDirection: "column", gap: 1.5, width: "100%" }}>
        {groupDependencies(doc.dependencies).map((group) => (
          <Box key={group.kind}>
            <Typography variant="overline">{group.kind}</Typography>
            <Box sx={{ display: "flex", flexWrap: "wrap", gap: 0.5 }}>
              {group.dependencies.map((d, i) => (
                <LyoChip key={`${d.name}-${i}`} label={dependencyLabel(d)} title={d.name} variant="outlined" color={d.kind === "lyo" ? "primary" : "default"} />
              ))}
            </Box>
          </Box>
        ))}
      </Box>
    </Box>
  );
}
