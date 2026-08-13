"use server";

import { revalidatePath } from "next/cache";

/** Drop cached library / series pages after cover or metadata changes. */
export async function revalidateComicViews(slug?: string | null): Promise<void> {
  revalidatePath("/");
  revalidatePath("/search");
  revalidatePath("/manage");
  if (slug) revalidatePath(`/manga/${slug}`);
}
