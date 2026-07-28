import type { Metadata } from "next";
import { PageHero } from "@/components/PageHero";
import { FeatureNav } from "@/components/FeatureNav";
import { CodeBlock } from "@/components/CodeBlock";
import { snippets } from "@/content/snippets";

export const metadata: Metadata = {
  title: "File storage",
};

export default function FileStorageFeaturePage() {
  return (
    <>
      <PageHero
        kicker="Lyo.FileStorage"
        title="One API, many backends"
        description="Local, S3, and Azure Blob providers share save/stream/copy/download, staged upload, multipart, duplicate detection, and an optional compress+encrypt pipeline."
      />
      <FeatureNav current="/features/file-storage" />
      <section className="section shell">
        <div className="panel" style={{ marginBottom: "1rem" }}>
          <h2 style={{ fontSize: "1.25rem" }}>Workbench surface</h2>
          <p className="muted" style={{ marginBottom: 0 }}>
            TestApi exposes <code>Workbench/FileStorage/*</code>: save, stream, download, presign,
            DEK migrate/rotate, direct-upload, stage, multipart, keys, diagnostics, plus FileMetadata
            Query.
          </p>
        </div>
        <div className="grid-2">
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Save with compress + encrypt</h2>
            <CodeBlock code={snippets.fileStorage} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Staged upload</h2>
            <CodeBlock code={snippets.fileStorageStage} />
          </div>
        </div>
      </section>
    </>
  );
}
