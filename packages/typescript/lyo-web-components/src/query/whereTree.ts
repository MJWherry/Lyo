import type { ConditionClause, GroupClause, WhereClause } from "lyo-query";
import { toMultiValueStrings } from "lyo-query";

/** Steps from the root: a child index inside a group, or `"sub"` for a node's `subClause`. `[]` is the root. */
export type WherePath = readonly (number | "sub")[];

const FIELD_PATTERN = /^[a-zA-Z.]+$/;

export function fieldNameError(field: string | null | undefined): string | null {
  if (!field?.trim()) return "Field name is required.";
  return FIELD_PATTERN.test(field) ? null : "Only letters (a-z, A-Z) and dots are allowed.";
}

const blankCondition = (): ConditionClause => ({ $type: "condition", Field: "", Comparison: "Equals", Value: null });
const blankGroup = (): GroupClause => ({ $type: "group", Operator: "And", Children: [blankCondition()] });

export function nodeAt(root: WhereClause, path: WherePath): WhereClause | undefined {
  let node: WhereClause | undefined = root;
  for (const step of path) {
    if (!node) return undefined;
    node = step === "sub" ? node.subClause : node.$type === "group" ? node.Children[step] : undefined;
  }
  return node;
}

/** Replaces the node at `path` with `update(node)`, copying each ancestor. Returns `root` unchanged when the path does not resolve. */
export function updateAt(root: WhereClause, path: WherePath, update: (node: WhereClause) => WhereClause): WhereClause {
  if (path.length === 0) return update(root);
  const [step, ...rest] = path;
  if (step === "sub") return root.subClause ? { ...root, subClause: updateAt(root.subClause, rest, update) } : root;
  if (root.$type !== "group" || !root.Children[step]) return root;
  const Children = root.Children.slice();
  Children[step] = updateAt(Children[step], rest, update);
  return { ...root, Children };
}

export function addChildAt(root: WhereClause, groupPath: WherePath, kind: "condition" | "group"): WhereClause {
  return updateAt(root, groupPath, (node) =>
    node.$type === "group" ? { ...node, Children: [...node.Children, kind === "group" ? blankGroup() : blankCondition()] } : node
  );
}

export function addSubClauseAt(root: WhereClause, path: WherePath): WhereClause {
  return updateAt(root, path, (node) => ({ ...node, subClause: blankGroup() }));
}

export function removeSubClauseAt(root: WhereClause, path: WherePath): WhereClause {
  return updateAt(root, path, (node) => {
    const { subClause: _removed, ...rest } = node;
    return rest as WhereClause;
  });
}

/** Removes a group child, or clears the sub clause when the path ends in `"sub"`. The root cannot be removed. */
export function removeAt(root: WhereClause, path: WherePath): WhereClause {
  if (path.length === 0) return root;
  const parent = path.slice(0, -1);
  const last = path[path.length - 1];
  if (last === "sub") return removeSubClauseAt(root, parent);
  return updateAt(root, parent, (node) => (node.$type === "group" ? { ...node, Children: node.Children.filter((_, i) => i !== last) } : node));
}

function isPrefix(prefix: WherePath, path: WherePath): boolean {
  return prefix.length <= path.length && prefix.every((step, i) => step === path[i]);
}

/** Drag and drop: appends the node at `from` to the group at `target`. Returns `root` unchanged for moves into a condition, into the node itself, or of the root. */
export function moveInto(root: WhereClause, from: WherePath, target: WherePath): WhereClause {
  const node = nodeAt(root, from);
  if (from.length === 0 || !node || nodeAt(root, target)?.$type !== "group" || isPrefix(from, target)) return root;
  const parent = from.slice(0, -1);
  const index = from[from.length - 1];
  const adjusted = target.slice();
  const depth = parent.length;
  if (typeof index === "number" && isPrefix(parent, target) && typeof adjusted[depth] === "number" && (adjusted[depth] as number) > index)
    adjusted[depth] = (adjusted[depth] as number) - 1;
  return updateAt(removeAt(root, from), adjusted, (group) => (group.$type === "group" ? { ...group, Children: [...group.Children, node] } : group));
}

export function setDescriptionAt(root: WhereClause, path: WherePath, description: string): WhereClause {
  const trimmed = description.trim();
  return updateAt(root, path, (node) => {
    const { Description: _old, ...rest } = node;
    return (trimmed ? { ...rest, Description: trimmed } : rest) as WhereClause;
  });
}

function valueText(value: unknown): string {
  if (value == null) return "null";
  if (Array.isArray(value)) return `[${toMultiValueStrings(value).join(", ")}]`;
  return String(value);
}

/** Tree label: `And (2 conditions) - Description` or `Field Comparison value - Description`. */
export function nodeLabel(node: WhereClause): string {
  const description = node.Description?.trim() ? ` - ${node.Description.trim()}` : "";
  if (node.$type === "group") {
    const count = node.Children.length;
    return `${node.Operator} (${count} condition${count === 1 ? "" : "s"})${description}`;
  }
  return `${node.Field} ${node.Comparison} ${valueText(node.Value)}${description}`;
}
