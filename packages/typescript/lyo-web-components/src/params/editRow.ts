import { LyoTypes, lyoTypeFromName, isLyoCollection } from "../types/lyoTypeCatalog.js";
import { defaultJsonFor, shouldResetValue, toListKind, validateTypedJson } from "../types/lyoTypeUi.js";
import { parseListJson } from "../types/lyoTypeValue.js";
import { allowedValuesFromOptions, optionsFromAllowedValues, optionsKindOf, type ParameterOptionsKind } from "./paramOptions.js";

const MASK = "***";

export type LyoParameterDefaultKind = "Literal" | "Expression";

/** Editable parameter row shared by job definitions, schedules, and report definitions. Hosts map their DTOs in and out. */
export type LyoParameterEditRow = {
  /** Null until the host persists a new row. */
  id: string | null;
  key: string;
  /** Stored type full name for `value`. */
  type: string;
  /** JSON payload; the API mask `***` never lands here. */
  value: string | null;
  description: string | null;
  required: boolean;
  /** Schedule overrides only. */
  enabled: boolean;
  isNew: boolean;
  isEncrypted: boolean;
  /** Base64 ciphertext; empty string means encrypt-at-rest without a default. */
  encryptedValue: string | null;
  /** JSON array for server validation / simple select. */
  allowedValues: string | null;
  /** Options JSON (static, root query, or sproc). */
  options: string | null;
  order: number;
  defaultKind: LyoParameterDefaultKind;
  /** Template producing the default when `defaultKind` is Expression. */
  defaultTemplate: string | null;
};

export function newEditRow(patch: Partial<LyoParameterEditRow> = {}): LyoParameterEditRow {
  return {
    id: null,
    key: "",
    type: LyoTypes.String.fullName,
    value: null,
    description: null,
    required: false,
    enabled: true,
    isNew: true,
    isEncrypted: false,
    encryptedValue: null,
    allowedValues: null,
    options: null,
    order: 0,
    defaultKind: "Literal",
    defaultTemplate: null,
    ...patch,
  };
}

export type LyoParameterRowDto = {
  id: string;
  key: string;
  type: string;
  value?: string | null;
  description?: string | null;
  required?: boolean;
  encryptedValue?: string | null;
  allowedValues?: string | null;
  options?: string | null;
  order?: number;
  enabled?: boolean;
  defaultKind?: LyoParameterDefaultKind;
  defaultTemplate?: string | null;
};

/** Row from an API response; `***` marks an encrypted value, not plaintext. */
export function editRowFromMasked(dto: LyoParameterRowDto): LyoParameterEditRow {
  const masked = dto.value === MASK;
  return newEditRow({
    id: dto.id,
    key: dto.key,
    type: dto.type,
    value: masked ? null : (dto.value ?? null),
    description: dto.description ?? null,
    required: dto.required ?? false,
    enabled: dto.enabled ?? true,
    isNew: false,
    isEncrypted: masked || Boolean(dto.encryptedValue),
    encryptedValue: dto.encryptedValue ?? null,
    allowedValues: dto.allowedValues ?? null,
    options: dto.options ?? null,
    order: dto.order ?? 0,
    defaultKind: dto.defaultKind ?? "Literal",
    defaultTemplate: dto.defaultTemplate ?? null,
  });
}

/** Row as it should be persisted: no mask value, no template on literal defaults, an encryption marker only when encrypted. */
export function rowForSave(row: LyoParameterEditRow): LyoParameterEditRow {
  return {
    ...row,
    value: row.value === MASK ? null : row.value,
    defaultTemplate: row.defaultKind === "Expression" ? row.defaultTemplate : null,
    encryptedValue: row.isEncrypted ? (row.encryptedValue ?? "") : null,
  };
}

export type LyoParameterCreated = { id: string; options?: string | null; allowedValues?: string | null; value?: string | null };

export function applyCreated(row: LyoParameterEditRow, created: LyoParameterCreated): LyoParameterEditRow {
  return {
    ...row,
    id: created.id,
    isNew: false,
    options: created.options ?? null,
    allowedValues: created.allowedValues ?? null,
    value: created.value === MASK ? null : (created.value ?? null),
  };
}

/** Explicit Options, else static options built from AllowedValues. */
export function effectiveOptions(row: Pick<LyoParameterEditRow, "options" | "allowedValues">): string | null {
  return row.options?.trim() ? row.options : optionsFromAllowedValues(row.allowedValues);
}

export function effectiveOptionsKind(row: Pick<LyoParameterEditRow, "options" | "allowedValues">): ParameterOptionsKind | null {
  return optionsKindOf(effectiveOptions(row));
}

