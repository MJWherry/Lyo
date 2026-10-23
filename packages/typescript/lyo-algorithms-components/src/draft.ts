import type {AlgorithmKind, AlgorithmPreviewRequest, AlgorithmRevisionRequest} from "lyo-algorithms-api-client";

export const ALIASES = [
  "bool", "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong", "float", "double", "decimal", "string", "datetime", "datetimeoffset", "timespan",
] as const;

const NUMERIC = new Set(["byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong", "float", "double", "decimal"]);

const KIND_PASCAL: Record<AlgorithmKind, string> = {
  expression: "Expression",
  named: "Named",
  discreteMap: "DiscreteMap",
  rangeBand: "RangeBand",
  linear: "Linear",
  composite: "Composite",
};

export type AlgorithmValueKind = "number" | "switch" | "text" | "date" | "duration" | "list";
export type MissingPolicy = "Zero" | "Reject";
export type RangePolicy = "Zero" | "Nearest" | "Reject";
export type Combiner = "Sum" | "WeightedSum" | "Formula";

export interface AlgorithmTypeRow {
  name: string;
  alias: string;
}

export interface AlgorithmMapEntry {
  key: string;
  value: string;
}

export interface AlgorithmBandEntry {
  min: string;
  max: string;
  value: string;
}

export interface AlgorithmChildDraft {
  key?: string;
  ref?: string;
  kind?: AlgorithmKind;
  weight: string;
  inline?: AlgorithmDraft;
}

export interface AlgorithmContextRow {
  name: string;
  alias: string;
  text: string;
}

export interface AlgorithmDraft {
  key: string;
  name?: string;
  description?: string;
  kind: AlgorithmKind;
  source: string;
  result: string;
  inputType?: string;
  parameters: AlgorithmTypeRow[];
  calls: AlgorithmTypeRow[];
  input: string;
  missing: MissingPolicy;
  outOfRange: RangePolicy;
  missingValue?: string;
  entries: AlgorithmMapEntry[];
  bands: AlgorithmBandEntry[];
  inputMin: string;
  inputMax: string;
  pointsMin: string;
  pointsMax: string;
  clamp: boolean;
  combiner: Combiner;
  formula?: string;
  children: AlgorithmChildDraft[];
}

export interface WorkbenchState {
  draft: AlgorithmDraft;
  context: AlgorithmContextRow[];
}

/** localStorage key used by the workbench. The `lyo-web:` prefix is added by `clientStore`. */
export const ALGORITHM_WORKBENCH_KEY = "algorithm-workbench";

export function emptyDraft(): AlgorithmDraft {
  return {
    key: "preview",
    kind: "expression",
    source: "",
    result: "double",
    parameters: [],
    calls: [],
    input: "",
    missing: "Zero",
    outOfRange: "Zero",
    entries: [],
    bands: [],
    inputMin: "0",
    inputMax: "1",
    pointsMin: "0",
    pointsMax: "1",
    clamp: false,
    combiner: "Sum",
    children: [],
  };
}

export function isIdentifier(name: string | undefined | null): boolean {
  if (!name) return false;
  if (!(isLetter(name[0]!) || name[0] === "_")) return false;
  for (let i = 1; i < name.length; i++) {
    const char = name[i]!;
    if (!(isLetter(char) || isDigit(char) || char === "_")) return false;
  }
  return true;
}

export function defaultAlias(name: string): string {
  return name.endsWith("_key") ? "string" : "double";
}

export function editorKind(alias: string | undefined): AlgorithmValueKind {
  const text = (alias ?? "double").trim();
  if (text.endsWith("[]")) return "list";
  if (text.toLowerCase() === "bool") return "switch";
  if (text.toLowerCase() === "datetime" || text.toLowerCase() === "datetimeoffset") return "date";
  if (text.toLowerCase() === "timespan") return "duration";
  if (NUMERIC.has(text.toLowerCase())) return "number";
  return "text";
}

export function isNumericAlias(alias: string | undefined | null): boolean {
  return !!alias && NUMERIC.has(alias.trim().toLowerCase());
}

const CLR_NAMES: Record<string, string> = {
  bool: "System.Boolean",
  byte: "System.Byte",
  sbyte: "System.SByte",
  short: "System.Int16",
  ushort: "System.UInt16",
  int: "System.Int32",
  uint: "System.UInt32",
  long: "System.Int64",
  ulong: "System.UInt64",
  float: "System.Single",
  double: "System.Double",
  decimal: "System.Decimal",
  string: "System.String",
  datetime: "System.DateTime",
  datetimeoffset: "System.DateTimeOffset",
  timespan: "System.TimeSpan",
};

