import "server-only";

import { createHighlighter, type Highlighter, type BundledLanguage } from "shiki";

const LANGS = [
  "csharp",
  "json",
  "bash",
  "shellscript",
  "xml",
  "sql",
  "typescript",
  "javascript",
  "yaml",
  "markdown",
] as const satisfies readonly BundledLanguage[];

type SupportedLang = (typeof LANGS)[number];

const ALIASES: Record<string, SupportedLang> = {
  cs: "csharp",
  "c#": "csharp",
  csharp: "csharp",
  json: "json",
  bash: "bash",
  sh: "shellscript",
  shell: "shellscript",
  zsh: "shellscript",
  xml: "xml",
  html: "xml",
  sql: "sql",
  ts: "typescript",
  typescript: "typescript",
  js: "javascript",
  javascript: "javascript",
  yml: "yaml",
  yaml: "yaml",
  md: "markdown",
  markdown: "markdown",
};

let highlighterPromise: Promise<Highlighter> | null = null;

function getHighlighter(): Promise<Highlighter> {
  highlighterPromise ??= createHighlighter({
    themes: ["github-light", "github-dark"],
    langs: [...LANGS],
  });
  return highlighterPromise;
}

export function normalizeHighlightLang(language?: string | null): SupportedLang {
  const key = (language ?? "csharp").trim().toLowerCase();
  if (!key) return "csharp";
  if (ALIASES[key]) return ALIASES[key];
  if ((LANGS as readonly string[]).includes(key)) return key as SupportedLang;
  // Portfolio docs are overwhelmingly C#; unknown fences still get a useful default.
  return "csharp";
}

/** Highlight source to dual-theme HTML (light/dark via CSS variables). */
export async function highlightCode(code: string, language?: string | null): Promise<string> {
  const lang = normalizeHighlightLang(language);
  const highlighter = await getHighlighter();

  return highlighter.codeToHtml(code.replace(/\n$/, ""), {
    lang,
    themes: {
      light: "github-light",
      dark: "github-dark",
    },
    defaultColor: false,
  });
}
