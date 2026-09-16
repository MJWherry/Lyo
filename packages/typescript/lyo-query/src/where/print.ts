import type { ConditionClause, GroupClause, WhereClause } from "../models/whereClause.js";
import { thresholdSuffix } from "./helpers.js";

const pad = (indent: number) => " ".repeat(indent * 2);

function printCondition(c: ConditionClause, indent: number): string {
  const p = pad(indent);
  if (Array.isArray(c.Value)) {
    const items = c.Value.map((v) => (v == null ? null : String(v)));
    if (items.length === 0) return `${p}${c.Field} ${c.Comparison} []`;
    if (items.length === 1) return `${p}${c.Field} ${c.Comparison} '${items[0]}'`;
    const lines = items.map((i) => `${pad(indent + 1)}- ${i ?? "NULL"}`);
    return `${p}${c.Field} ${c.Comparison} [\n${lines.join("\n")}\n${p}]`;
  }
  return `${p}${c.Field} ${c.Comparison} ${c.Value != null ? `'${String(c.Value)}'` : "NULL"}${thresholdSuffix(c)}`;
}

function leadingSpaces(line: string): number {
  return line.length - line.trimStart().length;
}

function printGroup(g: GroupClause, indent: number): string {
  const p = pad(indent);
  const op = g.Operator.toUpperCase();
  if (g.Children.length === 0) return `${p}(${op})`;
  if (g.Children.length === 1) return printAt(g.Children[0]!, indent);
  const lines = [`${p}(`];
  g.Children.forEach((child, i) => {
    const childLines = printAt(child, indent + 1).split("\n");
    if (i === 0) {
      lines.push(...childLines);
      return;
    }
    const first = childLines[0] ?? "";
    const leading = leadingSpaces(first);
    const opPad = " ".repeat(leading);
    const rest = first.slice(leading);
    lines.push(opPad + op + (rest ? ` ${rest}` : ""));
    for (const line of childLines.slice(1)) lines.push(opPad + (leadingSpaces(line) > leading ? line.slice(leading) : line.trimStart()));
  });
  lines.push(`${p})`);
  return lines.join("\n");
}

function printAt(clause: WhereClause, indent: number): string {
  return clause.$type === "group" ? printGroup(clause, indent) : printCondition(clause, indent);
}

/** Readable, indented rendering of a where clause (port of `WhereClause.Print`). Empty string for no clause. */
export function printWhere(clause: WhereClause | null | undefined, indent = 0): string {
  return clause ? printAt(clause, indent) : "";
}
