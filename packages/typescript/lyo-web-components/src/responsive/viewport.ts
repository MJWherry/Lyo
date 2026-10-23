export type LyoBreakpoint = "xs" | "sm" | "md" | "lg" | "xl";

/** MUI default breakpoint lower bounds in px. */
export const LYO_BREAKPOINTS: Record<LyoBreakpoint, number> = { xs: 0, sm: 600, md: 900, lg: 1200, xl: 1536 };

const ORDER: LyoBreakpoint[] = ["xs", "sm", "md", "lg", "xl"];

export function breakpointOf(width: number): LyoBreakpoint {
  let current: LyoBreakpoint = "xs";
  for (const bp of ORDER) if (width >= LYO_BREAKPOINTS[bp]) current = bp;
  return current;
}

/** True when `width` falls in `reference` or a narrower breakpoint. Null width (SSR, before first measure) is false so the wide layout renders first. */
export function isAtOrBelow(width: number | null | undefined, reference: LyoBreakpoint): boolean {
  if (width == null) return false;
  return ORDER.indexOf(breakpointOf(width)) <= ORDER.indexOf(reference);
}
