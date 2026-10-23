import { LyoChipHues, chipOf, type LyoChipSpec } from "../display/chips.js";
import {
  LYO_TYPE_CATEGORIES,
  LyoTypes,
  allLyoTypes,
  clrListFullName,
  isLyoCollection,
  lyoTypeFromName,
  lyoTypesByCategory,
  type LyoTypeCategory,
  type LyoTypeInfo,
} from "./lyoTypeCatalog.js";

/** Sentinel for the Custom… picker row (not a real CLR type). */
export const CUSTOM_TYPE_TOKEN = "__custom__";

export type LyoTypeCollectionShape = "Value" | "List" | "Array";

/** JSON-array element kind for chip editors and option selects. */
export type LyoListJsonKind = "String" | "Number" | "Bool";

/** Stored name of the registered formatter template type, or null when none is registered. */
export function templateTypeName(): string | null {
  return allLyoTypes().find((t) => t.editorKind === "Formatter")?.fullName ?? null;
}

const CATEGORY_HUES: Partial<Record<LyoTypeCategory, number>> = {
  Text: LyoChipHues.Blue,
  Boolean: LyoChipHues.Lime,
  Integer: LyoChipHues.Teal,
  Number: LyoChipHues.Cyan,
  Temporal: LyoChipHues.Amber,
  Identifier: LyoChipHues.Indigo,
  Enum: LyoChipHues.Violet,
  Json: LyoChipHues.Magenta,
  Collection: LyoChipHues.Coral,
  Binary: LyoChipHues.Purple,
};

export function hueForCategory(category: LyoTypeCategory): number | null {
  return CATEGORY_HUES[category] ?? null;
}

function capitalize(value: string): string {
  return value && value[0] !== value[0]!.toUpperCase() ? value[0]!.toUpperCase() + value.slice(1) : value;
}

/** Capitalized picker/chip label; collections draw as `List<Int>` / `Int[]`. Unknown names show as typed, blanks as an em dash. */
export function displayName(typeOrName: LyoTypeInfo | string | null | undefined): string {
  if (typeOrName == null || typeof typeOrName === "string") {
    const name = typeOrName?.trim();
    if (!name) return "—";
    const info = lyoTypeFromName(name);
    return info === LyoTypes.Unknown ? name : displayName(info);
  }
  const info = typeOrName;
  if (isLyoCollection(info) && info.category !== "Binary") {
    const element = displayName(lyoTypeFromName(info.elementFullName));
    return info.isArray ? `${element}[]` : `List<${element}>`;
  }
  return capitalize(info.shortName);
}

/** Chip for a stored type name: Formatter is Rose, Regex / Xml are warning, other categories use their identity hue. */
export function typeChip(typeName: string | null | undefined): LyoChipSpec {
  const info = lyoTypeFromName(typeName);
  const label = displayName(typeName);
  if (info.editorKind === "Formatter") return chipOf(label, { hue: LyoChipHues.Rose });
  if (info.editorKind === "Regex" || info.editorKind === "Xml") return chipOf(label, { color: "warning" });
  const hue = hueForCategory(info.category);
  return hue == null ? chipOf(label) : chipOf(label, { hue });
}

export type LyoTypePickerGroup = { category: LyoTypeCategory; types: LyoTypeInfo[] };

/** Non-collection rows grouped by category (category order), sorted by display name. */
export function pickerGroups(): LyoTypePickerGroup[] {
  return LYO_TYPE_CATEGORIES.filter((c) => c !== "Unknown" && c !== "Collection")
    .map((category) => ({
      category,
      types: lyoTypesByCategory(category)
        .filter((t) => !isLyoCollection(t))
        .sort((a, b) => displayName(a).localeCompare(displayName(b), undefined, { sensitivity: "base" })),
    }))
    .filter((g) => g.types.length > 0);
}

const WRAPPABLE: readonly LyoTypeCategory[] = ["Text", "Boolean", "Integer", "Number", "Temporal", "Identifier"];

