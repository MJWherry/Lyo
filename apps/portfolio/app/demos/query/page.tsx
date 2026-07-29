import type { Metadata } from "next";
import { PageHero } from "@/components/PageHero";
import { QueryDemo } from "@/components/query/QueryDemo";

export const metadata: Metadata = {
  title: "Query builder",
};

export default function QueryDemoPage() {
  return (
    <>
      <PageHero
        kicker="Demo"
        title="Person query builder"
        description="Build Concrete, Projection, root Query, or Get requests — where clauses, chips for In/NotIn, paging and select — then run through the BFF. The browser never picks hosts or endpoints."
      />
      <section className="section shell">
        <QueryDemo />
      </section>
    </>
  );
}
