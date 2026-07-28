import type { Metadata } from "next";
import { PageHero } from "@/components/PageHero";
import { QueryDemo } from "@/components/query/QueryDemo";

export const metadata: Metadata = {
  title: "Query demo",
};

export default function QueryDemoPage() {
  return (
    <>
      <PageHero
        kicker="Demo"
        title="Person QueryConcrete"
        description="Build a where clause, POST through the BFF, and page Person rows from TestApi. The browser never calls the API host directly."
      />
      <section className="section shell">
        <QueryDemo />
      </section>
    </>
  );
}
