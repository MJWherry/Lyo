"use client";

import { useEffect, useState, type ReactNode } from "react";
import Box from "@mui/material/Box";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Typography from "@mui/material/Typography";
import { isAtOrBelow, type LyoBreakpoint } from "./viewport.js";

/** Window width in px, or null before mount so the server and first client render agree. */
export function useViewport(): { width: number | null; isAtOrBelow: (bp: LyoBreakpoint) => boolean } {
  const [width, setWidth] = useState<number | null>(null);
  useEffect(() => {
    const update = () => setWidth(window.innerWidth);
    update();
    window.addEventListener("resize", update);
    return () => window.removeEventListener("resize", update);
  }, []);
  return { width, isAtOrBelow: (bp) => isAtOrBelow(width, bp) };
}

export type LyoResponsiveLayout = "Auto" | "Table" | "Cards";

export type LyoResponsiveTableProps = {
  table: ReactNode;
  cards: ReactNode;
  /** Auto switches to cards at or below `breakpoint`. */
  layout?: LyoResponsiveLayout;
  breakpoint?: LyoBreakpoint;
};

/** Renders `table` on wide screens and `cards` on narrow ones, or forces one with `layout`. */
export function LyoResponsiveTable({ table, cards, layout = "Auto", breakpoint = "sm" }: LyoResponsiveTableProps) {
  const viewport = useViewport();
  const showCards = layout === "Cards" || (layout === "Auto" && viewport.isAtOrBelow(breakpoint));
  return <>{showCards ? <Box sx={{ display: "flex", flexDirection: "column", gap: 1 }}>{cards}</Box> : table}</>;
}

export type LyoRecordCardProps = {
  title?: ReactNode;
  subtitle?: ReactNode;
  actions?: ReactNode;
  children?: ReactNode;
  selected?: boolean;
  onClick?: () => void;
};

/** Card for one record in a narrow layout: title row with actions, then `LyoRecordField` rows. */
export function LyoRecordCard({ title, subtitle, actions, children, selected, onClick }: LyoRecordCardProps) {
  return (
    <Card variant="outlined" onClick={onClick} sx={{ borderColor: selected ? "primary.main" : undefined, cursor: onClick ? "pointer" : undefined }}>
      <CardContent sx={{ py: 1.5, "&:last-child": { pb: 1.5 } }}>
        {title || actions ? (
          <Box sx={{ display: "flex", alignItems: "flex-start", gap: 1, mb: 1 }}>
            <Box sx={{ flexGrow: 1, minWidth: 0 }}>
              <Typography variant="subtitle2" noWrap>
                {title}
              </Typography>
              {subtitle ? (
                <Typography variant="caption" color="text.secondary">
                  {subtitle}
                </Typography>
              ) : null}
            </Box>
            {actions ? <Box onClick={(e) => e.stopPropagation()}>{actions}</Box> : null}
          </Box>
        ) : null}
        <Box sx={{ display: "grid", gridTemplateColumns: "minmax(90px, 35%) 1fr", columnGap: 1.5, rowGap: 0.5 }}>{children}</Box>
      </CardContent>
    </Card>
  );
}

export type LyoRecordFieldProps = {
  label: ReactNode;
  value?: ReactNode;
  children?: ReactNode;
  hideWhenEmpty?: boolean;
};

export function LyoRecordField({ label, value, children, hideWhenEmpty }: LyoRecordFieldProps) {
  const content = children ?? value;
  const empty = content == null || content === "";
  if (empty && hideWhenEmpty) return null;
  return (
    <>
      <Typography variant="caption" color="text.secondary" sx={{ alignSelf: "center" }}>
        {label}
      </Typography>
      <Box sx={{ fontSize: 14, minWidth: 0, wordBreak: "break-word" }}>{empty ? "—" : content}</Box>
    </>
  );
}
