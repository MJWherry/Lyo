using System.Text;
using Lyo.Exceptions;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Annotations;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.IO;
using UglyToad.PdfPig.AcroForms;
using PigDocument = UglyToad.PdfPig.PdfDocument;
using SharpReader = PdfSharp.Pdf.IO.PdfReader;

namespace Lyo.Pdf;

/// <summary>Fills AcroForm fields, redacts text, and writes highlight and stamp annotations.</summary>
public static class PdfMarkup
{
    /// <summary>Value of the named text field, or null when the document has no such field.</summary>
    public static string? ReadField(byte[] pdf, string name)
    {
        ArgumentHelpers.ThrowIfNull(pdf);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        using var document = PigDocument.Open(pdf);
        if (!document.TryGetForm(out var form))
            return null;

        foreach (var field in form.GetFields()) {
            if (!string.Equals(field.Information.PartialName, name, StringComparison.Ordinal))
                continue;
            return field.GetFieldValue().Value;
        }

        return null;
    }

    /// <summary>Sets an existing text field. An unknown name throws <see cref="PdfFormFieldException" /> and does not change <paramref name="pdf" />.</summary>
    public static byte[] SetField(byte[] pdf, string name, string value)
    {
        ArgumentHelpers.ThrowIfNull(pdf);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        ArgumentHelpers.ThrowIfNull(value);
        var marker = Encoding.ASCII.GetBytes($"/T ({name})");
        var at = IndexOf(pdf, marker, 0, pdf.Length);
        if (at < 0)
            throw new PdfFormFieldException($"Form field '{name}' was not found.");

        var encoded = Encoding.ASCII.GetBytes(EscapePdfString(value));
        var dictEnd = IndexOf(pdf, ">>"u8, at, pdf.Length);
        var valueKey = IndexOf(pdf, "/V ("u8, at, dictEnd < 0 ? pdf.Length : dictEnd);
        if (valueKey >= 0) {
            var open = valueKey + 4;
            var close = EndOfPdfString(pdf, open - 1);
            return Splice(pdf, open, close, encoded);
        }

        return Splice(pdf, at + marker.Length, at + marker.Length, Encoding.ASCII.GetBytes($" /V ({EscapePdfString(value)})"));
    }

    /// <summary>Removes <paramref name="text" /> from page content, then paints a black rectangle over its box.</summary>
    public static byte[] Redact(byte[] pdf, string text)
    {
        ArgumentHelpers.ThrowIfNull(pdf);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(text);
        var box = FindBox(pdf, text);
        using var input = new MemoryStream(pdf.ToArray(), writable: false);
        var document = SharpReader.Open(input, PdfDocumentOpenMode.Modify);
        var page = document.Pages[box.PageIndex];
        var content = page.Contents.CreateSingleContent();
        var raw = content.Stream?.UnfilteredValue ?? [];
        if (!BlankOut(raw, text))
            throw new InvalidOperationException($"Could not remove '{text}' from the page content stream.");

        page.Contents.ReplaceContent(ContentReader.ReadContent(raw));
        using (var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append)) {
            var top = page.Height.Point - box.Top;
            var height = Math.Max(box.Top - box.Bottom, 1);
            graphics.DrawRectangle(XBrushes.Black, box.Left, top, Math.Max(box.Right - box.Left, 1), height);
        }

        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }

    /// <summary>Writes a highlight annotation and a text stamp onto the first page. Both are stored in the file.</summary>
    public static byte[] Annotate(byte[] pdf, double x, double y, double width, double height, string stamp)
    {
        ArgumentHelpers.ThrowIfNull(pdf);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(stamp);
        using var input = new MemoryStream([.. pdf], writable: false);
        var document = SharpReader.Open(input, PdfDocumentOpenMode.Modify);
        var page = document.Pages[0];
        var highlight = new PdfTextAnnotation(document) {
            Rectangle = new(new(x, y, width, height))
        };
        highlight.Elements.SetName("/Subtype", "/Highlight");
        highlight.Elements.Add("/QuadPoints", Quad(document, x, y, width, height));
        page.Annotations.Add(highlight);

        var note = new PdfTextAnnotation(document) {
            Contents = stamp,
            Icon = PdfTextAnnotationIcon.Note,
            Rectangle = new(new(x, y - 24, 18, 18))
        };
        page.Annotations.Add(note);

        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }

    private static byte[] Splice(byte[] source, int replaceStart, int replaceEnd, byte[] insert)
    {
        var result = new byte[source.Length - (replaceEnd - replaceStart) + insert.Length];
        Buffer.BlockCopy(source, 0, result, 0, replaceStart);
        Buffer.BlockCopy(insert, 0, result, replaceStart, insert.Length);
        Buffer.BlockCopy(source, replaceEnd, result, replaceStart + insert.Length, source.Length - replaceEnd);
        return result;
    }

    private static int EndOfPdfString(byte[] pdf, int openParen)
    {
        var depth = 0;
        for (var i = openParen; i < pdf.Length; i++) {
            if (pdf[i] == '\\') {
                i++;
                continue;
            }

            if (pdf[i] == '(')
                depth++;
            else if (pdf[i] == ')') {
                depth--;
                if (depth == 0)
                    return i;
            }
        }

        return pdf.Length;
    }

    private static string EscapePdfString(string value)
        => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    private static int IndexOf(byte[] source, ReadOnlySpan<byte> needle, int start, int end)
    {
        var last = Math.Min(end, source.Length) - needle.Length;
        for (var i = start; i <= last; i++) {
            if (source.AsSpan(i, needle.Length).SequenceEqual(needle))
                return i;
        }

        return -1;
    }

    private static TextBox FindBox(byte[] pdf, string text)
    {
        using var document = PigDocument.Open(pdf);
        for (var pageNumber = 1; pageNumber <= document.NumberOfPages; pageNumber++) {
            var page = document.GetPage(pageNumber);
            foreach (var word in page.GetWords()) {
                if (!word.Text.Contains(text, StringComparison.Ordinal))
                    continue;
                var box = word.BoundingBox;
                return new(pageNumber - 1, box.Left, box.Bottom, box.Right, box.Top);
            }
        }

        throw new InvalidOperationException($"Text '{text}' was not found.");
    }

    private static bool BlankOut(byte[] content, string text)
    {
        var needle = Encoding.ASCII.GetBytes(text);
        var found = false;
        for (var i = 0; i <= content.Length - needle.Length; i++) {
            var match = true;
            for (var j = 0; j < needle.Length; j++) {
                if (content[i + j] == needle[j])
                    continue;
                match = false;
                break;
            }

            if (!match)
                continue;

            found = true;
            for (var j = 0; j < needle.Length; j++)
                content[i + j] = (byte)' ';
        }

        return found;
    }

    private static PdfArray Quad(PdfDocument document, double x, double y, double width, double height)
    {
        var right = x + width;
        var top = y + height;
        return new(document, new PdfReal(x), new PdfReal(top), new PdfReal(right), new PdfReal(top), new PdfReal(x), new PdfReal(y), new PdfReal(right), new PdfReal(y));
    }

    private readonly record struct TextBox(int PageIndex, double Left, double Bottom, double Right, double Top);
}
