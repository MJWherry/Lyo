import { describe, expect, it, vi } from "vitest";
import { LyoTypes } from "../types/lyoTypeCatalog.js";
import {
  addParameterRow,
  applyCreated,
  deleteParameterRow,
  dropInsertIndex,
  editRowFromMasked,
  editRowErrors,
  effectiveOptions,
  entriesFrom,
  entryIsEmpty,
  entryShouldSubmit,
  entrySubmitValue,
  moveParameterRow,
  newEditRow,
  requiredSatisfied,
  rowForSave,
  saveParameterRows,
  setRowEncrypt,
  setRowKey,
  setRowOptions,
  setRowType,
  siblingMap,
  type LyoParameterEditRow,
} from "./editRow.js";

const row = (patch: Partial<LyoParameterEditRow> = {}): LyoParameterEditRow => ({ ...newEditRow(), isNew: false, id: "1", key: "A", ...patch });

describe("row <-> api", () => {
  it("editRowFromMasked treats *** as an encrypted value and keeps the rest", () => {
    const masked = editRowFromMasked({ id: "9", key: "Secret", type: "System.String", value: "***", order: 3 });
    expect(masked).toMatchObject({ id: "9", key: "Secret", value: null, isEncrypted: true, isNew: false, order: 3, enabled: true, defaultKind: "Literal" });
    expect(editRowFromMasked({ id: "9", key: "K", type: "int", value: "4" })).toMatchObject({ value: "4", isEncrypted: false });
  });

  it("rowForSave never sends the mask, clears literal templates, and sends an encryption marker only when encrypted", () => {
    expect(rowForSave(row({ value: "***", isEncrypted: true }))).toMatchObject({ value: null, encryptedValue: "" });
    expect(rowForSave(row({ defaultKind: "Literal", defaultTemplate: "{now}" })).defaultTemplate).toBeNull();
    expect(rowForSave(row({ defaultKind: "Expression", defaultTemplate: "{now}" })).defaultTemplate).toBe("{now}");
    expect(rowForSave(row({ encryptedValue: "abc" })).encryptedValue).toBeNull();
  });

  it("applyCreated stamps the server id and clears isNew", () => {
    expect(applyCreated(row({ isNew: true, id: null }), { id: "5", options: null, allowedValues: '["a"]', value: "***" })).toMatchObject({
      id: "5",
      isNew: false,
      allowedValues: '["a"]',
      value: null,
    });
  });

  it("effectiveOptions falls back to static options from AllowedValues", () => {
    expect(effectiveOptions(row({ allowedValues: '["x"]' }))).toBe('{"kind":"static","items":[{"key":"x","label":"x"}],"sprocParameters":{}}');
    expect(effectiveOptions(row({ options: "{}", allowedValues: '["x"]' }))).toBe("{}");
  });
});

describe("editing", () => {
  it("addParameterRow names the next ParamN, appends the next order, and honours defaultRequired", () => {
    const rows = addParameterRow([row({ order: 4 })], { defaultRequired: true });
    expect(rows[1]).toMatchObject({ key: "Param2", type: LyoTypes.String.fullName, isNew: true, required: true, order: 5 });
  });

  it("addParameterRow picks the first unused inherited key and copies its type and options", () => {
    const inherit = [row({ key: "A" }), row({ key: "B", type: "int", options: "{o}", allowedValues: "[1]", description: "Bee" })];
    const [, added] = addParameterRow([row({ key: "a" })], { inheritFrom: inherit });
    expect(added).toMatchObject({ key: "B", type: "int", options: "{o}", allowedValues: "[1]", description: "Bee" });
  });

  it("setRowKey re-applies inheritance but keeps an existing description", () => {
    const inherit = [row({ key: "B", type: "int", description: "Bee" })];
    expect(setRowKey(row({ description: "mine" }), "b", inherit)).toMatchObject({ key: "b", type: "int", description: "mine" });
  });

  it("setRowType resets the value only when the type identity or shape changes", () => {
    expect(setRowType(row({ type: "int", value: "3" }), "System.Int32").value).toBe("3");
    expect(setRowType(row({ type: "int", value: "3" }), "long").value).toBeNull();
  });

  it("setRowEncrypt adds an empty ciphertext marker and clears it when turned off", () => {
    expect(setRowEncrypt(row(), true)).toMatchObject({ isEncrypted: true, encryptedValue: "" });
    expect(setRowEncrypt(row({ isEncrypted: true, encryptedValue: "c" }), false)).toMatchObject({ isEncrypted: false, encryptedValue: null });
  });

  it("setRowOptions keeps AllowedValues in step with static option keys", () => {
    const opts = '{"kind":"static","items":[{"key":"1","label":"One"}]}';
    expect(setRowOptions(row({ type: LyoTypes.IntList.fullName }), opts)).toMatchObject({ options: opts, allowedValues: "[1]" });
    expect(setRowOptions(row(), null)).toMatchObject({ options: null, allowedValues: null });
  });
});