/** CLR type name for a value field. An unknown alias, including a host enum, is a string. `T[]` is a one-level array. */
export function clrTypeName(alias: string | null | undefined): string {
  const text = (alias ?? "double").trim();
  const list = text.endsWith("[]");
  const element = (list ? text.slice(0, -2) : text).toLowerCase();
  const full = CLR_NAMES[element] ?? "System.String";
  return list ? `${full}[]` : full;
}

/** JSON for `LyoParameterValueField` from the author text stored on a context row. */
export function fieldJson(alias: string, text: string): string | null {
  const value = text.trim();
  if (!value) return null;
  if (alias.trim().endsWith("[]")) return value.startsWith("[") ? value : "[]";
  if (alias.trim().toLowerCase() === "bool") return value.toLowerCase() === "true" ? "true" : "false";
  if (isNumericAlias(alias)) return value;
  return JSON.stringify(text);
}

/** Author text from a value field's JSON. */
export function textFromField(alias: string, json: string | null): string {
  if (!json || json === "null") return "";
  if (alias.trim().endsWith("[]") || isNumericAlias(alias) || alias.trim().toLowerCase() === "bool") return json;
  try {
    const parsed = JSON.parse(json) as unknown;
    return typeof parsed === "string" ? parsed : json;
  } catch {
    return json;
  }
}

export function insert(source: string | undefined, start: number, end: number, token: string): string {
  const text = source ?? "";
  const from = clamp(start, 0, text.length);
  const to = clamp(end, from, text.length);
  return text.slice(0, from) + token + text.slice(to);
}

export function issues(draft: AlgorithmDraft): string[] {
  const found: string[] = [];
  if (draft.result?.endsWith("[][]")) found.push("Nested arrays are not supported.");
  if ((draft.kind === "expression" || draft.kind === "named") && !draft.source.trim()) found.push("Source is required.");
  if ((draft.kind === "linear" || draft.kind === "rangeBand") && draft.inputType?.trim() && !isNumericAlias(draft.inputType))
    found.push("Input type must be numeric.");
  if (draft.kind === "linear" && !isNumericAlias(draft.result)) found.push("A linear result must be numeric.");
  if (draft.kind === "composite" && draft.combiner !== "Formula" && !isNumericAlias(draft.result))
    found.push("Sum and weighted sum results must be numeric.");
  if (needsMissing(draft) && !draft.missingValue?.trim())
    found.push("A non-numeric result needs missingValue when the missing policy is zero.");
  if (draft.kind === "composite") {
    if (draft.children.length === 0) found.push("Composite children are required.");
    if (draft.combiner === "Formula" && !draft.formula?.trim()) found.push("Composite formula is required.");
    for (const child of draft.children) childIssues(child, found);
  }
  for (const row of draft.parameters) {
    if (!isIdentifier(row.name)) found.push(`'${row.name}' is not an expression identifier.`);
  }
  return found;
}

export function toPayload(draft: AlgorithmDraft): string | null {
  const body: Record<string, unknown> = {};
  let wrote = writeTypes(body, draft);
  wrote = writeBody(body, draft) || wrote;
  return wrote ? JSON.stringify(body) : null;
}

export function toRevision(draft: AlgorithmDraft): AlgorithmRevisionRequest {
  return {kind: draft.kind, source: draft.source ?? "", payload: toPayload(draft), name: draft.name, description: draft.description};
}

export function toPreview(draft: AlgorithmDraft, context: AlgorithmContextRow[]): AlgorithmPreviewRequest {
  return {key: draft.key, kind: draft.kind, source: draft.source, payload: toPayload(draft), context: contextObject(context)};
}

export function contextObject(rows: AlgorithmContextRow[]): Record<string, unknown> {
  const context: Record<string, unknown> = {};
  for (const row of rows) {
    if (!isIdentifier(row.name)) continue;
    context[row.name] = literal(row.alias, row.text);
  }
  return context;
}

export function setParameter(draft: AlgorithmDraft, name: string, alias: string): AlgorithmDraft {
  const parameters = draft.parameters.map((row) => ({...row}));
  const existing = parameters.find((row) => row.name === name);
  if (existing) existing.alias = alias;
  else parameters.push({name, alias});
  return {...draft, parameters};
}

/** Bag alias changes write `parameters`. A default alias does not add a new row. */
export function syncParameters(draft: AlgorithmDraft, rows: AlgorithmContextRow[]): AlgorithmDraft {
  let next = draft;
  for (const row of rows) {
    if (!isIdentifier(row.name)) continue;
    const declared = next.parameters.find((item) => item.name === row.name);
    if (declared) next = setParameter(next, row.name, row.alias);
    else if (row.alias.toLowerCase() !== defaultAlias(row.name).toLowerCase()) next = setParameter(next, row.name, row.alias);
  }
  return next;
}

