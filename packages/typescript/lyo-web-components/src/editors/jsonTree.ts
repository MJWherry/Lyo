export type JsonPathSegment = string | number;
export type JsonPath = readonly JsonPathSegment[];
export type JsonValueKind = "string" | "number" | "boolean" | "null" | "object" | "array";

export const ROOT_PATH_KEY = "$";

export type JsonTreeRow = {
  /** Stable path key (`$.meta.tags[0]`), also used for expanded / match sets. */
  id: string;
  path: JsonPath;
  /** Property name, `[i]` for array items, "" for the root. */
  key: string;
  depth: number;
  kind: JsonValueKind;
  value: unknown;
  expandable: boolean;
  expanded: boolean;
  /** True for object properties (keys are renameable); false for the root and array items. */
  keyEditable: boolean;
};

type JsonObject = Record<string, unknown>;

export function valueKind(value: unknown): JsonValueKind {
  if (value == null) return "null";
  if (Array.isArray(value)) return "array";
  switch (typeof value) {
    case "string":
      return "string";
    case "number":
    case "bigint":
      return "number";
    case "boolean":
      return "boolean";
    case "object":
      return "object";
    default:
      return "string";
  }
}

function isContainer(value: unknown): value is JsonObject | unknown[] {
  return value != null && typeof value === "object";
}

function childCount(value: unknown): number {
  if (Array.isArray(value)) return value.length;
  return isContainer(value) ? Object.keys(value).length : 0;
}

const IDENTIFIER = /^[A-Za-z_$][\w$]*$/;

export function pathKey(path: JsonPath): string {
  let key = ROOT_PATH_KEY;
  for (const segment of path) {
    if (typeof segment === "number") key += `[${segment}]`;
    else key += IDENTIFIER.test(segment) ? `.${segment}` : `[${JSON.stringify(segment)}]`;
  }
  return key;
}

/** Keys of every container above `path`, root first (what must be expanded to reveal it). */
export function ancestorKeys(path: JsonPath): string[] {
  return path.map((_, i) => pathKey(path.slice(0, i)));
}

function entriesOf(value: unknown): [JsonPathSegment, unknown][] {
  if (Array.isArray(value)) return value.map((v, i) => [i, v]);
  return isContainer(value) ? Object.entries(value) : [];
}

/** Rows in document order; children of collapsed containers are omitted. */
export function flattenVisible(root: unknown, expanded: ReadonlySet<string>): JsonTreeRow[] {
  const rows: JsonTreeRow[] = [];
  const walk = (value: unknown, path: JsonPath, key: string, keyEditable: boolean) => {
    const id = pathKey(path);
    const expandable = childCount(value) > 0;
    const isOpen = expandable && expanded.has(id);
    rows.push({ id, path, key, depth: path.length, kind: valueKind(value), value, expandable, expanded: isOpen, keyEditable });
    if (!isOpen) return;
    for (const [segment, child] of entriesOf(value)) {
      walk(child, [...path, segment], typeof segment === "number" ? `[${segment}]` : segment, typeof segment === "string");
    }
  };
  walk(root, [], "", false);
  return rows;
}

export function expandAll(root: unknown): Set<string> {
  const keys = new Set<string>();
  const walk = (value: unknown, path: JsonPath) => {
    if (childCount(value) === 0) return;
    keys.add(pathKey(path));
    for (const [segment, child] of entriesOf(value)) walk(child, [...path, segment]);
  };
  walk(root, []);
  return keys;
}

export function collapseAll(): Set<string> {
  return new Set([ROOT_PATH_KEY]);
}

/** Row text shown after the key: quoted strings, JSON for other primitives, the count badge for containers. */
export function displayValue(value: unknown): string {
  const summary = containerSummary(value);
  if (summary) return summary;
  if (value == null) return "null";
  return typeof value === "string" ? `"${value}"` : String(value);
}

export function containerSummary(value: unknown): string | null {
  if (Array.isArray(value)) return `[…] ${value.length}`;
  if (isContainer(value)) return `{…} ${Object.keys(value).length}`;
  return null;
}

/** Path keys of every row whose key or primitive value contains `text` (case-insensitive), in document order. */
export function searchMatches(root: unknown, text: string): string[] {
  const q = text.trim().toLowerCase();
  if (!q) return [];
  const matches: string[] = [];
  const walk = (value: unknown, path: JsonPath, key: string) => {
    const valueText = isContainer(value) ? "" : displayValue(value);
    if (key.toLowerCase().includes(q) || valueText.toLowerCase().includes(q)) matches.push(pathKey(path));
    for (const [segment, child] of entriesOf(value)) walk(child, [...path, segment], typeof segment === "string" ? segment : "");
  };
  walk(root, [], "");
  return matches;
}

