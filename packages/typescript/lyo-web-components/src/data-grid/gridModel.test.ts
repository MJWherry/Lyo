import { describe, expect, it } from "vitest";
import { createLyoColumn, createLyoIdColumn } from "./types.js";
import {
  DEFAULT_MAX_BULK_SIZE,
  bulkLabel,
  canExport,
  cardColumns,
  isIdField,
  quickSearchPlaceholder,
  quickSearchProperties,
  resolveCardMode,
  selectGrew,
} from "./gridModel.js";

const col = (field: string, header: string, extra: Partial<Parameters<typeof createLyoColumn>[0]> = {}) => createLyoColumn({ id: field, field, header, ...extra });

describe("quickSearch", () => {
  const columns = [col("Name", "Name", { quickSearch: true }), col("Id", "Id"), col("JobDefinition.Id", "Job"), col("MostRecentAddressId", "Recent")];

  it("isIdField matches a last path segment of Id only", () => {
    expect(isIdField("Id")).toBe(true);
    expect(isIdField("JobDefinition.Id")).toBe(true);
    expect(isIdField("MostRecentAddressId")).toBe(false);
    expect(isIdField(" ")).toBe(false);
  });

  it("unions Id-leaf columns into the quick-search columns, deduped case-insensitively", () => {
    expect(quickSearchProperties(columns)).toEqual(["Name", "Id", "JobDefinition.Id"]);
    expect(quickSearchProperties(columns, ["name", "Email"])).toEqual(["name", "Email", "Id", "JobDefinition.Id"]);
  });

  it("placeholder lists column headers (or leaves) that quick search ORs", () => {
    expect(quickSearchPlaceholder([col("Name", "Name", { quickSearch: true }), col("Id", "Id")])).toBe("Search Name, Id");
    expect(quickSearchPlaceholder([col("Name", "Name", { quickSearch: true }), col("Owner.Id", "")])).toBe("Search Name, Id");
    expect(quickSearchPlaceholder([col("Name", "Name")])).toBe("Search...");
  });
});

describe("cardColumns", () => {
  it("an explicit cardTitle wins", () => {
    const cards = cardColumns([col("Name", "Name"), col("City", "City", { cardTitle: true })], new Set());
    expect(cards.filter((c) => c.isTitle).map((c) => c.column.field)).toEqual(["City"]);
  });

  it("otherwise the first name-like column that is not an identifier", () => {
    const cards = cardColumns([createLyoIdColumn({ id: "k", field: "Key", header: "Key" }), col("Title", "Title"), col("Name", "Name")], new Set());
    expect(cards.find((c) => c.isTitle)?.column.field).toBe("Name");
  });

  it("filters hidden columns and has no title when nothing reads like a name", () => {
    const cards = cardColumns([col("Name", "Name"), col("Age", "Age")], new Set(["Name"]));
    expect(cards.map((c) => c.column.field)).toEqual(["Age"]);
    expect(cards.some((c) => c.isTitle)).toBe(false);
  });

  it("resolveCardMode: Cards and Table are fixed, Auto follows the viewport", () => {
    expect(resolveCardMode("Cards", false)).toBe(true);
    expect(resolveCardMode("Table", true)).toBe(false);
    expect(resolveCardMode("Auto", true)).toBe(true);
    expect(resolveCardMode("Auto", false)).toBe(false);
  });
});

describe("bulk", () => {
  const base = { loading: false, selectedCount: 0, total: 0, maxBulkSize: DEFAULT_MAX_BULK_SIZE };

  it("defaults maxBulkSize to 2000", () => {
    expect(DEFAULT_MAX_BULK_SIZE).toBe(2000);
  });

  it("bulkLabel covers loading, selection, empty, total, and capped totals", () => {
    expect(bulkLabel({ ...base, loading: true, total: 5 })).toBe("");
    expect(bulkLabel({ ...base, selectedCount: 3 })).toBe("(3 items)");
    expect(bulkLabel({ ...base, selectedCount: 2001 })).toBe("(too many items)");
    expect(bulkLabel(base)).toBe("(no items)");
    expect(bulkLabel({ ...base, total: 1500 })).toBe("(1,500 items)");
    expect(bulkLabel({ ...base, total: 5000 })).toBe("(first 2,000 of 5,000)");
  });

  it("canExport: never while loading, selection must fit the cap, otherwise needs rows", () => {
    expect(canExport({ ...base, loading: true, total: 5 })).toBe(false);
    expect(canExport({ ...base, selectedCount: 2000 })).toBe(true);
    expect(canExport({ ...base, selectedCount: 2001 })).toBe(false);
    expect(canExport({ ...base, total: 10_000 })).toBe(true);
    expect(canExport(base)).toBe(false);
  });
});

describe("visibility", () => {
  it("selectGrew is false when a column is hidden and true when a new field appears", () => {
    expect(selectGrew(["Id", "Name", "Age"], ["Id", "Name"])).toBe(false);
    expect(selectGrew(["Id", "Name"], ["id", " Name "])).toBe(false);
    expect(selectGrew(["Id", "Name"], ["Id", "Name", "Age"])).toBe(true);
    expect(selectGrew(null, ["Id"])).toBe(true);
  });
});

describe("createLyoIdColumn", () => {
  it("marks the column as an identifier and allows no related lookup", () => {
    const column = createLyoIdColumn({ id: "id", field: "Id", header: "Id" });
    expect(column.identifier).toBe(true);
    expect(column.related).toBeUndefined();
  });
});