export function fromPayload(kind: AlgorithmKind, source: string, payload: string | null | undefined): AlgorithmDraft {
  const draft = emptyDraft();
  draft.kind = kind;
  draft.source = source ?? "";
  if (!payload?.trim()) return draft;
  const root = JSON.parse(payload) as Record<string, unknown>;
  read(draft, root);
  return draft;
}

export function readWorkbenchState(raw: string | null | undefined): WorkbenchState | null {
  if (!raw?.trim()) return null;
  try {
    const state = JSON.parse(raw) as Partial<WorkbenchState> | null;
    if (!state?.draft) return null;
    return {draft: {...emptyDraft(), ...state.draft}, context: state.context ?? []};
  } catch {
    return null;
  }
}

function writeTypes(body: Record<string, unknown>, draft: AlgorithmDraft): boolean {
  let wrote = false;
  if (draft.result?.trim() && draft.result.toLowerCase() !== "double") {
    body.result = draft.result.trim();
    wrote = true;
  }
  if (draft.inputType?.trim()) {
    body.inputType = draft.inputType.trim();
    wrote = true;
  }
  const parameters = typeMap(draft.parameters);
  if (parameters) {
    body.parameters = parameters;
    wrote = true;
  }
  const calls = typeMap(draft.calls);
  if (calls) {
    body.calls = calls;
    wrote = true;
  }
  return wrote;
}

function writeBody(body: Record<string, unknown>, draft: AlgorithmDraft): boolean {
  if (draft.kind === "discreteMap") {
    body.input = draft.input;
    body.missing = draft.missing;
    if (draft.missingValue?.trim()) body.missingValue = literal(draft.result, draft.missingValue);
    body.entries = draft.entries.map((entry) => ({key: entry.key, points: literal(draft.result, entry.value)}));
    return true;
  }
  if (draft.kind === "rangeBand") {
    body.input = draft.input;
    body.outOfRange = draft.outOfRange;
    if (draft.missingValue?.trim()) body.missingValue = literal(draft.result, draft.missingValue);
    const inputAlias = draft.inputType || "double";
    body.bands = draft.bands.map((band) => ({
      min: literal(inputAlias, band.min),
      max: literal(inputAlias, band.max),
      points: literal(draft.result, band.value),
    }));
    return true;
  }
  if (draft.kind === "linear") {
    const inputAlias = draft.inputType || "double";
    body.input = draft.input;
    body.inputMin = literal(inputAlias, draft.inputMin);
    body.inputMax = literal(inputAlias, draft.inputMax);
    body.pointsMin = literal(draft.result, draft.pointsMin);
    body.pointsMax = literal(draft.result, draft.pointsMax);
    if (draft.clamp) body.clamp = true;
    return true;
  }
  if (draft.kind === "composite") {
    body.combiner = draft.combiner;
    if (draft.formula?.trim()) body.formula = draft.formula;
    body.children = draft.children.map(childObject);
    return true;
  }
  return false;
}

function childObject(child: AlgorithmChildDraft): Record<string, unknown> {
  const body: Record<string, unknown> = {};
  if (child.key?.trim()) body.key = child.key;
  if (child.ref?.trim()) body.ref = child.ref;
  if (child.kind) {
    body.kind = KIND_PASCAL[child.kind];
    const payload = child.inline ? toPayload(child.inline) : null;
    body.payload = payload ? JSON.parse(payload) : {};
  }
  if (child.weight?.trim() && child.weight.trim() !== "1") body.weight = literal("double", child.weight);
  return body;
}

function literal(alias: string | undefined, text: string | undefined): unknown {
  const name = (alias ?? "double").trim();
  const value = text ?? "";
  if (name.endsWith("[]")) {
    if (value.trimStart().startsWith("[")) {
      try {
        const parsed = JSON.parse(value) as unknown;
        if (Array.isArray(parsed)) return parsed;
      } catch {
        return [];
      }
    }
    return [];
  }
  if (name.toLowerCase() === "bool" && (value.toLowerCase() === "true" || value.toLowerCase() === "false")) return value.toLowerCase() === "true";
  if (NUMERIC.has(name.toLowerCase())) {
    const number = Number(value);
    if (value.trim() !== "" && Number.isFinite(number)) return number;
  }
  return value;
}

