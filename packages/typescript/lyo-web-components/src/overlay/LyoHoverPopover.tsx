"use client";

import { useEffect, useRef, useState, type KeyboardEvent, type ReactNode } from "react";
import Box from "@mui/material/Box";
import IconButton from "@mui/material/IconButton";
import Paper from "@mui/material/Paper";
import Popper from "@mui/material/Popper";
import Typography from "@mui/material/Typography";
import CloseIcon from "@mui/icons-material/Close";

export type LyoHoverPopoverProps = {
  children: ReactNode;
  /** Plain text body; ignored when `content` is set. */
  text?: string;
  content?: ReactNode;
  title?: ReactNode;
  openDelayMs?: number;
  closeDelayMs?: number;
  maxWidth?: number | string;
  maxHeight?: number | string;
  /** Click pins the popover open until closed or Escape. */
  pinnable?: boolean;
  disabled?: boolean;
  onOpenChange?: (open: boolean) => void;
};

/** Opens on hover or focus after a delay, stays open while the pointer is over it, and pins on click. */
export function LyoHoverPopover({
  children,
  text,
  content,
  title,
  openDelayMs = 150,
  closeDelayMs = 300,
  maxWidth = "32rem",
  maxHeight = "22rem",
  pinnable = true,
  disabled,
  onOpenChange,
}: LyoHoverPopoverProps) {
  const anchorRef = useRef<HTMLSpanElement>(null);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [open, setOpen] = useState(false);
  const [pinned, setPinned] = useState(false);

  const clear = () => {
    if (timer.current) clearTimeout(timer.current);
    timer.current = null;
  };
  const change = (next: boolean) => {
    setOpen(next);
    onOpenChange?.(next);
  };
  const scheduleOpen = () => {
    if (disabled) return;
    clear();
    timer.current = setTimeout(() => change(true), openDelayMs);
  };
  const scheduleClose = () => {
    if (pinned) return;
    clear();
    timer.current = setTimeout(() => change(false), closeDelayMs);
  };
  const close = () => {
    clear();
    setPinned(false);
    change(false);
  };
  const onKeyDown = (e: KeyboardEvent) => {
    if (e.key === "Escape") close();
  };

  useEffect(() => clear, []);

  return (
    <Box
      component="span"
      ref={anchorRef}
      tabIndex={0}
      onPointerEnter={scheduleOpen}
      onPointerLeave={scheduleClose}
      onFocus={scheduleOpen}
      onBlur={scheduleClose}
      onKeyDown={onKeyDown}
      onClick={() => {
        if (!pinnable || disabled) return;
        clear();
        setPinned(true);
        change(true);
      }}
      sx={{ cursor: disabled ? undefined : "help", display: "inline-flex" }}
    >
      {children}
      <Popper open={open && !disabled} anchorEl={anchorRef.current} placement="bottom-start" sx={{ zIndex: (t) => t.zIndex.tooltip }}>
        <Paper
          elevation={8}
          onPointerEnter={clear}
          onPointerLeave={scheduleClose}
          onKeyDown={onKeyDown}
          onClick={(e) => e.stopPropagation()}
          sx={{ maxWidth, maxHeight, overflow: "auto", p: 1.5 }}
        >
          {title || pinned ? (
            <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 0.5 }}>
              <Typography variant="subtitle2" sx={{ flexGrow: 1 }}>
                {title}
              </Typography>
              {pinned ? (
                <IconButton size="small" onClick={close} aria-label="Close">
                  <CloseIcon fontSize="inherit" />
                </IconButton>
              ) : null}
            </Box>
          ) : null}
          {content ?? (
            <Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>
              {text}
            </Typography>
          )}
        </Paper>
      </Popper>
    </Box>
  );
}
