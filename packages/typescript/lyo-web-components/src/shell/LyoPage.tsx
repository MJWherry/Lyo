"use client";

import type { ReactNode } from "react";
import Box from "@mui/material/Box";
import Breadcrumbs from "@mui/material/Breadcrumbs";
import Divider from "@mui/material/Divider";
import Link from "@mui/material/Link";
import Paper from "@mui/material/Paper";
import Typography, { type TypographyProps } from "@mui/material/Typography";

export type LyoBreadcrumb = { text: string; href?: string; onClick?: () => void };

/** Breadcrumb trail; the last item is the current page and is not a link. */
export function LyoBreadcrumbs({ items, separator = "/" }: { items?: readonly LyoBreadcrumb[]; separator?: ReactNode }) {
  if (!items?.length) return null;
  return (
    <Breadcrumbs separator={separator} sx={{ fontSize: 13 }}>
      {items.map((item, i) =>
        i === items.length - 1 || (!item.href && !item.onClick) ? (
          <Typography key={i} variant="body2" color={i === items.length - 1 ? "text.primary" : "text.secondary"}>
            {item.text}
          </Typography>
        ) : (
          <Link key={i} href={item.href} onClick={item.onClick} underline="hover" color="text.secondary" variant="body2" sx={{ cursor: "pointer" }}>
            {item.text}
          </Link>
        )
      )}
    </Breadcrumbs>
  );
}

export type LyoPageHeaderProps = {
  title?: ReactNode;
  description?: ReactNode;
  icon?: ReactNode;
  titleVariant?: TypographyProps["variant"];
  breadcrumbs?: readonly LyoBreadcrumb[];
  chips?: ReactNode;
  actions?: ReactNode;
  divider?: boolean;
};

/** Page title row with optional breadcrumbs, icon, chips, description, and right-aligned actions. */
export function LyoPageHeader({ title, description, icon, titleVariant = "h5", breadcrumbs, chips, actions, divider }: LyoPageHeaderProps) {
  return (
    <Box sx={{ mb: 2 }}>
      <LyoBreadcrumbs items={breadcrumbs} />
      <Box sx={{ display: "flex", alignItems: "center", gap: 1.5, flexWrap: "wrap", mt: breadcrumbs?.length ? 0.5 : 0 }}>
        {icon ? <Box sx={{ display: "flex", color: "primary.main" }}>{icon}</Box> : null}
        <Typography variant={titleVariant} component="h1">
          {title}
        </Typography>
        {chips ? <Box sx={{ display: "flex", gap: 0.5, flexWrap: "wrap" }}>{chips}</Box> : null}
        {actions ? <Box sx={{ ml: "auto", display: "flex", gap: 1, flexWrap: "wrap" }}>{actions}</Box> : null}
      </Box>
      {description ? (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
          {description}
        </Typography>
      ) : null}
      {divider ? <Divider sx={{ mt: 1.5 }} /> : null}
    </Box>
  );
}

export type LyoSectionProps = {
  title?: ReactNode;
  description?: ReactNode;
  icon?: ReactNode;
  titleVariant?: TypographyProps["variant"];
  actions?: ReactNode;
  children?: ReactNode;
  /** Remove body padding (for tables and grids that bring their own). */
  flush?: boolean;
  outlined?: boolean;
  elevation?: number;
};

/** Paper section with an optional title/actions header. */
export function LyoSection({ title, description, icon, titleVariant = "subtitle1", actions, children, flush, outlined = true, elevation = 0 }: LyoSectionProps) {
  const hasHeader = Boolean(title || actions);
  return (
    <Paper variant={outlined ? "outlined" : "elevation"} elevation={outlined ? undefined : elevation} sx={{ mb: 2 }}>
      {hasHeader ? (
        <Box sx={{ display: "flex", alignItems: "flex-start", gap: 1, px: 2, pt: 1.5, pb: flush ? 1.5 : 0 }}>
          <Box sx={{ flexGrow: 1 }}>
            <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
              {icon}
              <Typography variant={titleVariant}>{title}</Typography>
            </Box>
            {description ? (
              <Typography variant="caption" color="text.secondary">
                {description}
              </Typography>
            ) : null}
          </Box>
          {actions ? <Box sx={{ display: "flex", gap: 1 }}>{actions}</Box> : null}
        </Box>
      ) : null}
      <Box sx={flush ? undefined : { p: 2 }}>{children}</Box>
    </Paper>
  );
}
