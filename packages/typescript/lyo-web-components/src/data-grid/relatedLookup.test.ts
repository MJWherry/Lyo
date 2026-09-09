import { describe, expect, it, vi } from "vitest";
import { createLyoColumn, createLyoIdColumn } from "./types.js";
import {
  RELATED_KEY_CHUNK_SIZE,
  chunkKeys,
  collectRelatedIds,
  loadRelatedEntities,
  lookupRelated,
  relatedGroups,
  relatedIdKey,
  relatedQueryBody,
} from "./relatedLookup.js";

describe("relatedGroups", () => {
  it("unions select and fields for the same route and skips hidden columns", () => {
    const columns = [
      createLyoIdColumn({
        id: "pob",
        field: "PlaceOfBirthAddressId",
        header: "POB",
        related: { route: "PersonAddress", select: ["FullAddress"] },
      }),
      createLyoIdColumn({
        id: "other",
        field: "OtherAddressId",
        header: "Other",
        related: { route: "PersonAddress", select: ["City"] },
      }),
      createLyoIdColumn({
        id: "hidden",
        field: "HiddenAddressId",
        header: "Hidden",
        related: { route: "PersonAddress", select: ["County"] },
      }),
      createLyoColumn({ id: "name", field: "FirstName", header: "Name" }),
    ];

    const groups = relatedGroups(columns, new Set(["HiddenAddressId"]));
    expect(groups).toHaveLength(1);
    expect(groups[0]?.route).toBe("PersonAddress");
    expect(groups[0]?.fields).toEqual(expect.arrayContaining(["PlaceOfBirthAddressId", "OtherAddressId"]));
    expect(groups[0]?.fields).not.toContain("HiddenAddressId");
    expect(groups[0]?.select).toEqual(expect.arrayContaining(["FullAddress", "City", "Id"]));
  });

  it("splits different routes into separate groups", () => {
    const columns = [
      createLyoIdColumn({
        id: "addr",
        field: "AddressId",
        header: "Addr",
        related: { route: "PersonAddress", select: ["City"] },
      }),
      createLyoIdColumn({
        id: "person",
        field: "EmergencyContactPersonId",
        header: "EC",
        related: { route: "Person", select: ["FirstName"] },
      }),
    ];
    const groups = relatedGroups(columns, new Set());
    expect(groups.map((g) => g.route).sort()).toEqual(["Person", "PersonAddress"]);
  });
});

describe("collectRelatedIds / chunkKeys", () => {
  it("drops empty ids and dedupes", () => {
    const a = "11111111-1111-1111-1111-111111111111";
    const rows = [
      { PlaceOfBirthAddressId: a, OtherAddressId: a },
      { PlaceOfBirthAddressId: "22222222-2222-2222-2222-222222222222" },
      { PlaceOfBirthAddressId: "00000000-0000-0000-0000-000000000000" },
      { PlaceOfBirthAddressId: null },
    ];
    const ids = collectRelatedIds(rows, ["PlaceOfBirthAddressId", "OtherAddressId"]);
    expect(ids).toHaveLength(2);
  });

  it("chunks Keys rows at 100", () => {
    const ids = Array.from({ length: 250 }, (_, i) => i + 1);
    const chunks = chunkKeys(ids);
    expect(RELATED_KEY_CHUNK_SIZE).toBe(100);
    expect(chunks).toHaveLength(3);
    expect(chunks[0]).toHaveLength(100);
    expect(chunks[0]?.[0]).toEqual([1]);
    expect(chunks[2]).toHaveLength(50);
  });
});

describe("relatedQueryBody", () => {
  it("sets Keys Start and Amount", () => {
    const body = relatedQueryBody(["Id", "City"], [["a"], ["b"]]);
    expect(body).toEqual({
      Select: ["Id", "City"],
      Keys: [["a"], ["b"]],
      Start: 0,
      Amount: 2,
    });
  });
});

describe("loadRelatedEntities", () => {
  it("calls queryProject once per route with unioned keys", async () => {
    const a = "11111111-1111-1111-1111-111111111111";
    const b = "22222222-2222-2222-2222-222222222222";
    const queryProject = vi.fn(async (route: string) => ({
      ok: true,
      data: {
        isSuccess: true,
        items: route === "PersonAddress" ? [{ Id: a, FullAddress: "1 Main" }] : [],
      },
    }));
    const columns = [
      createLyoIdColumn({
        id: "pob",
        field: "PlaceOfBirthAddressId",
        header: "POB",
        related: { route: "PersonAddress", select: ["FullAddress"] },
      }),
      createLyoIdColumn({
        id: "other",
        field: "OtherAddressId",
        header: "Other",
        related: { route: "PersonAddress", select: ["City"] },
      }),
    ];
    const lookup = await loadRelatedEntities(
      queryProject,
      columns,
      new Set(),
      [{ PlaceOfBirthAddressId: a, OtherAddressId: b }]
    );
    expect(queryProject).toHaveBeenCalledTimes(1);
    const [, body] = queryProject.mock.calls[0] as [string, Record<string, unknown>];
    expect(queryProject.mock.calls[0]?.[0]).toBe("PersonAddress");
    expect(body.Keys).toEqual(expect.arrayContaining([[a], [b]]));
    expect(lookupRelated(lookup, "PersonAddress", a)).toEqual({ Id: a, FullAddress: "1 Main" });
  });
});

describe("relatedIdKey", () => {
  it("rejects empty guid and null", () => {
    expect(relatedIdKey(null)).toBeNull();
    expect(relatedIdKey("00000000-0000-0000-0000-000000000000")).toBeNull();
    expect(relatedIdKey("abc")).toBe("abc");
  });
});
