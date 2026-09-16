"use client";

import { Children, createContext, isValidElement, useContext, useState, type ReactElement, type ReactNode } from "react";
import Box from "@mui/material/Box";
import Button, { type ButtonProps } from "@mui/material/Button";
import Divider from "@mui/material/Divider";
import IconButton, { type IconButtonProps } from "@mui/material/IconButton";
import Menu from "@mui/material/Menu";
import Tooltip from "@mui/material/Tooltip";
import ArrowDropDownIcon from "@mui/icons-material/ArrowDropDown";
import UnfoldLessIcon from "@mui/icons-material/UnfoldLess";
import UnfoldMoreIcon from "@mui/icons-material/UnfoldMore";

type ToolbarContextValue = { disabled: boolean; dense: boolean };

const ToolbarContext = createContext<ToolbarContextValue>({ disabled: false, dense: false });

function useToolbarDisabled(disabled?: boolean): boolean {
  return Boolean(disabled) || useContext(ToolbarContext).disabled;
}

const rowSx = { display: "flex", flexWrap: "wrap", alignItems: "center", gap: 1, minHeight: 40 } as const;

export type LyoToolbarProps = {
  start?: ReactNode;
  end?: ReactNode;
  /** Middle, growing group. Ignored when `rows` is set. */
  children?: ReactNode;
  /** Multi-row layout of `LyoToolbarRow` elements. Rows marked `collapsible` hide behind a toggle on the first visible row. */
  rows?: ReactNode;
  dense?: boolean;
  /** Disables every toolbar button, icon button, and menu inside. */
  disabled?: boolean;
  rowsExpanded?: boolean;
  onRowsExpandedChange?: (expanded: boolean) => void;
  showRowToggle?: boolean;
  className?: string;
};

/** Toolbar shell: a single start / middle / end row, or explicit rows with optional collapsible extras. */
export function LyoToolbar({
  start,
  end,
  children,
  rows,
  dense,
  disabled = false,
  rowsExpanded,
  onRowsExpandedChange,
  showRowToggle = true,
  className,
}: LyoToolbarProps) {
  const [localExpanded, setLocalExpanded] = useState(true);
  const expanded = rowsExpanded ?? localExpanded;
  const toggle = () => {
    setLocalExpanded(!expanded);
    onRowsExpandedChange?.(!expanded);
  };

  const rowElements = Children.toArray(rows).filter(isValidElement) as ReactElement<LyoToolbarRowProps>[];
  const hasCollapsible = rowElements.some((r) => r.props.collapsible);
  const visibleRows = rowElements.filter((r) => expanded || !r.props.collapsible);

  return (
    <ToolbarContext.Provider value={{ disabled, dense: Boolean(dense) }}>
      <Box role="toolbar" className={className} sx={{ display: "flex", flexDirection: "column", gap: dense ? 0.5 : 1, py: dense ? 0.25 : 0.5 }}>
        {rows ? (
          visibleRows.map((row, i) => (
            <Box key={row.key ?? i} sx={rowSx}>
              {row.props.children}
              {i === 0 && hasCollapsible && showRowToggle ? (
                <LyoToolbarIconButton
                  tooltip={expanded ? "Hide extra rows" : "Show extra rows"}
                  icon={expanded ? <UnfoldLessIcon fontSize="small" /> : <UnfoldMoreIcon fontSize="small" />}
                  onClick={toggle}
                  disabled={false}
                />
              ) : null}
            </Box>
          ))
        ) : (
          <Box sx={rowSx}>
            {start ? <LyoToolbarGroup>{start}</LyoToolbarGroup> : null}
            {children ? <LyoToolbarGroup grow>{children}</LyoToolbarGroup> : <Box sx={{ flexGrow: 1 }} />}
            {end ? <LyoToolbarGroup alignEnd>{end}</LyoToolbarGroup> : null}
          </Box>
        )}
      </Box>
    </ToolbarContext.Provider>
  );
}

