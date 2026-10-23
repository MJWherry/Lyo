/** Width hint for a field inside `LyoFormGrid`. `Auto` derives it from the input kind. */
export type LyoFieldSize = "Auto" | "Small" | "Medium" | "Large" | "Full";

/** Editor a typed `LyoFormInput` draws. */
export type LyoFormInputKind = "string" | "richtext" | "bool" | "integer" | "number" | "enum" | "flags" | "date" | "datetime" | "time" | "guid";

export type LyoFieldColumns = { xs: number; sm: number; md: number };

const COLUMNS: Record<Exclude<LyoFieldSize, "Auto">, LyoFieldColumns> = {
  Small: { xs: 6, sm: 4, md: 3 },
  Medium: { xs: 12, sm: 6, md: 4 },
  Large: { xs: 12, sm: 12, md: 6 },
  Full: { xs: 12, sm: 12, md: 12 },
};

export function fieldSizeColumns(size: Exclude<LyoFieldSize, "Auto">): LyoFieldColumns {
  return COLUMNS[size];
}

/** Bools, numbers, dates, and times are narrow; enums, flags, date-times, and GUIDs medium; strings wide; multiline and rich text full width. */
export function autoFieldSize(kind: LyoFormInputKind, options: { multiline?: boolean } = {}): Exclude<LyoFieldSize, "Auto"> {
  if (options.multiline || kind === "richtext") return "Full";
  switch (kind) {
    case "bool":
    case "integer":
    case "number":
    case "date":
    case "time":
      return "Small";
    case "enum":
    case "flags":
    case "datetime":
    case "guid":
      return "Medium";
    default:
      return "Large";
  }
}

export function resolveFieldSize(size: LyoFieldSize | undefined, kind: LyoFormInputKind, options: { multiline?: boolean } = {}): LyoFieldColumns {
  return fieldSizeColumns(!size || size === "Auto" ? autoFieldSize(kind, options) : size);
}
