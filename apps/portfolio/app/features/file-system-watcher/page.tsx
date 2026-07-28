import type { Metadata } from "next";
import { PageHero } from "@/components/PageHero";
import { FeatureNav } from "@/components/FeatureNav";
import { CodeBlock } from "@/components/CodeBlock";
import { snippets } from "@/content/snippets";

export const metadata: Metadata = {
  title: "File system watcher",
};

export default function FileSystemWatcherFeaturePage() {
  return (
    <>
      <PageHero
        kicker="Lyo.FileSystemWatcher"
        title="Reliable change detection"
        description="Snapshot-based monitoring with debouncing and hash-based move/rename detection — more dependable than raw OS FileSystemWatcher events alone. Metrics and structured logging optional."
      />
      <FeatureNav current="/features/file-system-watcher" />

      <section className="section shell">
        <div className="panel" style={{ marginBottom: "1rem" }}>
          <h2 style={{ fontSize: "1.25rem" }}>Why it exists</h2>
          <ul className="muted" style={{ margin: "0.5rem 0 0", paddingLeft: "1.2rem" }}>
            <li>Snapshot diffs catch missed or coalesced OS events</li>
            <li>Debounce batches rapid create/write storms</li>
            <li>Content hashing detects moves/renames when the FS does not</li>
            <li>Separate file vs directory events; thread-safe; cancelable</li>
          </ul>
        </div>
        <div className="grid-2">
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Basic watch</h2>
            <CodeBlock code={snippets.fileSystemWatcher} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Options + metrics</h2>
            <CodeBlock code={snippets.fileSystemWatcherOptions} />
          </div>
        </div>
      </section>
    </>
  );
}
