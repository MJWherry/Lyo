"use server";

import { revalidatePath } from "next/cache";

/** Drop cached library / series pages after cover or metadata changes. */
export async function revalidateComicViews(seriesId?: string | null, slug?: string | null): Promise<void> {
  revalidatePath("/");
  revalidatePath("/search");
  revalidatePath("/manage");
  if (seriesId) revalidatePath(`/manga/${seriesId}`);
  if (slug) revalidatePath(`/manga/${slug}`);
}
