import { describe, expect, it } from "vitest";
import { buildGridWhere, mergeWhere } from "./grid.js";
import type { ConditionClause } from "../models/whereClause.js";

describe("mergeWhere", () => {
    it("returns null for empty", () => {
        expect(mergeWhere([])).toBeNull();
    });

    it("unwraps a single clause", () => {
        const c: ConditionClause = {
            $type: "condition",
            Field: "Name",
            Comparison: "Equals",
            Value: "a",
        };
        expect(mergeWhere([c])).toEqual(c);
    });

    it("And-groups multiple", () => {
        const a: ConditionClause = {
            $type: "condition",
            Field: "A",
            Comparison: "Equals",
            Value: 1,
        };
        const b: ConditionClause = {
            $type: "condition",
            Field: "B",
            Comparison: "Equals",
            Value: 2,
        };
        expect(mergeWhere([a, b])).toEqual({
            $type: "group",
            Operator: "And",
            Children: [a, b],
        });
    });
});

describe("buildGridWhere", () => {
    it("ORs search across fields and ANDs each with filters", () => {
        const filters: ConditionClause[] = [
            { $type: "condition", Field: "Status", Comparison: "Equals", Value: "Open" },
        ];
        const where = buildGridWhere({
            filters,
            searchText: "ann",
            quickSearchProperties: ["FirstName", "LastName"],
        });
        expect(where?.$type).toBe("group");
        if (where?.$type !== "group") return;
        expect(where.Operator).toBe("Or");
        expect(where.Children).toHaveLength(2);
    });

    it("uses filters only when search is empty", () => {
        const filters: ConditionClause[] = [
            { $type: "condition", Field: "Status", Comparison: "Equals", Value: "Open" },
        ];
        expect(buildGridWhere({ filters, searchText: "  " })).toEqual(filters[0]);
    });
});