/** True when the type can be wrapped as `List<T>` or `T[]` in the picker. */
export function canWrapInCollection(info: LyoTypeInfo): boolean {
  return (
    info !== LyoTypes.Unknown &&
    info !== LyoTypes.Enum &&
    info.isClr &&
    !isLyoCollection(info) &&
    WRAPPABLE.includes(info.category) &&
    info.editorKind !== "Regex" &&
    info.editorKind !== "Xml" &&
    info.editorKind !== "Formatter"
  );
}

/** List / array shape encoded in a stored name. `byte[]` stays a scalar binary value. */
export function collectionShape(typeName: string | null | undefined): LyoTypeCollectionShape {
  const info = lyoTypeFromName(typeName);
  if (!isLyoCollection(info) || info.category === "Binary") return "Value";
  return info.isArray ? "Array" : "List";
}

/** Element full name for a list / array; the normalized full name for a known scalar; otherwise the trimmed input. */
export function elementFullName(typeName: string | null | undefined): string {
  const info = lyoTypeFromName(typeName);
  if (isLyoCollection(info) && info.category !== "Binary") return info.elementFullName!;
  if (info !== LyoTypes.Unknown && info !== LyoTypes.Enum) return info.fullName;
  return typeName?.trim() || LyoTypes.String.fullName;
}

/** Stored full name for an element type plus list / array shape. Types that cannot be wrapped keep their own name. */
export function composeFullName(element: string | null | undefined, shape: LyoTypeCollectionShape): string {
  const trimmed = element?.trim() ?? "";
  if (!trimmed || trimmed === CUSTOM_TYPE_TOKEN) return trimmed;
  const info = lyoTypeFromName(trimmed);
  if (shape === "Value" || !canWrapInCollection(info)) return info === LyoTypes.Unknown ? trimmed : info.fullName;
  return shape === "List" ? clrListFullName(info.fullName) : `${info.fullName}[]`;
}

/** True when the picker needs a typed CLR full name (Custom…, Enum, or unknown). */
export function needsConcreteFullName(typeName: string | null | undefined): boolean {
  if (!typeName?.trim() || typeName === CUSTOM_TYPE_TOKEN) return true;
  const info = lyoTypeFromName(typeName);
  return info === LyoTypes.Enum || info === LyoTypes.Unknown;
}

/** Value for the CLR full name field; blank for Custom… and for the Enum sentinel. */
export function concreteFullNameDisplay(typeName: string | null | undefined): string {
  const trimmed = typeName?.trim() ?? "";
  return !trimmed || trimmed === CUSTOM_TYPE_TOKEN || trimmed === LyoTypes.Enum.fullName ? "" : trimmed;
}

/** Clearing the CLR name while editing an enum keeps the Enum sentinel. */
export function concreteFullNameFromInput(currentType: string | null | undefined, typed: string | null | undefined): string {
  if (typed?.trim()) return typed.trim();
  return lyoTypeFromName(currentType) === LyoTypes.Enum ? LyoTypes.Enum.fullName : "";
}

function elementInfo(typeName: string | null | undefined): LyoTypeInfo {
  const info = lyoTypeFromName(typeName);
  return isLyoCollection(info) ? lyoTypeFromName(info.elementFullName) : info;
}

/** Pattern that chips must match for numeric, boolean, and GUID elements; null when any text is allowed. */
export function chipValidation(typeName: string | null | undefined): { pattern: RegExp; message: string } | null {
  const element = elementInfo(typeName);
  switch (element.category) {
    case "Integer":
      return { pattern: /^[+-]?\d+$/, message: "Enter an integer." };
    case "Number":
      return { pattern: /^[+-]?(?:\d+\.?\d*|\.\d+)$/, message: "Enter a number." };
    case "Boolean":
      return { pattern: /^(?:true|false)$/i, message: "Enter true or false." };
    case "Identifier":
      return element === LyoTypes.Guid ? { pattern: /^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i, message: "Enter a GUID." } : null;
    default:
      return null;
  }
}

