import { customAlphabet } from "nanoid";
import { v3 as uuidv3, v4 as uuidv4, v5 as uuidv5, v6 as uuidv6, v7 as uuidv7 } from "uuid";

/** A generated id plus an optional note (embedded timestamp, counter state). */
export type IdEntry = { value: string; note?: string };

export type GuidVersion = "v3" | "v4" | "v5" | "v6" | "v7";
export type GuidNamespaceKind = "DNS" | "URL" | "OID" | "X500" | "Custom";

const GUID_NAMESPACES: Record<Exclude<GuidNamespaceKind, "Custom">, string> = {
  DNS: "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
  URL: "6ba7b811-9dad-11d1-80b4-00c04fd430c8",
  OID: "6ba7b812-9dad-11d1-80b4-00c04fd430c8",
  X500: "6ba7b814-9dad-11d1-80b4-00c04fd430c8",
};

export const NANO_DEFAULT_ALPHABET = "_-0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
export const NANO_DEFAULT_SIZE = 21;

const GUID_FORMAT = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function resolveGuidNamespace(kind: GuidNamespaceKind, custom?: string): string {
  if (kind !== "Custom") return GUID_NAMESPACES[kind];
  const trimmed = custom?.trim() ?? "";
  if (!GUID_FORMAT.test(trimmed)) throw new Error("Custom namespace is not a valid GUID.");
  return trimmed;
}

/** Unix ms embedded in a v7 GUID; null for other versions. */
export function guidTimestampMs(id: string): number | null {
  if (id[14] !== "7") return null;
  return parseInt(id.replace(/-/g, "").slice(0, 12), 16);
}

/** Unix ms embedded in a v6 GUID (Gregorian 100 ns ticks since 1582-10-15); null for other versions. */
function v6TimestampMs(id: string): number | null {
  if (id[14] !== "6") return null;
  const hex = id.replace(/-/g, "");
  const ticks = BigInt(`0x${hex.slice(0, 12)}${hex.slice(13, 16)}`);
  return Number((ticks - 122192928000000000n) / 10000n);
}

const utcNote = (ms: number) => `≈ ${new Date(ms).toISOString().replace("T", " ").replace("Z", "")} UTC`;

/** V3 / V5 are name-based and return one id; the others return `count` ids, with the embedded time noted for V6 / V7. */
export function generateGuids(version: GuidVersion, count: number, named: { namespace?: GuidNamespaceKind; customNamespace?: string; name?: string } = {}): IdEntry[] {
  if (version === "v3" || version === "v5") {
    const ns = resolveGuidNamespace(named.namespace ?? "DNS", named.customNamespace);
    const name = named.name ?? "";
    return [{ value: version === "v3" ? uuidv3(name, ns) : uuidv5(name, ns) }];
  }
  const make: () => string = version === "v4" ? () => uuidv4() : version === "v6" ? () => uuidv6() : () => uuidv7();
  return Array.from({ length: Math.max(0, count) }, () => {
    const value = make();
    const ms = version === "v7" ? guidTimestampMs(value) : version === "v6" ? v6TimestampMs(value) : null;
    return ms == null ? { value } : { value, note: utcNote(ms) };
  });
}

export function generateNanoIds(count: number, alphabet: string = NANO_DEFAULT_ALPHABET, size: number = NANO_DEFAULT_SIZE): IdEntry[] {
  if (!alphabet) throw new Error("Alphabet is required.");
  if (size < 1) throw new Error("Size must be at least 1.");
  const next = customAlphabet(alphabet, size);
  return Array.from({ length: Math.max(0, count) }, () => ({ value: next() }));
}

export type AutoIncrementType = "int" | "long" | "uint" | "ulong";

const RANGES: Record<AutoIncrementType, [bigint, bigint]> = {
  int: [-2147483648n, 2147483647n],
  uint: [0n, 4294967295n],
  long: [-9223372036854775808n, 9223372036854775807n],
  ulong: [0n, 18446744073709551615n],
};

/** Mirrors `AutoIncrementIdGenerator<T>`: the counter starts at `start`, so the first id is `start + 1`. */
export function autoIncrementIds(type: AutoIncrementType, start: number | bigint, count: number): IdEntry[] {
  const begin = BigInt(start);
  const [min, max] = RANGES[type];
  if (type === "ulong" && begin < 0n) throw new Error("Start value must be non-negative for ulong.");
  if (begin < min || begin > max) throw new Error(`Start value must be between ${min} and ${max} for ${type}.`);
  return Array.from({ length: Math.max(0, count) }, (_, i) => {
    const value = (begin + BigInt(i + 1)).toString();
    return { value, note: `current=${value}` };
  });
}
