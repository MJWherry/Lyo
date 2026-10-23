import {describe, expect, it} from "vitest";
import {grantFormToWrite} from "./grantForm.js";

describe("grant form", () => {
    it("blank resource id and tenant are null", () => {
        const write = grantFormToWrite({subject: "user-a", action: "read", resourceType: "note", resourceId: "", tenantId: "  "});
        expect(write.resourceId).toBeNull();
        expect(write.tenantId).toBeNull();
    });

    it("guid resource id is lowercase D", () => {
        const write = grantFormToWrite({
            subject: "user-a",
            action: "read",
            resourceType: "note",
            resourceId: "AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE",
        });
        expect(write.resourceId).toBe("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    });

    it("non-guid resource id is trimmed and not lowercased", () => {
        const write = grantFormToWrite({subject: "User-A", action: "read", resourceType: "note", resourceId: " Note-1 "});
        expect(write.resourceId).toBe("Note-1");
        expect(write.subject).toBe("User-A");
    });

    it("local expiry is UTC without shifting the clock", () => {
        const write = grantFormToWrite({
            subject: "user-a",
            action: "read",
            resourceType: "note",
            expiresAt: "2026-10-14T15:30",
        });
        expect(write.expiresAt).toBe("2026-10-14T15:30:00.000Z");
    });

    it("whitespace resource id throws", () => {
        expect(() => grantFormToWrite({subject: "user-a", action: "read", resourceType: "note", resourceId: "   "})).toThrow(/whitespace/i);
    });
});
