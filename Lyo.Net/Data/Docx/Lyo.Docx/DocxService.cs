using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Lyo.Exceptions;
using Lyo.Formatter;

namespace Lyo.Docx;

/// <summary>Open XML wordprocessing reader and <c>{Token}</c> filler.</summary>
public sealed class DocxService : IDocxService
{
    private readonly IFormatterService _formatter;

    /// <summary>Uses <paramref name="formatter" /> for token replacement.</summary>
    public DocxService(IFormatterService formatter)
    {
        ArgumentHelpers.ThrowIfNull(formatter);
        _formatter = formatter;
    }

    /// <inheritdoc />
    public string ReadText(byte[] docx)
    {
        ArgumentHelpers.ThrowIfNull(docx);
        using var stream = new MemoryStream(docx, false);
        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart?.Document?.Body;
        if (body is null)
            return string.Empty;

        var lines = new StringBuilder();
        foreach (var paragraph in body.Descendants<Paragraph>()) {
            if (lines.Length > 0)
                lines.Append('\n');

            lines.Append(paragraph.InnerText);
        }

        return lines.ToString();
    }

    /// <inheritdoc />
    public byte[] Fill(byte[] docx, object? model)
    {
        ArgumentHelpers.ThrowIfNull(docx);
        var stream = new MemoryStream();
        stream.Write(docx, 0, docx.Length);
        stream.Position = 0;
        using (var document = WordprocessingDocument.Open(stream, true)) {
            var body = document.MainDocumentPart?.Document?.Body;
            if (body is not null) {
                foreach (var paragraph in body.Descendants<Paragraph>().ToList())
                    FillParagraph(paragraph, model);
            }

            document.MainDocumentPart?.Document?.Save();
        }

        return stream.ToArray();
    }

    private void FillParagraph(Paragraph paragraph, object? model)
    {
        var runs = paragraph.Elements<Run>().ToList();
        if (runs.Count == 0)
            return;

        var joined = string.Concat(runs.Select(run => run.InnerText));
        if (joined.IndexOf('{') < 0)
            return;

        if (!_formatter.TryFormat(joined, model, out var formatted) || formatted is null)
            return;

        if (formatted == joined || _formatter.GetUnresolvedPlaceholders(joined, formatted).Count > 0)
            return;

        SetRunText(runs[0], formatted);
        for (var i = 1; i < runs.Count; i++)
            SetRunText(runs[i], string.Empty);
    }

    private static void SetRunText(Run run, string value)
    {
        var texts = run.Descendants<Text>().ToList();
        if (texts.Count == 0) {
            run.AppendChild(new Text(value) { Space = SpaceProcessingModeValues.Preserve });
            return;
        }

        texts[0].Text = value;
        texts[0].Space = SpaceProcessingModeValues.Preserve;
        for (var i = 1; i < texts.Count; i++)
            texts[i].Remove();
    }
}