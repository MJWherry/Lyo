import type { Metadata } from "next";
import { PageHero } from "@/components/PageHero";
import { FeatureNav } from "@/components/FeatureNav";
import { CodeBlock } from "@/components/CodeBlock";
import { snippets } from "@/content/snippets";

export const metadata: Metadata = {
  title: "Temp IO",
};

export default function TempIoFeaturePage() {
  return (
    <>
      <PageHero
        kicker="Lyo.IO.Temp"
        title="Session-scoped scratch space"
        description="Named temp sessions with size limits, overflow cleanup, generators, and disposable file/dir lifetimes — used by Gateway uploads, report staging, and batch transforms."
      />
      <FeatureNav current="/features/temp-io" />
      <section className="section shell">
        <div className="panel" style={{ marginBottom: "1rem" }}>
          <h2 style={{ fontSize: "1.25rem" }}>Why it exists</h2>
          <p className="muted" style={{ marginBottom: 0 }}>
            Avoid ad-hoc <code>Path.GetTempFileName</code> leaks. Sessions enforce naming, track
            bytes, overflow policies (throw / delete oldest / largest), and clean up on dispose —
            with optional auto-cleanup hosted service and in-memory storage for tests/WASM.
          </p>
        </div>
        <div className="grid-2">
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Session lifecycle</h2>
            <CodeBlock code={snippets.tempIo} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>File generator</h2>
            <CodeBlock code={snippets.tempIoGenerator} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Options + auto-cleanup</h2>
            <CodeBlock code={snippets.tempIoOptions} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Directory specs</h2>
            <CodeBlock code={snippets.tempIoSpec} />
          </div>
        </div>
      </section>
    </>
  );
}
