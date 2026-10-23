export type LyoResultPhase = "loading" | "error" | "empty" | "content";

export type LyoResultError = { code?: string | null; description?: string | null };

/** Minimal result envelope (`Lyo.Result` JSON shape). */
export type LyoResultLike<T> = {
  isSuccess: boolean;
  data?: T | null;
  errors?: readonly LyoResultError[] | null;
};

function isDataEmpty(data: unknown): boolean {
  if (data == null) return true;
  if (typeof data === "string") return data.length === 0;
  if (Array.isArray(data)) return data.length === 0;
  return false;
}

/** Loading (or no result yet) wins, then failure, then empty data, then content. */
export function resolveResultPhase<T>(options: {
  result: LyoResultLike<T> | null | undefined;
  loading?: boolean;
  isEmpty?: (data: T) => boolean;
}): LyoResultPhase {
  const { result, loading, isEmpty } = options;
  if (loading || !result) return "loading";
  if (!result.isSuccess) return "error";
  if (result.data == null) return "empty";
  return (isEmpty ? isEmpty(result.data) : isDataEmpty(result.data)) ? "empty" : "content";
}

/** One line per error (description, else code). */
export function formatResultErrors(errors: readonly LyoResultError[] | null | undefined): string {
  const lines = (errors ?? []).map((e) => e.description || e.code).filter((t): t is string => Boolean(t));
  return lines.length > 0 ? lines.join("\n") : "The operation failed.";
}
