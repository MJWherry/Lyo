import { describe, expect, it } from "vitest";
import {
  NANO_DEFAULT_ALPHABET,
  NANO_DEFAULT_SIZE,
  autoIncrementIds,
  generateGuids,
  generateNanoIds,
  guidTimestampMs,
  resolveGuidNamespace,
} from "./idOptions.js";

describe("guid options", () => {
  it("generates the requested count and version", () => {
    for (const version of ["v4", "v6", "v7"] as const) {
      const ids = generateGuids(version, 3);
      expect(ids).toHaveLength(3);
      for (const id of ids) expect(id.value[14]).toBe(version[1]);
    }
  });

  it("name-based versions return one id for the namespace and name", () => {
    expect(generateGuids("v5", 5, { namespace: "DNS", name: "example.com" }).map((e) => e.value)).toEqual(["cfbff0d1-9375-5685-968c-48ce8b15ae17"]);
    expect(generateGuids("v3", 5, { namespace: "DNS", name: "example.com" }).map((e) => e.value)).toEqual(["9073926b-929f-31c2-abc9-fad77ae3e8eb"]);
  });

  it("resolves standard and custom namespaces", () => {
    expect(resolveGuidNamespace("URL")).toBe("6ba7b811-9dad-11d1-80b4-00c04fd430c8");
    expect(resolveGuidNamespace("OID")).toBe("6ba7b812-9dad-11d1-80b4-00c04fd430c8");
    expect(resolveGuidNamespace("X500")).toBe("6ba7b814-9dad-11d1-80b4-00c04fd430c8");
    expect(resolveGuidNamespace("Custom", "11111111-2222-3333-4444-555555555555")).toBe("11111111-2222-3333-4444-555555555555");
    expect(() => resolveGuidNamespace("Custom", "nope")).toThrow("Custom namespace is not a valid GUID.");
  });

  it("reads the v7 timestamp and annotates time-ordered entries", () => {
    expect(guidTimestampMs("01890a5d-ac96-774b-bcce-b302099a8057")).toBe(1688096058518);
    expect(guidTimestampMs("9073926b-929f-31c2-abc9-fad77ae3e8eb")).toBeNull();
    const [entry] = generateGuids("v7", 1);
    expect(entry.note).toMatch(/^≈ \d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} UTC$/);
    expect(generateGuids("v4", 1)[0].note).toBeUndefined();
  });
});

describe("nano options", () => {
  it("uses the alphabet and size", () => {
    const ids = generateNanoIds(10, "ab", 8);
    expect(ids).toHaveLength(10);
    for (const id of ids) expect(id.value).toMatch(/^[ab]{8}$/);
    expect(generateNanoIds(1)[0].value).toHaveLength(NANO_DEFAULT_SIZE);
    expect(NANO_DEFAULT_ALPHABET).toBe("_-0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
  });

  it("rejects an empty alphabet or a size below 1", () => {
    expect(() => generateNanoIds(1, "", 5)).toThrow("Alphabet is required.");
    expect(() => generateNanoIds(1, "ab", 0)).toThrow("Size must be at least 1.");
  });
});

describe("auto increment", () => {
  it("returns start+1 onward with the current value noted", () => {
    expect(autoIncrementIds("int", 41, 2)).toEqual([
      { value: "42", note: "current=42" },
      { value: "43", note: "current=43" },
    ]);
    expect(autoIncrementIds("long", 9007199254740993n, 1)[0].value).toBe("9007199254740994");
  });

  it("enforces each type's range", () => {
    expect(() => autoIncrementIds("int", 2147483648, 1)).toThrow("Start value must be between -2147483648 and 2147483647 for int.");
    expect(() => autoIncrementIds("uint", -1, 1)).toThrow("Start value must be between 0 and 4294967295 for uint.");
    expect(() => autoIncrementIds("ulong", -1, 1)).toThrow("Start value must be non-negative for ulong.");
  });
});