describe("reordering", () => {
  const rows = [row({ key: "A", order: 0 }), row({ key: "B", order: 1 }), row({ key: "C", order: 2 })];

  it("moveParameterRow moves and renumbers; out-of-range and no-op moves return the same list", () => {
    expect(moveParameterRow(rows, 0, 2).map((r) => [r.key, r.order])).toEqual([
      ["B", 0],
      ["C", 1],
      ["A", 2],
    ]);
    expect(moveParameterRow(rows, 1, 1)).toBe(rows);
    expect(moveParameterRow(rows, 0, 5)).toBe(rows);
  });

  it("deleteParameterRow removes and renumbers", () => {
    expect(deleteParameterRow(rows, rows[0]!).map((r) => [r.key, r.order])).toEqual([
      ["B", 0],
      ["C", 1],
    ]);
  });

  it("dropInsertIndex shifts drops below the source up by one", () => {
    expect(dropInsertIndex(0, 2)).toBe(1);
    expect(dropInsertIndex(2, 0)).toBe(0);
  });
});

describe("validation", () => {
  it("flags blank keys, case-insensitive duplicates, and values that do not match the type", () => {
    const errors = editRowErrors([row({ key: " " }), row({ key: "X" }), row({ key: "x" }), row({ key: "N", type: "int", value: "1.5" }), row({ key: "OK", type: "int", value: "2" })]);
    expect(errors.get(0)).toEqual(["Key is required."]);
    expect(errors.get(1)).toEqual(["Duplicate key 'X'."]);
    expect(errors.get(2)).toEqual(["Duplicate key 'x'."]);
    expect(errors.get(3)).toEqual(["Value is not a valid int."]);
    expect(errors.has(4)).toBe(false);
  });
});

describe("entries", () => {
  const defs = [
    { key: "B", type: "int", required: true, order: 2 },
    { key: "A", type: "bool", required: false, order: 1 },
    { key: "C", type: "string", required: false, value: '"x"', order: 1 },
    { key: "L", type: LyoTypes.StringList.fullName, required: true, order: 3 },
  ];

  it("entriesFrom orders by order then key and seeds required / bool defaults; optional stays unset", () => {
    const entries = entriesFrom(defs, (d) => d.order);
    expect(entries.map((e) => [e.definition.key, e.value])).toEqual([
      ["A", "false"],
      ["C", '"x"'],
      ["B", "0"],
      ["L", "[]"],
    ]);
    expect(entries.map((e) => e.label)).toEqual(["A", "C", "B *", "L *"]);
  });

  it("isEmpty: bools are never empty, blank / null are empty, empty lists are empty", () => {
    const [a, , b, l] = entriesFrom(defs, (d) => d.order);
    expect(entryIsEmpty({ ...a!, value: null })).toBe(false);
    expect(entryIsEmpty({ ...b!, value: "null" })).toBe(true);
    expect(entryIsEmpty(l!)).toBe(true);
    expect(requiredSatisfied([b!, l!])).toBe(false);
    expect(requiredSatisfied([b!, { ...l!, value: '["v"]' }])).toBe(true);
  });

  it("submit value / shouldSubmit: blank optional entries are omitted, bools and required always sent", () => {
    const [a, c] = entriesFrom(defs, (d) => d.order);
    expect(entrySubmitValue({ ...c!, value: " " })).toBeNull();
    expect(entryShouldSubmit({ ...c!, value: null })).toBe(false);
    expect(entryShouldSubmit({ ...a!, value: null })).toBe(true);
  });

  it("siblingMap maps key to current value for {{Key}} binding", () => {
    expect(siblingMap(entriesFrom(defs.slice(0, 1)))).toEqual({ B: "0" });
  });
});

describe("saveParameterRows", () => {
  it("deletes missing ids first, creates new rows (stamping ids), then updates the rest", async () => {
    const calls: string[] = [];
    const rows = [row({ id: "1", key: "Keep" }), row({ id: null, key: "New", isNew: true })];
    const saved = await saveParameterRows(rows, ["1", "2"], {
      remove: vi.fn(async (id: string) => void calls.push(`delete ${id}`)),
      create: vi.fn(async (r: LyoParameterEditRow) => {
        calls.push(`create ${r.key}`);
        return { id: "3", options: null, allowedValues: null, value: r.value };
      }),
      update: vi.fn(async (r: LyoParameterEditRow) => void calls.push(`update ${r.key}`)),
    });
    expect(calls).toEqual(["delete 2", "create New", "update Keep"]);
    expect(saved[1]).toMatchObject({ id: "3", isNew: false });
  });
});
