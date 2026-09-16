/** Prepends 1-based line numbers padded to the widest number, as plain text so a browser selection copies them too. */
export function withLineNumbers(code: string | null | undefined): string {
  if (!code) return "";
  const lines = code.replace(/\r\n/g, "\n").split("\n");
  const width = String(lines.length).length;
  return lines.map((line, i) => `${String(i + 1).padStart(width)}  ${line}`).join("\n");
}
