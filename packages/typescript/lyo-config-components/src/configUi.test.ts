import {describe, expect, it} from "vitest";
import {sourceChip} from "./configUi.js";

const definition = {
    subjectEntityType: "App",
    key: "Theme",
    forValueType: "System.String",
};

describe("source chips", () => {
    it("sourceChip_OverriddenDefaultMissing", () => {
        expect(sourceChip({definition, binding: {definitionId: "d", key: "Theme", subjectEntityType: "App", subjectEntityId: "local", value: {typeName: "System.String", json: '"light"'}}})).toBe("Overridden");
        expect(sourceChip({definition: {...definition, defaultValue: {typeName: "System.String", json: '"light"'}}})).toBe("Using default");
        expect(sourceChip({definition})).toBe("Missing");
    });
});
