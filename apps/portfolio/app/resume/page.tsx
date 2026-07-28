import type { Metadata } from "next";
import { readFileSync } from "node:fs";
import path from "node:path";
import site from "@/content/site.json";

export const metadata: Metadata = {
  title: "Resume",
  description: `${site.fullName} — resume`,
};

function extractResumeParts(html: string): { styles: string; body: string } {
  const styleMatch = html.match(/<style[^>]*>([\s\S]*?)<\/style>/i);
  const bodyMatch = html.match(/<body[^>]*>([\s\S]*?)<\/body>/i);
  return {
    styles: styleMatch?.[1]?.trim() ?? "",
    body: (bodyMatch?.[1] ?? html).trim(),
  };
}

export default function ResumePage() {
  const html = readFileSync(path.join(process.cwd(), "public", "resume.html"), "utf8");
  const { styles, body } = extractResumeParts(html);

  return (
    <section className="section shell" style={{ paddingTop: "1.5rem" }}>
      <div className="panel resume-embed">
        <style dangerouslySetInnerHTML={{ __html: scopeResumeCss(styles) }} />
        <div
          className="resume-root"
          dangerouslySetInnerHTML={{ __html: body }}
        />
      </div>
    </section>
  );
}

/** Scope resume stylesheet so it doesn't fight the site chrome. */
function scopeResumeCss(css: string): string {
  const scoped = css
    .replace(/@page\s*\{[\s\S]*?\}/g, "")
    .replace(/(^|})\s*body\s*\{/g, "$1 .resume-root {")
    .replace(/(^|})\s*@media\s+print\s*\{\s*body\s*\{/g, "$1@media print { .resume-root {");

  return `
.resume-embed {
  background: #fff;
  color: #000;
  overflow-x: auto;
}
.resume-root {
  font-family: Arial, sans-serif;
  line-height: 1.3;
  color: #000;
  max-width: 8.5in;
  margin: 0 auto;
  padding: 0.5rem 0.25rem;
  font-size: 9pt;
}
${scoped}
`;
}
