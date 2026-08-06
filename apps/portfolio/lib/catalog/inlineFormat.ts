/** Escape HTML and apply light markdown: links, code, bold. */

/** Join soft line wraps; keep blank-line paragraphs; heal broken ``**`` markers. */
export function normalizeProse(text: string): string {
  if (!text) return "";
  let s = text.replace(/\r\n/g, "\n");
  s = s.replace(/\*\s*\n\s*\*/g, "**").replace(/\*\s+\*/g, "**");
  return s
    .split(/\n\s*\n/)
    .map((para) => para.replace(/[ \t]*\n[ \t]*/g, " ").replace(/[ \t]{2,}/g, " ").trim())
    .filter(Boolean)
    .join("\n\n");
}

/** Description HTML: paragraphs from blank lines (soft wraps become spaces). */
export function formatDescriptionHtml(text: string): string {
  return normalizeProse(text)
    .split("\n\n")
    .map((para) => `<p>${inlineFormat(para)}</p>`)
    .join("");
}

function packageHrefFromMarkdownUrl(url: string): string | null {
  const cleaned = url.trim();
  // ../Lyo.Foo/README.md or ../../../Core/Cache/Lyo.Cache/README.md
  const m = cleaned.match(/(Lyo\.[A-Za-z0-9.]+)(?:\/README\.md)?(?:#.*)?$/);
  if (m) return `/packages/${encodeURIComponent(m[1])}`;
  if (cleaned.startsWith("http://") || cleaned.startsWith("https://") || cleaned.startsWith("/")) {
    return cleaned;
  }
  return null;
}

export function inlineFormat(text: string): string {
  return text
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/\[`([^`]+)`\]\(([^)]+)\)/g, (_m, label: string, url: string) => {
      const href = packageHrefFromMarkdownUrl(url);
      if (!href) return `<code>${label}</code>`;
      return `<a href="${href}"><code>${label}</code></a>`;
    })
    .replace(/\[([^\]]+)\]\(([^)]+)\)/g, (_m, label: string, url: string) => {
      const href = packageHrefFromMarkdownUrl(url);
      if (!href) return label;
      return `<a href="${href}">${label}</a>`;
    })
    .replace(/\*\*`([^`]+)`\*\*/g, "<strong><code>$1</code></strong>")
    .replace(/`([^`]+)`/g, "<code>$1</code>")
    .replace(/\*\*([^*]+)\*\*/g, "<strong>$1</strong>")
    // Pull punctuation flush against inline code (avoids `EntityRef` ,)
    .replace(/<\/code>\s+([,.;:!?)\]])/g, "</code>$1")
    .replace(/([({\[])\s+<code>/g, "$1<code>");
}
