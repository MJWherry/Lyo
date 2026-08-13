import type { ComicPage } from "lyo-comic-api-client";
import { bffFetch } from "@/lib/api/bffFetch";

export function parsePageList(raw: unknown): ComicPage[] {
  if (Array.isArray(raw)) return raw as ComicPage[];
  if (raw && typeof raw === "object" && Array.isArray((raw as { data?: unknown }).data)) {
    return (raw as { data: ComicPage[] }).data;
  }
  return [];
}

export async function fetchChapterPages(chapterId: string): Promise<ComicPage[]> {
  const res = await bffFetch(`/api/comic/chapters/${encodeURIComponent(chapterId)}/pages`);
  if (!res.ok) return [];
  return parsePageList(await res.json());
}

export async function collectPageImageRefs(chapterIds: string[]): Promise<Set<string>> {
  const refs = new Set<string>();
  const unique = [...new Set(chapterIds.filter(Boolean))];
  const lists = await Promise.all(unique.map((id) => fetchChapterPages(id)));
  for (const pages of lists) {
    for (const page of pages) {
      if (page.imageRef) refs.add(page.imageRef);
    }
  }
  return refs;
}

export async function deleteCoverFileIfOrphan(ref: string | null | undefined, keep: string | null | undefined, pageRefs: Set<string>) {
  if (!ref || ref === keep || /^https?:\/\//i.test(ref) || pageRefs.has(ref)) return;
  await bffFetch(`/api/files/${encodeURIComponent(ref)}`, { method: "DELETE" }).catch(() => undefined);
}
