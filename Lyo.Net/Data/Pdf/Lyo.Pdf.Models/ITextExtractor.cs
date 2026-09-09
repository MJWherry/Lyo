namespace Lyo.Pdf.Models;

/// <summary>Per-document extraction surface combining <see cref="IPdfDocumentText" /> and <see cref="IPdfDocumentSections" />.</summary>
public interface ITextExtractor : IPdfDocumentText, IPdfDocumentSections { }