function findInherited(key: string, inheritFrom: readonly LyoParameterEditRow[] | undefined) {
  return inheritFrom?.find((p) => p.key.toLowerCase() === key.toLowerCase());
}

function applyInherit(row: LyoParameterEditRow, inheritFrom: readonly LyoParameterEditRow[] | undefined): LyoParameterEditRow {
  const src = findInherited(row.key, inheritFrom);
  if (!src) return row;
  return { ...row, type: src.type, options: src.options, allowedValues: src.allowedValues, description: row.description?.trim() ? row.description : src.description };
}

/** Appends a new row: the first unused inherited key (with its type and options) or `ParamN`, ordered after the rest. */
export function addParameterRow(
  rows: readonly LyoParameterEditRow[],
  options: { inheritFrom?: readonly LyoParameterEditRow[]; defaultRequired?: boolean } = {}
): LyoParameterEditRow[] {
  const used = new Set(rows.map((r) => r.key.toLowerCase()));
  const next = options.inheritFrom?.find((p) => !used.has(p.key.toLowerCase()));
  const row = newEditRow({
    key: next?.key ?? `Param${rows.length + 1}`,
    type: next?.type ?? LyoTypes.String.fullName,
    required: options.defaultRequired ?? false,
    order: rows.length === 0 ? 0 : Math.max(...rows.map((r) => r.order)) + 1,
  });
  return [...rows, applyInherit(row, options.inheritFrom)];
}

export function setRowKey(row: LyoParameterEditRow, key: string | null | undefined, inheritFrom?: readonly LyoParameterEditRow[]): LyoParameterEditRow {
  return applyInherit({ ...row, key: key ?? "" }, inheritFrom);
}

/** Changes the type and clears the value when the old payload no longer fits. */
export function setRowType(row: LyoParameterEditRow, type: string): LyoParameterEditRow {
  if (row.type === type) return row;
  return { ...row, type, value: shouldResetValue(row.type, type) ? null : row.value };
}

export function setRowEncrypt(row: LyoParameterEditRow, encrypt: boolean): LyoParameterEditRow {
  return { ...row, isEncrypted: encrypt, encryptedValue: encrypt ? (row.encryptedValue ?? "") : null };
}

/** Sets Options and keeps AllowedValues in step with static keys (typed by the row's list kind). */
export function setRowOptions(row: LyoParameterEditRow, options: string | null): LyoParameterEditRow {
  return { ...row, options, allowedValues: allowedValuesFromOptions(options, toListKind(row.type)) };
}

function reindex(rows: LyoParameterEditRow[]): LyoParameterEditRow[] {
  return rows.map((r, i) => (r.order === i ? r : { ...r, order: i }));
}

/** Moves a row and renumbers `order`. Out-of-range and no-op moves return the input list. */
export function moveParameterRow(rows: readonly LyoParameterEditRow[], from: number, to: number): readonly LyoParameterEditRow[] {
  if (from < 0 || from >= rows.length || to < 0 || to >= rows.length || from === to) return rows;
  const next = [...rows];
  const [item] = next.splice(from, 1);
  next.splice(to, 0, item!);
  return reindex(next);
}

export function deleteParameterRow(rows: readonly LyoParameterEditRow[], row: LyoParameterEditRow): LyoParameterEditRow[] {
  return reindex(rows.filter((r) => r !== row));
}

/** Target index for a drag from `from` dropped on `drop` (the dragged row leaves the list first). */
export function dropInsertIndex(from: number, drop: number): number {
  return drop > from ? drop - 1 : drop;
}

/** Row index to messages: blank key, duplicate key (case-insensitive), literal value that does not match the type. */
export function editRowErrors(rows: readonly LyoParameterEditRow[]): Map<number, string[]> {
  const counts = new Map<string, number>();
  for (const r of rows) if (r.key.trim()) counts.set(r.key.trim().toLowerCase(), (counts.get(r.key.trim().toLowerCase()) ?? 0) + 1);
  const errors = new Map<number, string[]>();
  rows.forEach((r, i) => {
    const messages: string[] = [];
    const key = r.key.trim();
    if (!key) messages.push("Key is required.");
    else if ((counts.get(key.toLowerCase()) ?? 0) > 1) messages.push(`Duplicate key '${key}'.`);
    if (r.defaultKind === "Literal" && !validateTypedJson(r.type, r.value)) messages.push(`Value is not a valid ${r.type}.`);
    if (messages.length) errors.set(i, messages);
  });
  return errors;
}

export type LyoParameterSaveHandlers = {
  remove: (id: string) => Promise<void>;
  /** Returns the created row's server id (and normalized columns), or null when the API returned nothing. */
  create: (row: LyoParameterEditRow) => Promise<LyoParameterCreated | null>;
  update: (row: LyoParameterEditRow) => Promise<void>;
};