/** True when a picker change should replace the stored JSON with the new type's default (identity or list shape changed). */
export function shouldResetValue(fromType: string | null | undefined, toType: string | null | undefined): boolean {
  if (fromType === toType) return false;
  const from = lyoTypeFromName(fromType);
  const to = lyoTypeFromName(toType);
  if (from === LyoTypes.Unknown && to === LyoTypes.Unknown) return false;
  if ((from === LyoTypes.Enum && to === LyoTypes.Unknown) || (from === LyoTypes.Unknown && to === LyoTypes.Enum)) return false;
  return from !== to || collectionShape(fromType) !== collectionShape(toType);
}

export function toListKind(typeName: string | null | undefined): LyoListJsonKind {
  switch (elementInfo(typeName).category) {
    case "Integer":
    case "Number":
      return "Number";
    case "Boolean":
      return "Bool";
    default:
      return "String";
  }
}

const WIDE_KINDS = new Set(["JsonObject", "JsonArray", "Xml", "Collection", "Binary", "Formatter", "Regex"]);

/** True when the editor belongs under a row expander (JSON, XML, collections, binary, regex, formatter, unknown / custom). */
export function isWideEditor(typeName: string | null | undefined): boolean {
  const info = lyoTypeFromName(typeName);
  return info === LyoTypes.Unknown || WIDE_KINDS.has(info.editorKind);
}

/** Short preview of stored JSON for a collapsed value cell. */
export function valuePreview(json: string | null | undefined, maxChars = 48): string {
  const trimmed = json?.trim();
  if (!trimmed || trimmed === "null") return "—";
  return trimmed.length <= maxChars ? trimmed : `${trimmed.slice(0, maxChars)}…`;
}

export function defaultJsonFor(typeName: string | null | undefined): string {
  const info = lyoTypeFromName(typeName);
  return info === LyoTypes.Unknown ? "null" : info.defaultJson;
}

const GUID = /^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i;

/** Absolute URI or a relative path containing `/`; bare tokens such as `87gg` are rejected. Empty is valid. */
export function isValidUriText(value: string | null | undefined): boolean {
  if (!value?.trim()) return true;
  if (/^[a-z][a-z0-9+.-]*:\/\/\S+$/i.test(value) || /^(mailto|urn|data|file):\S+$/i.test(value)) return true;
  return value.includes("/") && !/\s/.test(value);
}

function isRegexPattern(pattern: string): boolean {
  try {
    new RegExp(pattern);
    return true;
  } catch {
    return false;
  }
}

/** True when `json` is empty / null or parses as a payload for the type (port of `LyoTypeInfo.TryValidateJson`, XML accepted as any string). */
export function validateTypedJson(typeName: string | null | undefined, json: string | null | undefined): boolean {
  if (!json?.trim() || json.trim() === "null") return true;
  let value: unknown;
  try {
    value = JSON.parse(json);
  } catch {
    return false;
  }
  const info = lyoTypeFromName(typeName);
  switch (info.editorKind) {
    case "Integer":
      return typeof value === "number" ? Number.isInteger(value) : typeof value === "string" && /^[+-]?\d+$/.test(value);
    case "Decimal":
      return typeof value === "number" || (typeof value === "string" && value.trim() !== "" && Number.isFinite(Number(value)));
    case "Boolean":
      return typeof value === "boolean";
    case "Guid":
      return typeof value === "string" && GUID.test(value);
    case "Uri":
      return typeof value === "string" && value.trim() !== "" && isValidUriText(value);
    case "Regex":
      return typeof value === "string" && isRegexPattern(value);
    case "DateTime":
    case "DateOnly":
    case "TimeOnly":
    case "Duration":
    case "Text":
    case "Xml":
    case "Binary":
    case "Formatter":
      return info === LyoTypes.Unknown || typeof value === "string";
    case "Enum":
      return typeof value === "string" || typeof value === "number";
    case "Collection":
    case "JsonArray":
      return Array.isArray(value);
    case "JsonObject":
      return info === LyoTypes.JsonNode || (value !== null && typeof value === "object" && !Array.isArray(value));
    default:
      return true;
  }
}
