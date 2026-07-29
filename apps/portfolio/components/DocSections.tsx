import { CodeBlock } from "./CodeBlock";
import { inlineFormat } from "@/lib/catalog/inlineFormat";
import type { DocSection } from "@/lib/catalog/types";

type MdBlock =
  | { type: "table"; content: string }
  | { type: "code"; language: string; code: string }
  | { type: "pre"; content: string };

type MdGroup =
  | { type: "single"; block: MdBlock }
  | { type: "codes"; items: Array<Extract<MdBlock, { type: "code" }>> };

function MarkdownBlock({ body }: { body: string }) {
  // Render tables, fenced code, and prose in document order.
  const lines = body.replace(/\r\n/g, "\n").split("\n");
  const blocks: MdBlock[] = [];
  let i = 0;
  while (i < lines.length) {
    const fence = lines[i].match(/^```([^\n`]*)\s*$/);
    if (fence) {
      const language = (fence[1] || "csharp").trim() || "csharp";
      i++;
      const code: string[] = [];
      while (i < lines.length && !lines[i].startsWith("```")) {
        code.push(lines[i]);
        i++;
      }
      if (i < lines.length) i++; // closing ```
      blocks.push({ type: "code", language, code: code.join("\n") });
      continue;
    }
    if (lines[i].trim().startsWith("|")) {
      const tableLines: string[] = [];
      while (i < lines.length && lines[i].trim().startsWith("|")) {
        tableLines.push(lines[i]);
        i++;
      }
      blocks.push({ type: "table", content: tableLines.join("\n") });
      continue;
    }
    const pre: string[] = [];
    while (
      i < lines.length &&
      !lines[i].trim().startsWith("|") &&
      !/^```/.test(lines[i])
    ) {
      pre.push(lines[i]);
      i++;
    }
    const text = pre.join("\n").trim();
    if (text) blocks.push({ type: "pre", content: text });
  }

  // Group consecutive code fences so CSS can tighten the gap between them.
  const grouped: MdGroup[] = [];
  let codeRun: Array<Extract<MdBlock, { type: "code" }>> = [];
  const flushCodes = () => {
    if (!codeRun.length) return;
    if (codeRun.length === 1) grouped.push({ type: "single", block: codeRun[0] });
    else grouped.push({ type: "codes", items: [...codeRun] });
    codeRun = [];
  };
  for (const b of blocks) {
    if (b.type === "code") {
      codeRun.push(b);
      continue;
    }
    flushCodes();
    grouped.push({ type: "single", block: b });
  }
  flushCodes();

  return (
    <>
      {grouped.map((group, gidx) => {
        if (group.type === "codes") {
          return (
            <div key={gidx} className="code-stack" style={{ marginBottom: "0.85rem" }}>
              {group.items.map((b, idx) => (
                <CodeBlock key={idx} code={b.code} language={b.language} />
              ))}
            </div>
          );
        }
        const b = group.block;
        if (b.type === "code") {
          return (
            <div key={gidx} style={{ marginBottom: "0.85rem" }}>
              <CodeBlock code={b.code} language={b.language} />
            </div>
          );
        }
        if (b.type === "table") {
          const rows = b.content
            .split("\n")
            .filter((l: string) => l.trim() && !/^\|[\s-:|]+\|$/.test(l.trim()));
          return (
            <div key={gidx} className="table-wrap" style={{ marginBottom: "1rem" }}>
              <table className="data">
                <tbody>
                  {rows.map((row: string, ri: number) => {
                    const cells = row
                      .split("|")
                      .slice(1, -1)
                      .map((c: string) => c.trim());
                    const Tag = ri === 0 ? "th" : "td";
                    return (
                      <tr key={ri}>
                        {cells.map((cell: string, ci: number) => (
                          <Tag
                            key={ci}
                            className="wrap"
                            dangerouslySetInnerHTML={{ __html: inlineFormat(cell) }}
                          />
                        ))}
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          );
        }
        return (
          <div
            key={gidx}
            className="muted"
            style={{ whiteSpace: "pre-wrap", marginBottom: "1rem" }}
            dangerouslySetInnerHTML={{ __html: inlineFormat(b.content) }}
          />
        );
      })}
    </>
  );
}

export function DocSections({ sections }: { sections: DocSection[] }) {
  if (!sections?.length) return null;

  return (
    <>
      {sections.map((section, i) => (
        <SectionBlock key={`${section.type}-${section.title ?? ""}-${i}`} section={section} />
      ))}
    </>
  );
}

function SectionBlock({ section }: { section: DocSection }) {
  if (section.type === "paragraph") {
    return (
      <div className="panel" style={{ marginBottom: "1rem" }}>
        {section.title ? <h2 style={{ fontSize: "1.25rem" }}>{section.title}</h2> : null}
        <p
          className="muted"
          style={{ marginBottom: 0 }}
          dangerouslySetInnerHTML={{ __html: inlineFormat(section.text) }}
        />
      </div>
    );
  }

  if (section.type === "list") {
    const ListTag = section.ordered ? "ol" : "ul";
    return (
      <div className="panel" style={{ marginBottom: "1rem" }}>
        {section.title ? <h2 style={{ fontSize: "1.25rem" }}>{section.title}</h2> : null}
        <ListTag className="muted" style={{ paddingLeft: "1.2rem", margin: "0.5rem 0 0" }}>
          {section.items.map((item, idx) => (
            <li key={idx} dangerouslySetInnerHTML={{ __html: inlineFormat(item) }} />
          ))}
        </ListTag>
      </div>
    );
  }

  if (section.type === "code") {
    return (
      <div className="code-stack" style={{ marginBottom: "0.85rem" }}>
        {section.title ? <h2 style={{ fontSize: "1.15rem" }}>{section.title}</h2> : null}
        <CodeBlock code={section.code} language={section.language} />
      </div>
    );
  }

  if (section.type === "table") {
    return (
      <div className="panel" style={{ marginBottom: "1rem" }}>
        {section.title ? <h2 style={{ fontSize: "1.25rem" }}>{section.title}</h2> : null}
        {section.lead ? (
          <div style={{ marginBottom: "0.75rem" }}>
            <MarkdownBlock body={section.lead} />
          </div>
        ) : null}
        <div className="table-wrap" style={{ marginBottom: section.trail ? "0.75rem" : 0 }}>
          <table className="data">
            <thead>
              <tr>
                {section.headers.map((h, i) => (
                  <th key={i} className="wrap" dangerouslySetInnerHTML={{ __html: inlineFormat(h) }} />
                ))}
              </tr>
            </thead>
            <tbody>
              {section.rows.map((row, ri) => (
                <tr key={ri}>
                  {section.headers.map((_, ci) => (
                    <td
                      key={ci}
                      className="wrap"
                      dangerouslySetInnerHTML={{ __html: inlineFormat(row[ci] ?? "") }}
                    />
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {section.trail ? <MarkdownBlock body={section.trail} /> : null}
      </div>
    );
  }

  return (
    <div className="panel" style={{ marginBottom: "1rem" }}>
      {section.title ? <h2 style={{ fontSize: "1.25rem" }}>{section.title}</h2> : null}
      <MarkdownBlock body={section.body} />
    </div>
  );
}
