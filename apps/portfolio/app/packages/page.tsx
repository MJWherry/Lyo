import type { Metadata } from "next";
import { redirect } from "next/navigation";

export const metadata: Metadata = {
  title: "Packages",
};

/** Catalog lives on Features; keep URL for old links. */
export default function PackagesPage() {
  redirect("/features");
}
