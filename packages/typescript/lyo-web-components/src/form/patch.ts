import type { LyoFormInputKind } from "./fieldSize.js";

export type PropertyChange = {
  propertyName: string;
  originalValue: unknown;
  currentValue: unknown;
  hasChanged: boolean;
};

export type PatchRequest = {
  keys: unknown[];
  data: Record<string, unknown>;
};

/** Value equality for form values: dates by instant, arrays and plain objects by content, NaN equal to itself. */
export function valuesEqual(a: unknown, b: unknown): boolean {
  if (Object.is(a, b)) return true;
  if (a instanceof Date && b instanceof Date) return a.getTime() === b.getTime();
  if (Array.isArray(a) && Array.isArray(b)) return a.length === b.length && a.every((v, i) => valuesEqual(v, b[i]));
  if (a && b && typeof a === "object" && typeof b === "object" && !Array.isArray(a) && !Array.isArray(b)) {
    const ka = Object.keys(a);
    const kb = Object.keys(b);
    return ka.length === kb.length && ka.every((k) => valuesEqual((a as Record<string, unknown>)[k], (b as Record<string, unknown>)[k]));
  }
  return false;
}

export function propertyChange(propertyName: string, originalValue: unknown, currentValue: unknown): PropertyChange {
  return { propertyName, originalValue, currentValue, hasChanged: !valuesEqual(originalValue, currentValue) };
}

/** Patch body from changed properties; null when nothing changed. A scalar key is wrapped in an array. */
export function buildPatchRequest(changes: Record<string, PropertyChange>, key: unknown): PatchRequest | null {
  const entries = Object.values(changes).filter((c) => c.hasChanged);
  if (entries.length === 0) return null;
  const data: Record<string, unknown> = {};
  for (const e of entries) data[e.propertyName] = e.currentValue;
  return { keys: Array.isArray(key) ? key : [key], data };
}

export type LyoParsedFormValue = { value: unknown; invalid?: undefined } | { value?: undefined; invalid: true };

const GUID = /^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i;
const NUMBER = /^[+-]?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?$/;

/** Turns input text into the model value for a kind. Invalid text (bad GUID, non-integer, blank non-nullable number) is reported instead of committed. */
export function parseFormValue(kind: LyoFormInputKind, text: string, options: { nullable?: boolean } = {}): LyoParsedFormValue {
  const trimmed = text.trim();
  if (!trimmed && kind !== "string" && kind !== "richtext") return options.nullable ? { value: null } : { invalid: true };
  switch (kind) {
    case "integer":
      return /^[+-]?\d+$/.test(trimmed) ? { value: Number(trimmed) } : { invalid: true };
    case "number":
      return NUMBER.test(trimmed) ? { value: Number(trimmed) } : { invalid: true };
    case "guid":
      return GUID.test(trimmed) ? { value: trimmed.toLowerCase() } : { invalid: true };
    case "datetime": {
      const ms = Date.parse(trimmed.length === 16 ? `${trimmed}:00Z` : trimmed);
      return Number.isNaN(ms) ? { invalid: true } : { value: new Date(ms).toISOString() };
    }
    case "time":
      return { value: trimmed.length === 5 ? `${trimmed}:00` : trimmed };
    case "date":
      return { value: trimmed };
    default:
      return !text && options.nullable ? { value: null } : { value: text };
  }
}

/** Input text for a model value (`datetime-local` in UTC for date-times). */
export function formatFormValue(kind: LyoFormInputKind, value: unknown): string {
  if (value == null) return "";
  if (kind === "datetime") {
    const ms = value instanceof Date ? value.getTime() : Date.parse(String(value));
    return Number.isNaN(ms) ? String(value) : new Date(ms).toISOString().slice(0, 16);
  }
  return String(value);
}

export type LyoFlagOption = { value: number; label: string };

/** Options whose bits are set in a flags value. */
export function flagsSelected(value: number | null | undefined, options: readonly LyoFlagOption[]): number[] {
  const bits = value ?? 0;
  return options.filter((o) => o.value !== 0 && (bits & o.value) === o.value).map((o) => o.value);
}

export function flagsToggle(value: number | null | undefined, bit: number, on: boolean): number {
  return on ? (value ?? 0) | bit : (value ?? 0) & ~bit;
}
