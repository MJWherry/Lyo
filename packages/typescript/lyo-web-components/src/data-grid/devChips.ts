import { chipOf, type LyoChipColor, type LyoChipSpec } from "../display/chips.js";
import { durationColor, formatDuration } from "../time/duration.js";

/** Metrics the grid records from its last query for the Dev menu. */
export type LyoGridQueryMetrics = {
  elapsedMs?: number | null;
  statusCode?: number | null;
  itemCount?: number | null;
  total?: number | null;
  queryScore?: number | null;
  hasMore?: boolean | null;
};

function statusCodeColor(code: number): LyoChipColor {
  if (code >= 200 && code < 300) return "success";
  if (code >= 400 && code < 500) return "warning";
  if (code >= 500) return "error";
  return "default";
}

/** Chips for the Dev "View Response" dialog: elapsed, HTTP status, count / total, score, has-more. */
export function responseDevChips(metrics: LyoGridQueryMetrics, locale?: string): LyoChipSpec[] {
  const { elapsedMs, statusCode, itemCount, total, queryScore, hasMore } = metrics;
  const count = (n: number | null | undefined) => (n == null ? "—" : n.toLocaleString(locale));
  const chips: LyoChipSpec[] = [];
  if (elapsedMs != null) chips.push(chipOf(formatDuration(elapsedMs), { color: durationColor(elapsedMs), icon: "timer" }));
  if (statusCode != null) chips.push(chipOf(String(statusCode), { color: statusCodeColor(statusCode) }));
  if (itemCount != null || total != null) chips.push(chipOf(`${count(itemCount)} / ${count(total)}`));
  if (queryScore != null) chips.push(chipOf(`Score ${queryScore}`));
  if (hasMore) chips.push(chipOf("Has more", { color: "info" }));
  return chips;
}
