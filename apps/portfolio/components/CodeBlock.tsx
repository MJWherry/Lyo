import { highlightCode } from "@/lib/highlightCode";

type CodeBlockProps = {
  code: string;
  language?: string;
};

/** Server-rendered syntax-highlighted fence (Shiki, dual light/dark themes). */
export async function CodeBlock({ code, language = "csharp" }: CodeBlockProps) {
  const html = await highlightCode(code.trim(), language);
  return (
    <div
      className="code-block code-block--shiki"
      data-language={language}
      dangerouslySetInnerHTML={{ __html: html }}
    />
  );
}
