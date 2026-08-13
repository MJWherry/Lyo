export type ReadingProgress = {
  seriesId: string;
  seriesSlug: string;
  seriesTitle: string;
  coverImageRef?: string | null;
  chapterId: string;
  chapterNumber: number;
  page: number;
  mode: "paged" | "vertical";
  updatedAt: number;
};

const KEY = "lyo-comic-continue";

export function loadProgress(): ReadingProgress[] {
  if (typeof window === "undefined") return [];
  try {
    const raw = window.localStorage.getItem(KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw) as ReadingProgress[];
    if (!Array.isArray(parsed)) return [];
    return parsed.sort((a, b) => b.updatedAt - a.updatedAt);
  } catch {
    return [];
  }
}

export function saveProgress(entry: ReadingProgress): void {
  if (typeof window === "undefined") return;
  const rest = loadProgress().filter((p) => p.seriesId !== entry.seriesId);
  const next = [entry, ...rest].slice(0, 12);
  window.localStorage.setItem(KEY, JSON.stringify(next));
}

export function progressForSeries(seriesId: string): ReadingProgress | undefined {
  return loadProgress().find((p) => p.seriesId === seriesId);
}

export function patchProgressCover(seriesId: string, coverImageRef: string | null | undefined): void {
  if (typeof window === "undefined") return;
  const next = loadProgress().map((p) =>
    p.seriesId === seriesId ? { ...p, coverImageRef: coverImageRef ?? null, updatedAt: Date.now() } : p
  );
  window.localStorage.setItem(KEY, JSON.stringify(next));
}
