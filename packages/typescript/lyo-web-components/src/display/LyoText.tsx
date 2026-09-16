"use client";

import Box from "@mui/material/Box";
import Tooltip from "@mui/material/Tooltip";
import { splitHighlight, truncateText } from "./highlight.js";

/** Text clipped past `maxLength` with an ellipsis; the full text is in the tooltip. */
export function LyoTruncatedText({ text, maxLength = 80 }: { text: string | null | undefined; maxLength?: number }) {
  const result = truncateText(text, maxLength);
  if (!result.truncated) return <span>{result.text}</span>;
  return (
    <Tooltip title={text}>
      <span>{result.text}</span>
    </Tooltip>
  );
}

/** Marks case-insensitive occurrences of `query` inside `text`. */
export function LyoHighlighter({ text, query }: { text: string | null | undefined; query: string | null | undefined }) {
  return (
    <span>
      {splitHighlight(text, query).map((segment, i) =>
        segment.match ? (
          <Box key={i} component="mark" sx={{ bgcolor: "warning.light", color: "inherit", borderRadius: 0.5, px: 0.25 }}>
            {segment.text}
          </Box>
        ) : (
          <span key={i}>{segment.text}</span>
        )
      )}
    </span>
  );
}
