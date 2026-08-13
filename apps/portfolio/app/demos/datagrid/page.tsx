import type { Metadata } from "next";
import { PageHero } from "@/components/PageHero";
import { PersonDataGridDemo } from "@/components/grid/PersonDataGridDemo";

export const metadata: Metadata = {
  title: "Person data grid",
};

export default function DataGridDemoPage() {
  return (
    <>
      <PageHero
        kicker="Demo"
        title="Person data grid"
        description="LyoDataGrid against Person — Root Query, QueryProject, and QueryConcrete. Search, filters, multi-sort, resizable columns, paging, and CSV/XLSX export."
      />
      <section className="section shell">
        <PersonDataGridDemo />
      </section>
    </>
  );
}
