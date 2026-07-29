type CodeBlockProps = {
  code: string;
  language?: string;
};

export function CodeBlock({ code, language = "csharp" }: CodeBlockProps) {
  return (
    <div className="code-block">
      <pre>
        <code data-language={language}>{code.trim()}</code>
      </pre>
    </div>
  );
}
