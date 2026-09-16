import { describe, expect, it } from "vitest";
import type { ConditionClause, GroupClause, WhereClause } from "lyo-query";
import {
  addChildAt,
  addSubClauseAt,
  fieldNameError,
  moveInto,
  nodeAt,
  nodeLabel,
  removeAt,
  removeSubClauseAt,
  setDescriptionAt,
} from "./whereTree.js";

const cond = (Field: string, Value: unknown = null): ConditionClause => ({ $type: "condition", Field, Comparison: "Equals", Value });
const group = (...Children: WhereClause[]): GroupClause => ({ $type: "group", Operator: "And", Children });

describe("whereTree", () => {
  it("adds a condition or group to the group at a path without mutating the input", () => {
    const root = group(cond("A"), group(cond("B")));
    const withCondition = addChildAt(root, [1], "condition");
    expect((nodeAt(withCondition, [1]) as GroupClause).Children).toHaveLength(2);
    expect(nodeAt(withCondition, [1, 1])).toMatchObject({ $type: "condition", Field: "", Comparison: "Equals", Value: null });
    expect((root.Children[1] as GroupClause).Children).toHaveLength(1);

    const withGroup = addChildAt(root, [], "group");
    expect(nodeAt(withGroup, [2])).toMatchObject({ $type: "group", Operator: "And", Children: [{ $type: "condition", Field: "" }] });
  });

  it("adds and removes a SubClause on a condition or group", () => {
    const root = group(cond("A"));
    const withSub = addSubClauseAt(root, [0]);
    expect(nodeAt(withSub, [0, "sub"])).toMatchObject({ $type: "group", Children: [{ $type: "condition", Field: "" }] });
    expect(nodeAt(withSub, [0, "sub", 0])).toMatchObject({ Field: "" });
    expect(nodeAt(removeSubClauseAt(withSub, [0]), [0])).not.toHaveProperty("subClause");

    const rootSub = addSubClauseAt(root, []);
    expect(nodeAt(rootSub, ["sub"])).toMatchObject({ $type: "group" });
  });

  it("removes a node from its parent group, or clears a SubClause path; the root stays", () => {
    const root = group(cond("A"), cond("B"));
    expect((removeAt(root, [0]) as GroupClause).Children.map((c) => (c as ConditionClause).Field)).toEqual(["B"]);
    expect(removeAt(root, [])).toBe(root);
    const withSub = addSubClauseAt(root, [1]);
    expect(nodeAt(removeAt(withSub, [1, "sub"]), [1])).not.toHaveProperty("subClause");
  });

  it("moves a node into a group (drag and drop), adjusting for the removed sibling", () => {
    const root = group(cond("A"), cond("B"), group(cond("C")));
    const moved = moveInto(root, [0], [2]) as GroupClause;
    expect(moved.Children.map((c) => c.$type)).toEqual(["condition", "group"]);
    expect((moved.Children[1] as GroupClause).Children.map((c) => (c as ConditionClause).Field)).toEqual(["C", "A"]);
  });

  it("refuses moves into a condition, into the node's own subtree, or of the root", () => {
    const root = group(cond("A"), group(cond("C")));
    expect(moveInto(root, [0], [1, 0])).toBe(root);
    expect(moveInto(root, [1], [1])).toBe(root);
    const nested = addChildAt(root, [1], "group");
    expect(moveInto(nested, [1], [1, 1])).toBe(nested);
    expect(moveInto(root, [], [1])).toBe(root);
  });

  it("sets or clears Description", () => {
    const root = group(cond("A"));
    expect(nodeAt(setDescriptionAt(root, [0], " Adults "), [0])).toMatchObject({ Description: "Adults" });
    expect(nodeAt(setDescriptionAt(setDescriptionAt(root, [0], "x"), [0], "  "), [0])).not.toHaveProperty("Description");
  });

  it("validates field names: required, letters and dots only", () => {
    expect(fieldNameError("")).toBe("Field name is required.");
    expect(fieldNameError("Person.FirstName")).toBeNull();
    expect(fieldNameError("First_Name")).toBe("Only letters (a-z, A-Z) and dots are allowed.");
    expect(fieldNameError("LastName[0]")).toBe("Only letters (a-z, A-Z) and dots are allowed.");
  });

  it("labels nodes like the Blazor tree", () => {
    expect(nodeLabel({ ...group(cond("A"), cond("B")), Description: "Both" })).toBe("And (2 conditions) - Both");
    expect(nodeLabel(group(cond("A")))).toBe("And (1 condition)");
    expect(nodeLabel(cond("Age", 5))).toBe("Age Equals 5");
    expect(nodeLabel({ ...cond("Tag"), Comparison: "In", Value: ["a", "b"] })).toBe("Tag In [a, b]");
    expect(nodeLabel(cond("Name"))).toBe("Name Equals null");
  });
});
