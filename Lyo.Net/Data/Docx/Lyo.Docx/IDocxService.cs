namespace Lyo.Docx;

/// <summary>Reads paragraph and table text from a .docx and writes <c>{Token}</c> replacements back into the package.</summary>
public interface IDocxService
{
    /// <summary>Paragraph text, including paragraphs inside tables, one line each.</summary>
    string ReadText(byte[] docx);

    /// <summary>
    /// Replaces <c>{Token}</c> placeholders with <see cref="Lyo.Formatter.IFormatterService" />. A token split across runs is joined first.
    /// An unknown token leaves that paragraph unchanged. Returns the written package bytes.
    /// </summary>
    byte[] Fill(byte[] docx, object? model);
}
