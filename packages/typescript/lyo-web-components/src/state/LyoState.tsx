"use client";

import type { ReactNode } from "react";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Skeleton from "@mui/material/Skeleton";
import Typography, { type TypographyProps } from "@mui/material/Typography";
import InboxIcon from "@mui/icons-material/Inbox";
import { formatResultErrors, resolveResultPhase, type LyoResultError, type LyoResultLike } from "./resultPhase.js";

export type LyoEmptyStateProps = {
  title?: ReactNode;
  description?: ReactNode;
  /** Pass null to hide the icon. */
  icon?: ReactNode | null;
  titleVariant?: TypographyProps["variant"];
  actions?: ReactNode;
};

export function LyoEmptyState({ title = "Nothing to show", description, icon, titleVariant = "h6", actions }: LyoEmptyStateProps) {
  return (
    <Box sx={{ display: "flex", flexDirection: "column", alignItems: "center", textAlign: "center", gap: 1, py: 4, px: 2, color: "text.secondary" }}>
      {icon === null ? null : (icon ?? <InboxIcon sx={{ fontSize: 48, opacity: 0.6 }} />)}
      <Typography variant={titleVariant} color="text.primary">
        {title}
      </Typography>
      {description ? <Typography variant="body2">{description}</Typography> : null}
      {actions ? <Box sx={{ display: "flex", gap: 1, mt: 1 }}>{actions}</Box> : null}
    </Box>
  );
}

export type LyoSkeletonPanelProps = {
  rows?: number;
  showHeader?: boolean;
  variant?: "text" | "rectangular" | "rounded";
  rowHeight?: number | string;
  headerHeight?: number | string;
  animation?: "wave" | "pulse" | false;
};

export function LyoSkeletonPanel({ rows = 4, showHeader, variant = "text", rowHeight = "1.5rem", headerHeight = "2rem", animation = "wave" }: LyoSkeletonPanelProps) {
  return (
    <Box sx={{ display: "flex", flexDirection: "column", gap: 1 }} aria-busy="true">
      {showHeader ? <Skeleton variant="rectangular" height={headerHeight} animation={animation} width="40%" /> : null}
      {Array.from({ length: rows }, (_, i) => (
        <Skeleton key={i} variant={variant} height={rowHeight} animation={animation} />
      ))}
    </Box>
  );
}

export type LyoResultBoundaryProps<T> = {
  result: LyoResultLike<T> | null | undefined;
  loading?: boolean;
  isEmpty?: (data: T) => boolean;
  children: (data: T) => ReactNode;
  loadingContent?: ReactNode;
  emptyContent?: ReactNode;
  errorContent?: (errors: readonly LyoResultError[]) => ReactNode;
  emptyTitle?: string;
  emptyDescription?: string;
  emptyActions?: ReactNode;
  skeletonRows?: number;
  /** Shows a Retry action on the default error alert. */
  onRetry?: () => void;
};

/** Renders loading, error, empty, or content for a `Result<T>`-shaped value. */
export function LyoResultBoundary<T>({
  result,
  loading,
  isEmpty,
  children,
  loadingContent,
  emptyContent,
  errorContent,
  emptyTitle = "Nothing to show",
  emptyDescription,
  emptyActions,
  skeletonRows = 4,
  onRetry,
}: LyoResultBoundaryProps<T>) {
  const phase = resolveResultPhase({ result, loading, isEmpty });
  if (phase === "loading") return <>{loadingContent ?? <LyoSkeletonPanel rows={skeletonRows} />}</>;
  if (phase === "error") {
    const errors = result?.errors ?? [];
    if (errorContent) return <>{errorContent(errors)}</>;
    return (
      <Alert
        severity="error"
        sx={{ whiteSpace: "pre-line" }}
        action={
          onRetry ? (
            <Button color="inherit" size="small" onClick={onRetry}>
              Retry
            </Button>
          ) : undefined
        }
      >
        {formatResultErrors(errors)}
      </Alert>
    );
  }
  if (phase === "empty") return <>{emptyContent ?? <LyoEmptyState title={emptyTitle} description={emptyDescription} actions={emptyActions} />}</>;
  return <>{children(result!.data as T)}</>;
}