function typeMap(rows: AlgorithmTypeRow[]): Record<string, string> | null {
  const usable = rows.filter((row) => row.name.trim());
  if (usable.length === 0) return null;
  const map: Record<string, string> = {};
  for (const row of usable) map[row.name.trim()] = row.alias.trim() || "double";
  return map;
}

function needsMissing(draft: AlgorithmDraft): boolean {
  if (isNumericAlias(draft.result)) return false;
  if (draft.kind === "discreteMap" && draft.missing === "Zero") return true;
  return draft.kind === "rangeBand" && draft.outOfRange === "Zero";
}

function childIssues(child: AlgorithmChildDraft, found: string[]): void {
  const hasRef = !!child.ref?.trim();
  const hasInline = child.kind != null;
  if (hasRef === hasInline) found.push(hasRef ? "Composite child needs a ref or an inline kind, not both." : "Composite child needs a ref or an inline kind.");
  if (hasInline && !isIdentifier(child.key)) found.push("Inline composite child key must be an identifier.");
}

function read(draft: AlgorithmDraft, root: Record<string, unknown>): void {
  if (typeof root.result === "string") draft.result = root.result;
  if (typeof root.inputType === "string") draft.inputType = root.inputType;
  draft.parameters = readMap(root.parameters);
  draft.calls = readMap(root.calls);
  if (typeof root.input === "string") draft.input = root.input;
  if (root.missing === "Zero" || root.missing === "Reject") draft.missing = root.missing;
  if (root.outOfRange === "Zero" || root.outOfRange === "Nearest" || root.outOfRange === "Reject") draft.outOfRange = root.outOfRange;
  if ("missingValue" in root) draft.missingValue = textOf(root.missingValue);
  if (Array.isArray(root.entries)) {
    draft.entries = root.entries.map((entry) => {
      const row = entry as Record<string, unknown>;
      return {key: typeof row.key === "string" ? row.key : "", value: textOf(row.points)};
    });
  }
  if (Array.isArray(root.bands)) {
    draft.bands = root.bands.map((band) => {
      const row = band as Record<string, unknown>;
      return {min: textOf(row.min) || "0", max: textOf(row.max) || "1", value: textOf(row.points)};
    });
  }
  if ("inputMin" in root) draft.inputMin = textOf(root.inputMin);
  if ("inputMax" in root) draft.inputMax = textOf(root.inputMax);
  if ("pointsMin" in root) draft.pointsMin = textOf(root.pointsMin);
  if ("pointsMax" in root) draft.pointsMax = textOf(root.pointsMax);
  if (typeof root.clamp === "boolean") draft.clamp = root.clamp;
  if (root.combiner === "Sum" || root.combiner === "WeightedSum" || root.combiner === "Formula") draft.combiner = root.combiner;
  if (typeof root.formula === "string") draft.formula = root.formula;
  if (Array.isArray(root.children)) draft.children = root.children.map((child) => readChild(child as Record<string, unknown>));
}

function readChild(child: Record<string, unknown>): AlgorithmChildDraft {
  const draft: AlgorithmChildDraft = {weight: "1"};
  if (typeof child.key === "string") draft.key = child.key;
  if (typeof child.ref === "string") draft.ref = child.ref;
  const kind = kindFromPascal(child.kind);
  if (kind) draft.kind = kind;
  if ("weight" in child) draft.weight = textOf(child.weight);
  if (child.payload && typeof child.payload === "object") {
    draft.inline = emptyDraft();
    draft.inline.kind = kind ?? "discreteMap";
    read(draft.inline, child.payload as Record<string, unknown>);
  }
  return draft;
}

function readMap(value: unknown): AlgorithmTypeRow[] {
  if (!value || typeof value !== "object" || Array.isArray(value)) return [];
  return Object.entries(value as Record<string, unknown>).map(([name, alias]) => ({name, alias: typeof alias === "string" ? alias : "double"}));
}

function kindFromPascal(value: unknown): AlgorithmKind | undefined {
  if (typeof value !== "string") return undefined;
  const match = (Object.entries(KIND_PASCAL) as Array<[AlgorithmKind, string]>).find(([, pascal]) => pascal.toLowerCase() === value.toLowerCase());
  return match?.[0];
}

function textOf(value: unknown): string {
  if (value == null) return "";
  if (typeof value === "string") return value;
  if (typeof value === "boolean") return value ? "true" : "false";
  return JSON.stringify(value);
}

function isLetter(char: string): boolean {
  return (char >= "A" && char <= "Z") || (char >= "a" && char <= "z");
}

function isDigit(char: string): boolean {
  return char >= "0" && char <= "9";
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}
