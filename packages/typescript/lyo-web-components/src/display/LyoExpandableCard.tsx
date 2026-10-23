"use client";

import type { ReactNode } from "react";
import Accordion from "@mui/material/Accordion";
import AccordionDetails from "@mui/material/AccordionDetails";
import AccordionSummary from "@mui/material/AccordionSummary";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import { resolveElementId } from "../provider/elementId.js";

export type LyoExpandableCardProps = {
  summary: ReactNode;
  children?: ReactNode;
  defaultExpanded?: boolean;
  expanded?: boolean;
  onExpandedChange?: (expanded: boolean) => void;
  elementId?: string;
};

export function LyoExpandableCard({ summary, children, defaultExpanded, expanded, onExpandedChange, elementId }: LyoExpandableCardProps) {
  return (
    <Accordion
      id={resolveElementId(elementId, "lyo-expandable-card")}
      variant="outlined"
      disableGutters
      defaultExpanded={defaultExpanded}
      expanded={expanded}
      onChange={(_, next) => onExpandedChange?.(next)}
    >
      <AccordionSummary expandIcon={<ExpandMoreIcon />}>{summary}</AccordionSummary>
      <AccordionDetails>{children}</AccordionDetails>
    </Accordion>
  );
}
