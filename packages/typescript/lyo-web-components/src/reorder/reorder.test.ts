import {describe, expect, it} from "vitest";
import {moveDown} from "./reorder.js";

describe("reorder", () => {
    it("reorder_MoveDown_SwapsNeighbors", () => {
        expect(moveDown(["a", "b", "c"], 1)).toEqual(["a", "c", "b"]);
    });
});
