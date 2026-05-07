namespace Lyo.Pdf.Models;

/// <summary>Multi-page section boundaries via <see cref="IPdfReader.Text" /> (<see cref="ITextExtractor" />).</summary>
public interface IPdfDocumentSections
{
    IReadOnlyList<PdfWord> GetWordsBetweenSections(
        string startSection,
        IEnumerable<string> sectionsInOrder,
        string? defaultEndSection = null,
        int? startPage = null,
        int? endPage = null);

    IReadOnlyList<PdfTextLine> GetLinesBetweenSections(
        string startSection,
        IEnumerable<string> sectionsInOrder,
        string? defaultEndSection = null,
        int? startPage = null,
        int? endPage = null,
        double? yTolerance = null);

    Task<IReadOnlyList<PdfTextLine>> GetLinesBetweenSectionsAsync(
        string startSection,
        IEnumerable<string> sectionsInOrder,
        string? defaultEndSection = null,
        int? startPage = null,
        int? endPage = null,
        double? yTolerance = null,
        CancellationToken ct = default);

    PdfSection? GetSection(
        string startSection,
        IEnumerable<string> sectionsInOrder,
        string? defaultEndSection = null,
        int? startPage = null,
        int? endPage = null,
        double? yTolerance = null);

    Task<PdfSection?> GetSectionAsync(
        string startSection,
        IEnumerable<string> sectionsInOrder,
        string? defaultEndSection = null,
        int? startPage = null,
        int? endPage = null,
        double? yTolerance = null,
        CancellationToken ct = default);
}