export type LyoToolbarRowProps = { children?: ReactNode; collapsible?: boolean };

/** Marker for `LyoToolbar rows`; rendered by the toolbar so collapsible rows can hide together. */
export function LyoToolbarRow({ children }: LyoToolbarRowProps) {
  return <Box sx={rowSx}>{children}</Box>;
}

export function LyoToolbarGroup({ children, grow, alignEnd }: { children?: ReactNode; grow?: boolean; alignEnd?: boolean }) {
  return (
    <Box sx={{ display: "flex", flexWrap: "wrap", alignItems: "center", gap: 1, flexGrow: grow ? 1 : 0, ml: alignEnd ? "auto" : 0 }}>
      {children}
    </Box>
  );
}

export function LyoToolbarSeparator() {
  return <Divider orientation="vertical" flexItem sx={{ mx: 0.5 }} />;
}

function withTooltip(tooltip: string | undefined, node: ReactElement) {
  if (!tooltip) return node;
  return (
    <Tooltip title={tooltip}>
      <span>{node}</span>
    </Tooltip>
  );
}

export type LyoToolbarButtonProps = Omit<ButtonProps, "size"> & { tooltip?: string; icon?: ReactNode };

/** Small outlined button that honours the toolbar-wide `disabled`. */
export function LyoToolbarButton({ tooltip, icon, disabled, variant = "outlined", color = "inherit", children, ...rest }: LyoToolbarButtonProps) {
  const isDisabled = useToolbarDisabled(disabled);
  return withTooltip(
    tooltip,
    <Button size="small" variant={variant} color={color} startIcon={icon} disabled={isDisabled} {...rest}>
      {children}
    </Button>
  );
}

export type LyoToolbarIconButtonProps = Omit<IconButtonProps, "size" | "children"> & { tooltip?: string; icon: ReactNode };

export function LyoToolbarIconButton({ tooltip, icon, disabled, ...rest }: LyoToolbarIconButtonProps) {
  const isDisabled = useToolbarDisabled(disabled);
  return withTooltip(
    tooltip,
    <IconButton size="small" disabled={isDisabled} aria-label={tooltip} {...rest}>
      {icon}
    </IconButton>
  );
}

export type LyoToolbarMenuProps = {
  /** Button text; omit for an icon-only activator. */
  label?: ReactNode;
  icon?: ReactNode;
  tooltip?: string;
  disabled?: boolean;
  /** Menu items. Receives `close` so items can dismiss the menu after acting. */
  children: ReactNode | ((close: () => void) => ReactNode);
};

export function LyoToolbarMenu({ label, icon, tooltip, disabled, children }: LyoToolbarMenuProps) {
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const isDisabled = useToolbarDisabled(disabled);
  const close = () => setAnchor(null);
  const activator = label ? (
    <Button size="small" variant="outlined" color="inherit" startIcon={icon} endIcon={<ArrowDropDownIcon />} disabled={isDisabled} onClick={(e) => setAnchor(e.currentTarget)}>
      {label}
    </Button>
  ) : (
    <IconButton size="small" disabled={isDisabled} aria-label={tooltip} onClick={(e) => setAnchor(e.currentTarget)}>
      {icon ?? <ArrowDropDownIcon />}
    </IconButton>
  );
  return (
    <>
      {withTooltip(tooltip, activator)}
      <Menu anchorEl={anchor} open={Boolean(anchor)} onClose={close}>
        {Children.toArray(typeof children === "function" ? children(close) : children)}
      </Menu>
    </>
  );
}

/** Fixed-width slot for a search box or select inside a toolbar. */
export function LyoToolbarField({ children, width = 240, grow }: { children?: ReactNode; width?: number | string; grow?: boolean }) {
  return <Box sx={{ width: grow ? "auto" : width, minWidth: grow ? width : undefined, flexGrow: grow ? 1 : 0 }}>{children}</Box>;
}
