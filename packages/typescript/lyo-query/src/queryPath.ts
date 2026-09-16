/** Shown when a projection select or aggregate field is not a query path. */
export const QUERY_PATH_MESSAGE =
  "Only letters (a-z, A-Z), dots, and a window ([n], [start:], [:end], [start:end], [:]) are allowed.";

const NAME = /^[A-Za-z]+$/;
const NON_NEGATIVE = /^\d+$/;

/**
 * Null when `path` is a property path. A segment may end with `[n]`, `[start:]`, `[:end]`, `[start:end]`, or `[:]`.
 * `start` is inclusive, `end` is exclusive, and a present bound is a non-negative integer. Filters and root `/Query` selects do not use this.
 */
export function queryPathError(path: string | null | undefined): string | null {
  const text = path?.trim() ?? "";
  if (!text) return "Field name is required.";
  const segments = text.split(".");
  for (const segment of segments) {
    const reason = segmentError(segment);
    if (reason) return reason;
  }
  return null;
}

function segmentError(segment: string): string | null {
  const open = segment.indexOf("[");
  if (open < 0) return NAME.test(segment) && !segment.includes("]") ? null : QUERY_PATH_MESSAGE;
  if (open === 0 || segment.indexOf("]") !== segment.length - 1 || segment.indexOf("[", open + 1) >= 0) return QUERY_PATH_MESSAGE;
  if (!NAME.test(segment.slice(0, open))) return QUERY_PATH_MESSAGE;
  return windowError(segment.slice(open + 1, -1));
}

function windowError(body: string): string | null {
  if (body.length === 0 || body.includes("[")) return "is not a valid range.";
  const colon = body.indexOf(":");
  if (colon < 0) return NON_NEGATIVE.test(body) ? null : "is not a valid range.";
  if (body.indexOf(":", colon + 1) >= 0) return "is not a valid range.";

  const startText = body.slice(0, colon);
  const endText = body.slice(colon + 1);
  let start = 0;
  if (startText.length > 0) {
    if (!NON_NEGATIVE.test(startText)) return "is not a valid range.";
    start = Number(startText);
  }
  if (endText.length === 0) return null;
  if (!NON_NEGATIVE.test(endText) || Number(endText) <= start) return "is not a valid range.";
  return null;
}
