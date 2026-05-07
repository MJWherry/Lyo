namespace Lyo.Pdf.Models;

/// <summary>Per-document extraction API: composes the <see cref="IPdfDocumentText" /> and <see cref="IPdfDocumentSections" /> model contracts.</summary>
public interface ITextExtractor : IPdfDocumentText, IPdfDocumentSections { }