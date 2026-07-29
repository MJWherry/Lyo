"use client";

import { useEffect, useState } from "react";
import hljs from "highlight.js/lib/core";
import csharp from "highlight.js/lib/languages/csharp";
import json from "highlight.js/lib/languages/json";
import bash from "highlight.js/lib/languages/bash";
import xml from "highlight.js/lib/languages/xml";
import typescript from "highlight.js/lib/languages/typescript";
import javascript from "highlight.js/lib/languages/javascript";
import sql from "highlight.js/lib/languages/sql";

let registered = false;

function ensureLanguages() {
  if (registered) return;
  hljs.registerLanguage("csharp", csharp);
  hljs.registerLanguage("cs", csharp);
  hljs.registerLanguage("json", json);
  hljs.registerLanguage("bash", bash);
  hljs.registerLanguage("shell", bash);
  hljs.registerLanguage("sh", bash);
  hljs.registerLanguage("xml", xml);
  hljs.registerLanguage("html", xml);
  hljs.registerLanguage("typescript", typescript);
  hljs.registerLanguage("ts", typescript);
  hljs.registerLanguage("javascript", javascript);
  hljs.registerLanguage("js", javascript);
  hljs.registerLanguage("sql", sql);
  registered = true;
}

function normalizeLang(language?: string): string {
  const key = (language ?? "csharp").trim().toLowerCase();
  if (key === "c#") return "csharp";
  return key || "csharp";
}

type ClientCodeBlockProps = {
  code: string;
  language?: string;
};

/** Client-side highlighter for interactive demos (highlight.js). */
export function ClientCodeBlock({ code, language = "csharp" }: ClientCodeBlockProps) {
  const lang = normalizeLang(language);
  const trimmed = code.trim();
  const [html, setHtml] = useState<string | null>(null);

  useEffect(() => {
    ensureLanguages();
    try {
      const resolved = hljs.getLanguage(lang) ? lang : "csharp";
      setHtml(hljs.highlight(trimmed, { language: resolved }).value);
    } catch {
      setHtml(null);
    }
  }, [trimmed, lang]);

  return (
    <div className="code-block code-block--hljs" data-language={lang}>
      <pre>
        {html ? (
          <code
            className={`hljs language-${lang}`}
            dangerouslySetInnerHTML={{ __html: html }}
          />
        ) : (
          <code>{trimmed}</code>
        )}
      </pre>
    </div>
  );
}
