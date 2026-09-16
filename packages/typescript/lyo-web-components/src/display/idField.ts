export type LyoIdAbbreviation = "suffix" | "prefix" | "none";

export const DEFAULT_ID_ABBREVIATION_LENGTH = 9;

function isNumericId(text: string): boolean {
  return /^\d+$/.test(text);
}

function effectiveMode(text: string, mode: LyoIdAbbreviation): LyoIdAbbreviation {
  return isNumericId(text) ? "none" : mode;
}

/** Abbreviates an id to `length` characters from the end (default) or start. Digit-only ids are never abbreviated; blanks read `null`. */
export function abbreviateId(
  value: unknown,
  mode: LyoIdAbbreviation = "suffix",
  length: number = DEFAULT_ID_ABBREVIATION_LENGTH
): string {
  const text = value == null ? "" : String(value).trim();
  if (!text) return "null";
  const effective = effectiveMode(text, mode);
  if (effective === "none" || length < 1 || text.length <= length) return text;
  return effective === "prefix" ? text.slice(0, length) : text.slice(-length);
}

/** True when abbreviation hides characters, so the expand toggle is worth showing. */
export function idFieldCanToggle(
  value: unknown,
  mode: LyoIdAbbreviation = "suffix",
  length: number = DEFAULT_ID_ABBREVIATION_LENGTH
): boolean {
  const text = value == null ? "" : String(value).trim();
  if (!text) return false;
  return effectiveMode(text, mode) !== "none" && text.length > Math.max(1, length);
}
