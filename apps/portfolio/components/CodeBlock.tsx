type CodeBlockProps = {
  code: string;
  language?: string;
};

export function CodeBlock({ code, language = "csharp" }: CodeBlockProps) {
  return (
    <pre>
      <code data-language={language}>{code.trim()}</code>
    </pre>
  );
}
