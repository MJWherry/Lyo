import {describe, expect, it} from "vitest";
import {createTreeLoader} from "./fileTree.js";

describe("file tree", () => {
    it("fileTree_Expand_LoadsChildrenOnce", async () => {
        const calls: string[] = [];
        const tree = createTreeLoader(async (path) => {
            calls.push(path);
            return [{path: `${path}/a`, name: "a", isDirectory: false}];
        });
        await tree.expand("/docs");
        await tree.expand("/docs");
        expect(calls).toEqual(["/docs"]);
    });
});