/**
 * Reconciles editor rows against the ids that were loaded: deletes first (so a key can be replaced without a uniqueness clash), then creates, then updates. Returns
 * the rows with created ids stamped, so saving twice does not duplicate.
 */
export async function saveParameterRows(
  rows: readonly LyoParameterEditRow[],
  loadedIds: Iterable<string>,
  handlers: LyoParameterSaveHandlers
): Promise<LyoParameterEditRow[]> {
  const present = new Set(rows.map((r) => r.id));
  for (const id of loadedIds) if (!present.has(id)) await handlers.remove(id);
  const out: LyoParameterEditRow[] = [];
  for (const row of rows) {
    if (!row.isNew) {
      out.push(row);
      continue;
    }
    const created = await handlers.create(rowForSave(row));
    out.push(created ? applyCreated(row, created) : row);
  }
  for (const row of rows) if (!row.isNew && row.id) await handlers.update(rowForSave(row));
  return out;
}

/** Definition fields a run dialog needs (job and report parameter DTOs both fit). */
export type LyoParameterDefinition = {
  key: string;
  type: string;
  value?: string | null;
  required?: boolean;
  description?: string | null;
  options?: string | null;
  allowedValues?: string | null;
  defaultKind?: LyoParameterDefaultKind;
  defaultTemplate?: string | null;
};

/** One value being filled in for a definition (job run, report generation). */
export type LyoParameterEntry<TDefinition extends LyoParameterDefinition = LyoParameterDefinition> = {
  definition: TDefinition;
  /** Current JSON; null means unset. */
  value: string | null;
  /** Run-time Encrypt switch. */
  encrypted: boolean;
  /** Key with ` *` when required. */
  label: string;
  /** Caption naming the expression the seeded value came from. */
  defaultExpressionHint: string | null;
};

const isBoolType = (type: string) => lyoTypeFromName(type).editorKind === "Boolean";

/**
 * Entries for a definition list, ordered by `order` then key when given. Values seed from `resolveExpression` (expression defaults), the literal default, or the
 * type default for required and bool parameters; optional ones without a default stay unset.
 */
export function entriesFrom<TDefinition extends LyoParameterDefinition>(
  definitions: readonly TDefinition[] | null | undefined,
  order?: (d: TDefinition) => number,
  resolveExpression?: (d: TDefinition) => string | null
): LyoParameterEntry<TDefinition>[] {
  const list = [...(definitions ?? [])];
  if (order) list.sort((a, b) => order(a) - order(b) || a.key.localeCompare(b.key, undefined, { sensitivity: "base" }));
  return list.map((definition) => {
    const isExpression = definition.defaultKind === "Expression" && Boolean(definition.defaultTemplate?.trim());
    const resolved = isExpression ? resolveExpression?.(definition) : null;
    const value = resolved ?? (definition.value ? definition.value : definition.required || isBoolType(definition.type) ? defaultJsonFor(definition.type) : null);
    return {
      definition,
      value,
      encrypted: false,
      label: definition.required ? `${definition.key} *` : definition.key,
      defaultExpressionHint: isExpression ? `Default from expression: ${definition.defaultTemplate}` : null,
    };
  });
}

/** Bools are never empty; blank / `null` is empty; a collection with no items is empty. */
export function entryIsEmpty(entry: Pick<LyoParameterEntry, "definition" | "value">): boolean {
  if (isBoolType(entry.definition.type)) return false;
  if (!entry.value?.trim() || entry.value === "null") return true;
  return isLyoCollection(lyoTypeFromName(entry.definition.type)) && parseListJson(entry.value).length === 0;
}

export function entrySubmitValue(entry: Pick<LyoParameterEntry, "value">): string | null {
  return !entry.value?.trim() || entry.value === "null" ? null : entry.value;
}

/** Blank optional entries are omitted so the server uses the definition default; bools and required entries are always sent. */
export function entryShouldSubmit(entry: Pick<LyoParameterEntry, "definition" | "value">): boolean {
  return entrySubmitValue(entry) !== null || Boolean(entry.definition.required) || isBoolType(entry.definition.type);
}

export function requiredSatisfied(entries: readonly Pick<LyoParameterEntry, "definition" | "value">[]): boolean {
  return entries.filter((e) => e.definition.required).every((e) => !entryIsEmpty(e));
}

/** Key to current value, for `{{Key}}` binding in query / sproc options. */
export function siblingMap(entries: readonly { definition: { key: string }; value: string | null }[]): Record<string, string | null> {
  return Object.fromEntries(entries.map((e) => [e.definition.key, e.value]));
}
