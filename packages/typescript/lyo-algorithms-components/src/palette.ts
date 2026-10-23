export const MATH_METHODS = ["Abs", "Min", "Max", "Round", "Floor", "Ceiling", "Pow", "Sqrt", "Sign"];
export const CONVERT_METHODS = ["ToDouble", "ToDecimal", "ToInt32", "ToInt64"];
export const DATE_MEMBERS = ["Year", "Month", "Day", "Hour", "Minute", "Second", "Date", "UtcDateTime"];
export const DATE_METHODS = ["AddDays", "AddMonths", "AddYears", "AddHours"];
export const TIMESPAN_MEMBERS = ["TotalDays", "TotalHours", "Days"];
export const COLLECTION_MEMBERS = ["Length", "Count"];

const SKIP = new Set([
  "Math", "Convert", "DateTime", "DateTimeOffset", "TimeSpan", "true", "false", "null",
  ...MATH_METHODS, ...CONVERT_METHODS, ...DATE_MEMBERS, ...DATE_METHODS, ...TIMESPAN_MEMBERS, ...COLLECTION_MEMBERS,
]);

export function paletteTokens(identifiers: string[], catalogKeys: string[]): string[] {
  return [
    ...identifiers,
    ...MATH_METHODS.map((name) => `Math.${name}`),
    ...CONVERT_METHODS.map((name) => `Convert.${name}`),
    ...DATE_MEMBERS.map((name) => `.${name}`),
    ...DATE_METHODS.map((name) => `.${name}(`),
    ...TIMESPAN_MEMBERS.map((name) => `.${name}`),
    ...COLLECTION_MEMBERS.map((name) => `.${name}`),
    ...catalogKeys.flatMap((key) => [`Algo("${key}")`, `score("${key}")`]),
  ];
}

/** Bare names left after quoted strings and known members are removed. Expression parsing stays on the server. */
export function bareNames(source: string): string[] {
  const stripped = source.replace(/"(?:\\.|[^"\\])*"/g, " ");
  const names = stripped.match(/[A-Za-z_][A-Za-z0-9_]*/g) ?? [];
  const found: string[] = [];
  for (const name of names) {
    if (SKIP.has(name) || found.includes(name)) continue;
    if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(name)) continue;
    found.push(name);
  }
  return found;
}

export function identifierAt(source: string, caret: number): string | null {
  const index = Math.min(Math.max(caret, 0), source.length);
  let start = index;
  while (start > 0 && isId(source[start - 1]!)) start--;
  let end = Math.min(index, source.length);
  while (end < source.length && isId(source[end]!)) end++;
  if (end <= start) return null;
  const name = source.slice(start, end);
  return /^[A-Za-z_][A-Za-z0-9_]*$/.test(name) ? name : null;
}

/** Caret completions. An empty prefix returns nothing. Members outside the allowlist, such as `Math.Atan`, are never offered. */
export function suggestions(prefix: string, identifiers: string[], catalogKeys: string[]): string[] {
  if (!prefix) return [];
  const items: string[] = [];
  for (const name of identifiers) {
    if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(name) || SKIP.has(name) || items.includes(name)) continue;
    if (matches(name, prefix)) items.push(name);
  }
  for (const token of allowlistTokens()) {
    if (matches(token, prefix) && !items.includes(token)) items.push(token);
  }
  for (const key of catalogKeys) {
    if (!key.trim()) continue;
    const algo = `Algo("${key}")`;
    const score = `score("${key}")`;
    if (matchesCall(algo, key, prefix)) items.push(algo);
    if (matchesCall(score, key, prefix)) items.push(score);
  }
  return items;
}

function allowlistTokens(): string[] {
  return [
    ...MATH_METHODS.map((name) => `Math.${name}`),
    ...CONVERT_METHODS.map((name) => `Convert.${name}`),
    ...DATE_MEMBERS.map((name) => `.${name}`),
    ...DATE_METHODS.map((name) => `.${name}(`),
    ...TIMESPAN_MEMBERS.map((name) => `.${name}`),
    ...COLLECTION_MEMBERS.map((name) => `.${name}`),
  ];
}

function matches(suggestion: string, prefix: string): boolean {
  return suggestion.toLowerCase().startsWith(prefix.toLowerCase()) || tail(suggestion).toLowerCase().startsWith(prefix.toLowerCase());
}

function matchesCall(suggestion: string, key: string, prefix: string): boolean {
  const token = prefix.toLowerCase();
  return suggestion.toLowerCase().startsWith(token) || key.toLowerCase().startsWith(token);
}

function tail(suggestion: string): string {
  const start = suggestion.lastIndexOf(".") + 1;
  const match = /^[A-Za-z_][A-Za-z0-9_]*/.exec(suggestion.slice(start));
  return match?.[0] ?? suggestion;
}

function isId(char: string): boolean {
  return /[A-Za-z0-9_]/.test(char);
}
