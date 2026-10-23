import type { LyoTypeCategory } from "lyo-query";

export type { LyoTypeCategory };

/** Which input widget a type renders (mirrors the server `LyoTypeEditorKind` enum). */
export type LyoTypeEditorKind =
  | "Text"
  | "Boolean"
  | "Integer"
  | "Decimal"
  | "DateTime"
  | "DateOnly"
  | "TimeOnly"
  | "Duration"
  | "Guid"
  | "Uri"
  | "Enum"
  | "JsonObject"
  | "JsonArray"
  | "Collection"
  | "Binary"
  | "Regex"
  | "Xml"
  | "Formatter";

export const LYO_TYPE_CATEGORIES: readonly LyoTypeCategory[] = [
  "Unknown",
  "Text",
  "Boolean",
  "Integer",
  "Number",
  "Temporal",
  "Identifier",
  "Enum",
  "Json",
  "Collection",
  "Binary",
];

/** Port of the server `LyoTypeInfo` record. `fullName` is the stored name: a CLR FullName when `isClr`, otherwise a package-owned id. */
export type LyoTypeInfo = {
  shortName: string;
  fullName: string;
  description: string;
  category: LyoTypeCategory;
  editorKind: LyoTypeEditorKind;
  /** False for JSON documents, collections, and binary payloads that need a JSON editor. */
  isScalar: boolean;
  /** Empty/zero JSON payload for new values. */
  defaultJson: string;
  aliases: readonly string[];
  /** CLR `Type.Name` (`Int32`, `XDocument`), also a lookup alias. */
  clrName: string | null;
  /** Element FullName for lists and arrays. */
  elementFullName: string | null;
  isArray: boolean;
  isClr: boolean;
};

const CORE_LIB = "System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e";
const ELEMENT_ASSEMBLIES: Record<string, string> = {
  "System.Uri": "System.Private.Uri, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
};

/** CLR FullName of `List<T>` for an element FullName (assembly-qualified like the runtime writes it). */
export function clrListFullName(elementFullName: string): string {
  return `System.Collections.Generic.List\`1[[${elementFullName}, ${ELEMENT_ASSEMBLIES[elementFullName] ?? CORE_LIB}]]`;
}

type Row = Omit<LyoTypeInfo, "aliases" | "clrName" | "elementFullName" | "isArray" | "isClr" | "isScalar"> &
  Partial<Pick<LyoTypeInfo, "aliases" | "clrName" | "elementFullName" | "isArray" | "isClr" | "isScalar">>;

function row(r: Row): LyoTypeInfo {
  return { aliases: [], clrName: null, elementFullName: null, isArray: false, isClr: true, isScalar: true, ...r };
}

function scalar(shortName: string, fullName: string, description: string, category: LyoTypeCategory, editorKind: LyoTypeEditorKind, defaultJson: string, aliases: string[] = []) {
  return row({ shortName, fullName, description, category, editorKind, defaultJson, aliases, clrName: fullName.slice(fullName.lastIndexOf(".") + 1) });
}

function list(element: string, elementFullName: string, description: string) {
  return row({
    shortName: `List<${element}>`,
    fullName: clrListFullName(elementFullName),
    description,
    category: "Collection",
    editorKind: "Collection",
    defaultJson: "[]",
    isScalar: false,
    elementFullName,
    aliases: [`list<${element.toLowerCase()}>`, `${element.toLowerCase()} list`],
  });
}

function array(element: string, elementFullName: string, description: string, aliases: string[] = []) {
  return row({
    shortName: `${element}[]`,
    fullName: `${elementFullName}[]`,
    description,
    category: "Collection",
    editorKind: "Collection",
    defaultJson: "[]",
    isScalar: false,
    elementFullName,
    isArray: true,
    clrName: `${elementFullName.slice(elementFullName.lastIndexOf(".") + 1)}[]`,
    aliases,
  });
}

