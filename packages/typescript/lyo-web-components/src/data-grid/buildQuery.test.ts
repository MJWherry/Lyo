import { describe, expect, it } from "vitest";
import { buildGridWhere } from "lyo-query";
import { buildProjectedQuery, buildRootQuery } from "./buildQuery.js";
import { createLyoColumn } from "./types.js";

describe("buildProjectedQuery", () => {
  it("includes visible columns and key fields", () => {
    const q = buildProjectedQuery({
      start: 25,
      amount: 25,
      filters: [{ $type: "condition", Field: "Status", Comparison: "Equals", Value: "Open" }],
      searchText: "ann",
      quickSearchProperties: ["FirstName", "LastName"],
      sorts: [{ PropertyName: "LastName", Direction: "Asc", Priority: 0 }],
      columns: [
        createLyoColumn({ id: "id", field: "Id", header: "ID" }),
        createLyoColumn({ id: "fn", field: "FirstName", header: "First" }),
        createLyoColumn({ id: "hidden", field: "Secret", header: "Secret" }),
      ],
      hiddenFields: new Set(["Secret"]),
      keyFields: ["Id"],
    });
    expect(q.Start).toBe(25);
    expect(q.Amount).toBe(25);
    expect(q.Select).toEqual(["Id", "FirstName"]);
    expect(q.whereClause).toEqual(
      buildGridWhere({
        filters: [{ $type: "condition", Field: "Status", Comparison: "Equals", Value: "Open" }],
        searchText: "ann",
        quickSearchProperties: ["FirstName", "LastName"],
      })
    );
  });
});

describe("buildRootQuery", () => {
  it("prefixes Select with the From alias", () => {
    const q = buildRootQuery({
      start: 0,
      amount: 25,
      filters: [],
      searchText: null,
      quickSearchProperties: ["FirstName"],
      sorts: [],
      columns: [
        createLyoColumn({ id: "id", field: "Id", header: "ID" }),
        createLyoColumn({ id: "fn", field: "FirstName", header: "First" }),
      ],
      hiddenFields: new Set(),
      keyFields: ["Id"],
      entityType: "Person",
      fromAlias: "p",
    });
    expect(q.From).toEqual({ Alias: "p", EntityType: "Person" });
    expect(q.Select).toEqual(["p.Id", "p.FirstName"]);
  });

  it("omits nested projection paths from Select", () => {
    const q = buildRootQuery({
      start: 0,
      amount: 25,
      filters: [],
      searchText: null,
      quickSearchProperties: [],
      sorts: [],
      columns: [
        createLyoColumn({ id: "id", field: "Id", header: "ID" }),
        createLyoColumn({ id: "addr", field: "ContactAddresses.Count", header: "Addresses" }),
      ],
      hiddenFields: new Set(),
      keyFields: ["Id"],
    });
    expect(q.From.EntityType).toBe("Person");
    expect(q.Select).toEqual(["p.Id"]);
  });
});
