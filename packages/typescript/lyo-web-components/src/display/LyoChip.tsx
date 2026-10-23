"use client";

import type { ReactElement, ReactNode } from "react";
import Chip, { type ChipProps } from "@mui/material/Chip";
import CancelIcon from "@mui/icons-material/Cancel";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import ErrorIcon from "@mui/icons-material/Error";
import HelpIcon from "@mui/icons-material/Help";
import PauseCircleIcon from "@mui/icons-material/PauseCircle";
import PlayCircleIcon from "@mui/icons-material/PlayCircle";
import ScheduleIcon from "@mui/icons-material/Schedule";
import SkipNextIcon from "@mui/icons-material/SkipNext";
import TimerIcon from "@mui/icons-material/Timer";
import WarningIcon from "@mui/icons-material/Warning";
import { chipFromBool, chipFromEnum, chipOf, hueColor, type LyoChipIconKey, type LyoChipSpec } from "./chips.js";
import { createStatusPaletteResolver, type LyoStatusPalette } from "./status.js";

const ICONS: Record<LyoChipIconKey, ReactElement> = {
  check: <CheckCircleIcon />,
  warning: <WarningIcon />,
  error: <ErrorIcon />,
  timer: <TimerIcon />,
  play: <PlayCircleIcon />,
  schedule: <ScheduleIcon />,
  cancel: <CancelIcon />,
  skip: <SkipNextIcon />,
  pause: <PauseCircleIcon />,
  help: <HelpIcon />,
};

export type LyoChipProps = Omit<ChipProps, "icon"> & {
  /** Display spec from `LyoChips` / status resolvers; explicit props override its fields. */
  spec?: LyoChipSpec;
  /** HSL hue for identity chips (see `LyoChipHues`); wins over `color`. */
  hue?: number;
  icon?: LyoChipIconKey | ReactElement;
};

/** MUI Chip that also takes a `LyoChipSpec`, an icon key, or a named hue. Defaults to size small. */
export function LyoChip({ spec, hue, icon, label, color, variant, size = "small", sx, ...rest }: LyoChipProps) {
  const resolvedHue = hue ?? spec?.hue;
  const resolvedIcon = icon ?? spec?.icon;
  const iconElement = typeof resolvedIcon === "string" ? ICONS[resolvedIcon] : resolvedIcon;
  const outlined = (variant ?? spec?.variant) === "outlined";
  const hueSx =
    resolvedHue == null
      ? undefined
      : outlined
        ? { borderColor: hueColor(resolvedHue), color: hueColor(resolvedHue), "& .MuiChip-icon": { color: "inherit" } }
        : { bgcolor: hueColor(resolvedHue), color: "#fff", "& .MuiChip-icon": { color: "inherit" } };
  return (
    <Chip
      size={size}
      label={label ?? spec?.label}
      color={resolvedHue == null ? (color ?? spec?.color ?? "default") : "default"}
      variant={variant ?? spec?.variant ?? "filled"}
      icon={iconElement}
      sx={[hueSx ?? {}, ...(Array.isArray(sx) ? sx : [sx])]}
      {...rest}
    />
  );
}

/** Factory helpers that return `LyoChipSpec` for `<LyoChip spec={...} />`. */
export const LyoChips = {
  of: chipOf,
  fromBool: chipFromBool,
  fromEnum: chipFromEnum,
};

export type LyoStatusChipProps = Omit<LyoChipProps, "spec"> & {
  status: string | null | undefined;
  /** Domain palette name registered in `palettes`. */
  palette?: string | null;
  palettes?: readonly LyoStatusPalette[];
  /** Replaces the resolved label. */
  children?: ReactNode;
};

/** Status chip: domain palette, then the shared status vocabulary, then a neutral chip with the raw text. */
export function LyoStatusChip({ status, palette, palettes, children, ...rest }: LyoStatusChipProps) {
  const spec = createStatusPaletteResolver(palettes).resolve(status, palette);
  return <LyoChip spec={spec} label={children ?? spec.label} {...rest} />;
}
