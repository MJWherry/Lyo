import { describe, expect, it } from "vitest";
import { defaultQueryOptions, type ProjectionQueryReq, type QueryReq } from "lyo-query";
import {
  addComputedField,
  addJoinOn,
  formatKeySet,
  keysFromLines,
  parseKeyInput,
  patchJoin,
  patchJoinOn,
  removeJoinOn,
  withAggregates,
  withComputedFields,
  withKeys,
  withZip,
  zipEnabled,
} from "./requestShape.js";

const project = (): ProjectionQueryReq => ({ Options: defaultQueryOptions(), Select: ["Id"] });
const root = (): QueryReq => ({
  Options: defaultQueryOptions(),
  From: { Alias: "p", EntityType: "Person" },
  Joins: [{ Alias: "ca", EntityType: "ContactAddressEntity", Type: "Left", On: [{ From: "p.Id", To: "ca.PersonId" }] }],
  Select: ["p.Id"],
});

describe("keys editor", () => {
  it("parses comma parts into numbers or strings, stripping quotes", () => {
    expect(parseKeyInput("1")).toEqual([1]);
    expect(parseKeyInput(' "a,b" ')).toEqual(['"a', 'b"']);
    expect(parseKeyInput('"abc", 2, 1.5, x')).toEqual(["abc", 2, 1.5, "x"]);
    expect(parseKeyInput("  ")).toEqual([]);
  });

  it("produces Keys rows [[...]] and drops blank lines", () => {
    expect(keysFromLines(["1", '"a", 2', " "])).toEqual([[1], ["a", 2]]);
  });

  it("formats key sets for the chips (strings quoted)", () => {
    expect(formatKeySet(["a", 2, null])).toBe('"a", 2, null');
  });

  it("withKeys sets Keys and removes the property when empty", () => {
    expect(withKeys(project(), [[1]]).Keys).toEqual([[1]]);
    expect(withKeys({ ...project(), Keys: [[1]] }, [])).not.toHaveProperty("Keys");
  });
});

describe("computed fields", () => {
  it("adds trimmed fields, ignores blanks, and round-trips on the request", () => {
    let fields = addComputedField([], " FullName ", " {FirstName} {LastName} ");
    fields = addComputedField(fields, "", "x");
    fields = addComputedField(fields, "y", " ");
    expect(fields).toEqual([{ Name: "FullName", Template: "{FirstName} {LastName}" }]);

    const req = withComputedFields(project(), fields);
    expect(req.ComputedFields).toEqual(fields);
    expect(JSON.parse(JSON.stringify(req)).ComputedFields).toEqual(fields);
    expect(withComputedFields(req, [])).not.toHaveProperty("ComputedFields");
  });
});

describe("aggregates", () => {
  it("keeps a Count field, drops a blank field, and omits an empty list", () => {
    const req = withAggregates(project(), [
      { Function: "Count", Name: "Rows", Field: "  " },
      { Function: "Count", Name: "Addresses", Field: " contactaddresses " },
      { Function: "Sum", Name: "AgeSum", Field: " Age " },
    ]);
    expect(req.Aggregates).toEqual([
      { Function: "Count", Name: "Rows" },
      { Function: "Count", Name: "Addresses", Field: "contactaddresses" },
      { Function: "Sum", Name: "AgeSum", Field: "Age" },
    ]);
    expect(JSON.parse(JSON.stringify(req)).Aggregates).toEqual(req.Aggregates);
    expect(withAggregates(req, [])).not.toHaveProperty("Aggregates");
  });
});

describe("joins", () => {
  it("sets Type and As (blank As is dropped) and edits On pairs", () => {
    let q = patchJoin(root(), 0, { Type: "Inner", As: " Address " });
    q = addJoinOn(q, 0);
    q = patchJoinOn(q, 0, 1, { From: "p.AddressId", To: "ca.Id" });
    expect(q.Joins?.[0]).toEqual({
      Alias: "ca",
      EntityType: "ContactAddressEntity",
      Type: "Inner",
      As: "Address",
      On: [
        { From: "p.Id", To: "ca.PersonId" },
        { From: "p.AddressId", To: "ca.Id" },
      ],
    });
    expect(patchJoin(q, 0, { As: "  " }).Joins?.[0]).not.toHaveProperty("As");
    expect(removeJoinOn(q, 0, 0).Joins?.[0].On).toEqual([{ From: "p.AddressId", To: "ca.Id" }]);
  });

  it("new On pairs start from the From and join aliases", () => {
    expect(addJoinOn(root(), 0).Joins?.[0].On[1]).toEqual({ From: "p.", To: "ca." });
  });
});

describe("zip", () => {
  it("defaults on and writes the flag into Options", () => {
    expect(zipEnabled(project())).toBe(true);
    const off = withZip(project(), false);
    expect(off.Options.ZipSiblingCollectionSelections).toBe(false);
    expect(zipEnabled(off)).toBe(false);
  });
});
