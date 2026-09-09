"use client";

import { useMemo, useState } from "react";
import { diffLines } from "diff";
import Box from "@mui/material/Box";
import Stack from "@mui/material/Stack";
import ToggleButton from "@mui/material/ToggleButton";
import ToggleButtonGroup from "@mui/material/ToggleButtonGroup";
import Typography from "@mui/material/Typography";

export function LyoTextDiffViewer({
  original,
  modified,
}: {
  original: string;
  modified: string;
}) {
  const [mode, setMode] = useState<"unified" | "split">("split");
  const parts = useMemo(() => diffLines(original, modified), [original, modified]);

  if (mode === "unified") {
    return (
      <Box>
        <ToggleButtonGroup size="small" exclusive value={mode} onChange={(_, v) => v && setMode(v)} sx={{ mb: 1 }}>
          <ToggleButton value="split">Split</ToggleButton>
          <ToggleButton value="unified">Unified</ToggleButton>
        </ToggleButtonGroup>
        <Box className="lyo-diff">
          {parts.map((p, i) => (
            <span key={i} className={p.added ? "lyo-diff__add" : p.removed ? "lyo-diff__del" : undefined}>
              {p.value}
            </span>
          ))}
        </Box>
      </Box>
    );
  }

  return (
    <Box>
      <ToggleButtonGroup size="small" exclusive value={mode} onChange={(_, v) => v && setMode(v)} sx={{ mb: 1 }}>
        <ToggleButton value="split">Split</ToggleButton>
        <ToggleButton value="unified">Unified</ToggleButton>
      </ToggleButtonGroup>
      <Stack direction="row" spacing={2}>
        <Box sx={{ flex: 1 }}>
          <Typography variant="caption">Original</Typography>
          <Box className="lyo-diff">
            {parts
              .filter((p) => !p.added)
              .map((p, i) => (
                <span key={i} className={p.removed ? "lyo-diff__del" : undefined}>
                  {p.value}
                </span>
              ))}
          </Box>
        </Box>
        <Box sx={{ flex: 1 }}>
          <Typography variant="caption">Modified</Typography>
          <Box className="lyo-diff">
            {parts
              .filter((p) => !p.removed)
              .map((p, i) => (
                <span key={i} className={p.added ? "lyo-diff__add" : undefined}>
                  {p.value}
                </span>
              ))}
          </Box>
        </Box>
      </Stack>
    </Box>
  );
}
