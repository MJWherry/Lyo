import type { Metadata } from "next";
import { PageHero } from "@/components/PageHero";
import { ComponentsGallery } from "@/components/gallery/ComponentsGallery";

export const metadata: Metadata = {
  title: "Component gallery",
};

export default function ComponentsDemoPage() {
  return (
    <>
      <PageHero
        kicker="Demo"
        title="Lyo web components"
        description="MUI primitives, change-tracking form, file upload, rich text, JSON editor, text diff, and ID workbench."
      />
      <section className="section shell">
        <ComponentsGallery />
      </section>
    </>
  );
}