/** Built-in rows, same order and payloads as the server catalog. */
export const LyoTypes = {
  Unknown: row({ shortName: "unknown", fullName: "System.Object", description: "Unknown or unregistered CLR type", category: "Unknown", editorKind: "Text", defaultJson: "null", isScalar: false, aliases: ["unknown", "unspecified"] }),
  String: scalar("string", "System.String", "UTF-16 string", "Text", "Text", '""'),
  Uri: scalar("Uri", "System.Uri", "Absolute or relative URI", "Text", "Uri", '"http://localhost"', ["uri", "url"]),
  Bool: scalar("bool", "System.Boolean", "Boolean true/false", "Boolean", "Boolean", "false", ["boolean", "Bool"]),
  Byte: scalar("byte", "System.Byte", "Unsigned 8-bit integer", "Integer", "Integer", "0"),
  Short: scalar("short", "System.Int16", "Signed 16-bit integer", "Integer", "Integer", "0", ["int16"]),
  Int: scalar("int", "System.Int32", "Signed 32-bit integer", "Integer", "Integer", "0", ["int32", "Int"]),
  Long: scalar("long", "System.Int64", "Signed 64-bit integer", "Integer", "Integer", "0", ["int64", "Long"]),
  Float: scalar("float", "System.Single", "IEEE 754 single-precision floating point", "Number", "Decimal", "0", ["single"]),
  Double: scalar("double", "System.Double", "IEEE 754 double-precision floating point", "Number", "Decimal", "0"),
  Decimal: scalar("decimal", "System.Decimal", "128-bit decimal number", "Number", "Decimal", "0"),
  Guid: scalar("Guid", "System.Guid", "128-bit globally unique identifier", "Identifier", "Guid", '"00000000-0000-0000-0000-000000000000"', ["uuid"]),
  Enum: scalar("enum", "System.Enum", "Enumeration (name or numeric value). Store the concrete enum FullName, not System.Enum.", "Enum", "Enum", "0"),
  DateTime: scalar("DateTime", "System.DateTime", "Date and time of day", "Temporal", "DateTime", '"0001-01-01T00:00:00"'),
  DateTimeOffset: scalar("DateTimeOffset", "System.DateTimeOffset", "Date and time with UTC offset", "Temporal", "DateTime", '"0001-01-01T00:00:00+00:00"'),
  DateOnly: scalar("DateOnly", "System.DateOnly", "Calendar date without time", "Temporal", "DateOnly", '"0001-01-01"'),
  TimeOnly: scalar("TimeOnly", "System.TimeOnly", "Time of day without date", "Temporal", "TimeOnly", '"00:00:00"'),
  TimeSpan: scalar("TimeSpan", "System.TimeSpan", "Time duration", "Temporal", "Duration", '"00:00:00"', ["duration"]),
  Regex: scalar("regex", "System.Text.RegularExpressions.Regex", "Regular-expression pattern", "Text", "Regex", '""', ["Regex", "regexp"]),
  Xml: scalar("xml", "System.Xml.Linq.XDocument", "XML document", "Json", "Xml", '"<root/>"', ["Xml", "XDocument"]),
  JsonObject: row({ ...scalar("JSON object", "System.Text.Json.Nodes.JsonObject", "JSON object document", "Json", "JsonObject", "{}", ["json object"]), isScalar: false }),
  JsonArray: row({ ...scalar("JSON array", "System.Text.Json.Nodes.JsonArray", "JSON array document", "Json", "JsonArray", "[]", ["json array"]), isScalar: false }),
  JsonNode: row({ ...scalar("JSON node", "System.Text.Json.Nodes.JsonNode", "Any JSON node (object, array, or value)", "Json", "JsonObject", "{}", ["json node", "json"]), isScalar: false }),
  StringList: list("string", "System.String", "List of strings"),
  IntList: list("int", "System.Int32", "List of 32-bit integers"),
  LongList: list("long", "System.Int64", "List of 64-bit integers"),
  GuidList: list("Guid", "System.Guid", "List of GUIDs"),
  StringArray: array("string", "System.String", "Array of strings", ["string array"]),
  IntArray: array("int", "System.Int32", "Array of 32-bit integers", ["int array"]),
  ByteArray: row({ ...array("byte", "System.Byte", "Binary payload (JSON base64)", ["bytes", "binary"]), category: "Binary", editorKind: "Binary", defaultJson: '""' }),
} as const satisfies Record<string, LyoTypeInfo>;

