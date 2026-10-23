export type LyoChipColor = "default" | "primary" | "secondary" | "success" | "error" | "warning" | "info";

export type LyoChipVariant = "filled" | "outlined";

/** Icon keys understood by `LyoChip`; hosts can also pass a ReactNode icon directly. */
export type LyoChipIconKey = "check" | "warning" | "error" | "timer" | "play" | "schedule" | "cancel" | "skip" | "pause" | "help";

/** Display spec consumed by `LyoChip`. `hue` wins over `color` for the fill. */
export type LyoChipSpec = {
  label: string;
  color?: LyoChipColor;
  icon?: LyoChipIconKey;
  variant?: LyoChipVariant;
  hue?: number;
};

/**
 * Named HSL hues for identity chips (formats, kinds). Prefer these over theme primary/secondary, which carry meaning elsewhere.
 * Spaced around the wheel away from the theme error/warning/success/info hues.
 */
export const LyoChipHues = {
  Coral: 18,
  Amber: 52,
  Lime: 84,
  Green: 152,
  Teal: 174,
  Cyan: 192,
  Blue: 228,
  Indigo: 252,
  Violet: 274,
  Purple: 292,
  Magenta: 312,
  Rose: 332,
} as const;

export type LyoChipHueName = keyof typeof LyoChipHues;

/** Named hues in wheel order, Coral through Rose. */
export const LyoChipHueList: readonly number[] = Object.values(LyoChipHues);

export function normalizeHue(degrees: number): number {
  const hue = Math.round(degrees) % 360;
  return hue < 0 ? hue + 360 : hue;
}

export function hueColor(degrees: number): string {
  return `hsl(${normalizeHue(degrees)} 72% 50%)`;
}

/** Blank labels become an em dash. */
export function chipOf(
  label: string | null | undefined,
  options: { color?: LyoChipColor; icon?: LyoChipIconKey; variant?: LyoChipVariant; hue?: number } = {}
): LyoChipSpec {
  const spec: LyoChipSpec = {
    label: label == null || !label.trim() ? "—" : label,
    color: options.color ?? "default",
    variant: options.variant ?? "filled",
  };
  if (options.icon) spec.icon = options.icon;
  if (options.hue != null) spec.hue = options.hue;
  return spec;
}

/** Matches `text` case-insensitively against `values`. Unknown or blank text keeps the raw label with the default colour. */
export function chipFromEnum<T extends string>(
  text: string | null | undefined,
  values: readonly T[],
  color: (value: T) => LyoChipColor,
  icon?: (value: T) => LyoChipIconKey | undefined
): LyoChipSpec {
  const match = values.find((v) => v.toLowerCase() === (text ?? "").trim().toLowerCase());
  if (!match) return chipOf(text);
  return chipOf(match, { color: color(match), icon: icon?.(match) });
}

/** Nullable bool to labels and colours. Null is an em dash with the default colour. */
export function chipFromBool(
  value: boolean | null | undefined,
  trueLabel: string,
  falseLabel: string,
  trueColor: LyoChipColor,
  falseColor: LyoChipColor
): LyoChipSpec {
  if (value === true) return chipOf(trueLabel, { color: trueColor });
  if (value === false) return chipOf(falseLabel, { color: falseColor });
  return chipOf(null);
}