/** Next match index, wrapping. From no selection (-1), forward starts at the first and back at the last. -1 when there are no matches. */
export function stepMatch(count: number, current: number, delta: 1 | -1): number {
  if (count <= 0) return -1;
  if (current < 0) return delta > 0 ? 0 : count - 1;
  return (((current + delta) % count) + count) % count;
}

export function getAt(root: unknown, path: JsonPath): unknown {
  let current = root;
  for (const segment of path) {
    if (!isContainer(current)) return undefined;
    current = (current as Record<JsonPathSegment, unknown>)[segment];
  }
  return current;
}

function withChild(container: unknown, segment: JsonPathSegment, child: unknown): unknown {
  if (Array.isArray(container)) {
    const copy = [...container];
    copy[segment as number] = child;
    return copy;
  }
  return { ...(container as JsonObject), [segment]: child };
}

function updateAt(root: unknown, path: JsonPath, update: (value: unknown) => unknown): unknown {
  if (path.length === 0) return update(root);
  const [head, ...rest] = path;
  if (!isContainer(root)) return root;
  return withChild(root, head!, updateAt((root as Record<JsonPathSegment, unknown>)[head!], rest, update));
}

export function setAt(root: unknown, path: JsonPath, value: unknown): unknown {
  return updateAt(root, path, () => value);
}

export function removeAt(root: unknown, path: JsonPath): unknown {
  if (path.length === 0) return root;
  const last = path[path.length - 1]!;
  return updateAt(root, path.slice(0, -1), (parent) => {
    if (Array.isArray(parent)) return parent.filter((_, i) => i !== last);
    if (!isContainer(parent)) return parent;
    const { [last as string]: _removed, ...rest } = parent as JsonObject;
    return rest;
  });
}

function uniqueKey(obj: JsonObject, requested: string, excluding?: string): string {
  const base = requested.trim() || "newField";
  let candidate = base;
  for (let n = 1; candidate in obj && candidate !== excluding; n++) candidate = `${base}_${n}`;
  return candidate;
}

function defaultFromExemplar(exemplar: unknown): unknown {
  if (Array.isArray(exemplar)) {
    const first = exemplar.find((x) => x != null);
    return first === undefined ? [] : [defaultFromExemplar(first)];
  }
  if (isContainer(exemplar)) {
    return Object.fromEntries(Object.entries(exemplar).map(([k, v]) => [k, v == null ? null : defaultFromExemplar(v)]));
  }
  switch (valueKind(exemplar)) {
    case "string":
      return "";
    case "number":
      return 0;
    case "boolean":
      return false;
    default:
      return null;
  }
}

/** Objects gain a unique `newField: null`; arrays gain a default shaped like their first non-null item (null when empty). */
export function addChild(root: unknown, path: JsonPath): unknown {
  return updateAt(root, path, (container) => {
    if (Array.isArray(container)) {
      const exemplar = container.find((x) => x != null);
      return [...container, exemplar === undefined ? null : defaultFromExemplar(exemplar)];
    }
    if (!isContainer(container)) return container;
    return { ...container, [uniqueKey(container as JsonObject, "newField")]: null };
  });
}

/** Renames an object property in place (key order kept); collisions get `_n` suffixes. */
export function renameKey(root: unknown, path: JsonPath, newKey: string): unknown {
  if (path.length === 0) return root;
  const oldKey = path[path.length - 1];
  if (typeof oldKey !== "string") return root;
  return updateAt(root, path.slice(0, -1), (parent) => {
    if (!isContainer(parent) || Array.isArray(parent)) return parent;
    const obj = parent as JsonObject;
    const target = uniqueKey(obj, newKey, oldKey);
    return Object.fromEntries(Object.entries(obj).map(([k, v]) => [k === oldKey ? target : k, v]));
  });
}

/** Clipboard text for the value at `path`: raw strings, pretty JSON for everything else. */
export function copyText(root: unknown, path: JsonPath): string {
  const value = getAt(root, path);
  if (typeof value === "string") return value;
  return JSON.stringify(value ?? null, null, 2);
}

/** Inline-edit input to a value: `null` (any case), JSON literals, else the raw string. */
export function parseEditValue(input: string): unknown {
  const trimmed = input.trim();
  if (trimmed.toLowerCase() === "null") return null;
  try {
    return JSON.parse(trimmed);
  } catch {
    return input;
  }
}

/** Initial text for an inline value editor. */
export function editText(value: unknown): string {
  if (typeof value === "string") return value;
  return JSON.stringify(value ?? null);
}
