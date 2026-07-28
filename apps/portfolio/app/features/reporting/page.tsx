import type { Metadata } from "next";
import { PageHero } from "@/components/PageHero";
import { FeatureNav } from "@/components/FeatureNav";
import { CodeBlock } from "@/components/CodeBlock";
import { snippets } from "@/content/snippets";

export const metadata: Metadata = {
  title: "Reporting",
};

export default function ReportingFeaturePage() {
  return (
    <>
      <PageHero
        kicker="Lyo.Reporting"
        title="Definitions through generated artifacts"
        description="Composable report models, PostgreSQL persistence, multi-format generation (HTML/PDF/CSV/XLSX/JSON), concurrency throttling, retention cleanup, and MudBlazor management — wired next to the same API/query stack as Jobs."
      />
      <FeatureNav current="/features/reporting" />

      <section className="section shell">
        <div className="panel" style={{ marginBottom: "1rem" }}>
          <h2 style={{ fontSize: "1.25rem" }}>Pipeline</h2>
          <ol className="muted" style={{ paddingLeft: "1.2rem", margin: "0.5rem 0 0" }}>
            <li>
              <strong style={{ color: "var(--ink)" }}>Definition</strong> — saved composition +
              parameters (<code>ReportDefinition</code>), optional generation profile key
            </li>
            <li>
              <strong style={{ color: "var(--ink)" }}>Generate</strong> — merge params, optional
              data provider, render via <code>IReportRenderer</code>, stage output
            </li>
            <li>
              <strong style={{ color: "var(--ink)" }}>Hooks</strong> — persist staged files to
              FileStorage in <code>AfterRenderAsync</code>; delete on{" "}
              <code>OnCleanupAsync</code>
            </li>
            <li>
              <strong style={{ color: "var(--ink)" }}>Hardening</strong> — ad-hoc generate gate,
              max concurrent generations (<code>ReportBusyException</code> → 503), retention
              service
            </li>
            <li>
              <strong style={{ color: "var(--ink)" }}>API + UI</strong> —{" "}
              <code>BuildReportingGroup</code> + Blazor reporting components
            </li>
          </ol>
          <p className="muted" style={{ marginTop: "1rem", marginBottom: 0 }}>
            Packages: <code>Lyo.Reporting.Models</code>, <code>.Postgres</code>,{" "}
            <code>.Client</code>, <code>.Web</code>, <code>.Web.Components</code>,{" "}
            <code>Lyo.Api.Reporting</code>.
          </p>
        </div>

        <div className="grid-2">
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Host registration</h2>
            <CodeBlock code={snippets.reportingRegister} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Generate + hooks</h2>
            <CodeBlock code={snippets.reportingHooks} />
          </div>
        </div>
      </section>
    </>
  );
}