const catalog: LyoTypeInfo[] = [];
const byFullName = new Map<string, LyoTypeInfo>();
const byAlias = new Map<string, LyoTypeInfo>();
const synthesized = new Map<string, LyoTypeInfo>();

const aliasKey = (name: string) => name.trim().toLowerCase();

function index(info: LyoTypeInfo) {
  catalog.push(info);
  byFullName.set(info.fullName, info);
  for (const alias of [info.shortName, info.clrName, ...info.aliases]) {
    if (alias && !byAlias.has(aliasKey(alias))) byAlias.set(aliasKey(alias), info);
  }
}

for (const info of Object.values(LyoTypes)) if (info !== LyoTypes.Unknown) index(info);

/** Registered rows excluding Unknown, in declaration then registration order. */
export function allLyoTypes(): readonly LyoTypeInfo[] {
  return catalog;
}

export function lyoTypesByCategory(category: LyoTypeCategory): LyoTypeInfo[] {
  return catalog.filter((t) => t.category === category);
}

export function isLyoCollection(info: LyoTypeInfo): boolean {
  return info.elementFullName != null;
}

const CLR_LIST = /^System\.Collections\.Generic\.List`1\[\[([^,\]]+)/;
const SHORT_LIST = /^List<(.+)>$/i;

function collectionElement(name: string): { element: string; isArray: boolean } | null {
  const clr = CLR_LIST.exec(name) ?? SHORT_LIST.exec(name);
  if (clr) return { element: clr[1]!.trim(), isArray: false };
  return name.endsWith("[]") ? { element: name.slice(0, -2).trim(), isArray: true } : null;
}

function synthesizeCollection(name: string): LyoTypeInfo | null {
  const parsed = collectionElement(name);
  if (!parsed) return null;
  const element = lyoTypeFromName(parsed.element);
  if (element === LyoTypes.Unknown || element === LyoTypes.Enum || isLyoCollection(element)) return null;
  const fullName = parsed.isArray ? `${element.fullName}[]` : clrListFullName(element.fullName);
  const registered = byFullName.get(fullName);
  if (registered) return registered;
  const cached = synthesized.get(fullName);
  if (cached) return cached;
  const info = row({
    shortName: parsed.isArray ? `${element.shortName}[]` : `List<${element.shortName}>`,
    fullName,
    description: parsed.isArray ? `Array of ${element.shortName}` : `List of ${element.shortName}`,
    category: "Collection",
    editorKind: "Collection",
    defaultJson: "[]",
    isScalar: false,
    elementFullName: element.fullName,
    isArray: parsed.isArray,
  });
  synthesized.set(fullName, info);
  return info;
}

/** Full name (exact), then short name / CLR name / alias (case-insensitive), then a synthesized `List<T>` / `T[]` of a known element. Unknown otherwise. */
export function lyoTypeFromName(name: string | null | undefined): LyoTypeInfo {
  const trimmed = name?.trim();
  if (!trimmed) return LyoTypes.Unknown;
  return byFullName.get(trimmed) ?? byAlias.get(aliasKey(trimmed)) ?? synthesizeCollection(trimmed) ?? LyoTypes.Unknown;
}

export type LyoTypeRegistration = {
  shortName: string;
  fullName: string;
  description: string;
  category: LyoTypeCategory;
  editorKind: LyoTypeEditorKind;
  defaultJson: string;
  isClr?: boolean;
  isScalar?: boolean;
  aliases?: readonly string[];
};

/** Adds a row from another package (for example a formatter template type). Idempotent on `fullName`. */
export function registerLyoType(registration: LyoTypeRegistration): LyoTypeInfo {
  const existing = byFullName.get(registration.fullName);
  if (existing) return existing;
  const info = row({ ...registration, isClr: registration.isClr ?? false, aliases: registration.aliases ?? [] });
  index(info);
  return info;
}
