import { LyoTypes, lyoTypeFromName } from "./lyoTypeCatalog.js";
import { chipValidation, defaultJsonFor, isValidUriText, type LyoListJsonKind } from "./lyoTypeUi.js";

export type LyoValueEditorMode = "Scalar" | "Collection" | "JsonTree";

/** Result of turning editor text into stored JSON: `json` (null means unset) or a validation `error` to show without committing. */
export type LyoScalarCommit = { json: string | null; error?: undefined } | { json?: undefined; error: string };

function formatElement(value: unknown): string {
  if (typeof value === "string") return value.trim();
  if (typeof value === "number") return String(value);
  if (typeof value === "boolean") return value ? "True" : "False";
  return "";
}

const RAW_NUMBER = /-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?/y;

/** Port of `ParameterListJson.Parse`: a JSON array as canonical strings (numbers keep their written form). Anything else yields an empty list. */
export function parseListJson(json: string | null | undefined): string[] {
  if (!json?.trim()) return [];
  let parsed: unknown;
  try {
    parsed = JSON.parse(json);
  } catch {
    return [];
  }
  if (!Array.isArray(parsed)) return [];
  const raws = rawNumbers(json);
  return parsed.map((v) => (typeof v === "number" ? (raws.shift() ?? String(v)) : formatElement(v))).filter((v) => v.length > 0);
}

function rawNumbers(json: string): string[] {
  const out: string[] = [];
  let inString = false;
  for (let i = 0; i < json.length; i++) {
    const c = json[i]!;
    if (inString) {
      if (c === "\\") i++;
      else if (c === '"') inString = false;
      continue;
    }
    if (c === '"') inString = true;
    else if (c === "-" || (c >= "0" && c <= "9")) {
      RAW_NUMBER.lastIndex = i;
      const m = RAW_NUMBER.exec(json);
      if (m) {
        out.push(m[0]);
        i += m[0].length - 1;
      }
    }
  }
  return out;
}

const NUMBER_TEXT = /^[+-]?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?$/;

/** Port of `ParameterListJson.Serialize`: chips as a JSON array in the kind's wire shape. Null when nothing parses. */
export function serializeListJson(values: readonly string[] | null | undefined, kind: LyoListJsonKind = "String"): string | null {
  const out: unknown[] = [];
  for (const raw of values ?? []) {
    const trimmed = raw.trim();
    if (!trimmed) continue;
    if (kind === "Number") {
      if (NUMBER_TEXT.test(trimmed)) out.push(Number(trimmed));
    } else if (kind === "Bool") {
      if (/^(true|false)$/i.test(trimmed)) out.push(trimmed.toLowerCase() === "true");
    } else out.push(trimmed);
  }
  return out.length === 0 ? null : JSON.stringify(out);
}

/** Which editor `LyoTypeValueInput` shows for a type. */
export function editorModeFor(typeName: string | null | undefined): LyoValueEditorMode {
  const info = lyoTypeFromName(typeName);
  if (info.editorKind === "Collection") return "Collection";
  if (info === LyoTypes.Unknown || info.editorKind === "JsonObject" || info.editorKind === "JsonArray" || info.editorKind === "Binary") return "JsonTree";
  return "Scalar";
}

function unwrapJsonString(json: string): string {
  try {
    const value: unknown = JSON.parse(json);
    return typeof value === "string" ? value : json.trim().replace(/^"+|"+$/g, "");
  } catch {
    return json.trim().replace(/^"+|"+$/g, "");
  }
}

/** Text for a scalar input (`datetime-local` value for DateTime kinds, in UTC). Blank JSON uses the type default unless unset is allowed. */
export function jsonToScalarText(typeName: string | null | undefined, json: string | null | undefined, allowUnset = false): string {
  if (!json?.trim()) {
    if (allowUnset) return "";
    json = defaultJsonFor(typeName);
  }
  let value: unknown;
  try {
    value = JSON.parse(json);
  } catch {
    return unwrapJsonString(json);
  }
  if (value == null) return "";
  if (typeof value !== "string") return String(value);
  if (lyoTypeFromName(typeName).editorKind !== "DateTime") return value;
  const ms = Date.parse(value);
  return Number.isNaN(ms) ? value : new Date(ms).toISOString().slice(0, 16);
}

const GUID = /^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i;
const DURATION = /^-?(?:\d+\.)?\d{1,2}:\d{2}(?::\d{2}(?:\.\d{1,7})?)?$/;

function withSeconds(time: string): string {
  return time.length === 5 ? `${time}:00` : time;
}

/** Turns editor text into stored JSON for scalar kinds, validating Guid / Uri / numeric / duration text first. */
export function scalarTextToJson(typeName: string | null | undefined, text: string, allowUnset = false): LyoScalarCommit {
  const trimmed = text.trim();
  const info = lyoTypeFromName(typeName);
  if (!trimmed && (allowUnset || info.editorKind !== "Text")) return { json: allowUnset ? null : "null" };
  switch (info.editorKind) {
    case "Integer":
      return /^[+-]?\d+$/.test(trimmed) ? { json: String(Number(trimmed)) } : { error: "Enter an integer." };
    case "Decimal":
      return NUMBER_TEXT.test(trimmed) ? { json: String(Number(trimmed)) } : { error: "Enter a number." };
    case "Boolean":
      return { json: String(trimmed.toLowerCase() === "true") };
    case "Guid":
      return GUID.test(trimmed) ? { json: JSON.stringify(trimmed.toLowerCase()) } : { error: "Enter a GUID." };
    case "Uri":
      return isValidUriText(trimmed) ? { json: JSON.stringify(trimmed) } : { error: "Enter an absolute URI or a path with /." };
    case "DateTime": {
      const local = trimmed.length === 10 ? `${trimmed}T00:00` : trimmed;
      return { json: JSON.stringify(`${local.slice(0, 16)}:00${info === LyoTypes.DateTimeOffset ? "+00:00" : "Z"}`) };
    }
    case "TimeOnly":
      return { json: JSON.stringify(withSeconds(trimmed)) };
    case "Duration":
      return DURATION.test(trimmed) ? { json: JSON.stringify(trimmed) } : { error: "Enter a duration like 1.02:03:04." };
    default:
      return { json: JSON.stringify(text) };
  }
}

/** Chips that fail the type's chip pattern (numeric / bool / GUID elements). */
export function invalidChips(typeName: string | null | undefined, chips: readonly string[]): string[] {
  const rule = chipValidation(typeName);
  return rule ? chips.filter((c) => !rule.pattern.test(c.trim())) : [];
}
