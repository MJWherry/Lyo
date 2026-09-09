import { describe, expect, it } from "vitest";

function buildPatchRequest(
  changes: Record<string, { propertyName: string; currentValue: unknown; hasChanged: boolean }>,
  key: unknown
) {
  const entries = Object.values(changes).filter((c) => c.hasChanged);
  if (entries.length === 0) return null;
  const data: Record<string, unknown> = {};
  for (const e of entries) data[e.propertyName] = e.currentValue;
  return { keys: Array.isArray(key) ? key : [key], data };
}

describe("buildPatchRequest", () => {
  it("returns null when nothing changed", () => {
    expect(buildPatchRequest({}, "id-1")).toBeNull();
  });

  it("includes only changed properties", () => {
    const patch = buildPatchRequest(
      {
        FirstName: { propertyName: "FirstName", currentValue: "Ann", hasChanged: true },
      },
      "abc"
    );
    expect(patch).toEqual({ keys: ["abc"], data: { FirstName: "Ann" } });
  });
});
