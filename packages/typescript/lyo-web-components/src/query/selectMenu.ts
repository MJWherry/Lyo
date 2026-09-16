import type { MenuProps } from "@mui/material/Menu";

export const LYO_SELECT_MENU_MAX_HEIGHT = 320;

/** Select `MenuProps` for long option lists: caps the menu at `LYO_SELECT_MENU_MAX_HEIGHT` and scrolls instead of filling the viewport. */
export function compactMenuProps(extra: Partial<MenuProps> = {}): Partial<MenuProps> {
  return { ...extra, slotProps: { ...extra.slotProps, paper: { sx: { maxHeight: LYO_SELECT_MENU_MAX_HEIGHT, overflowY: "auto" } } } };
}
