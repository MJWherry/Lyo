using System.Text;
using UglyToad.PdfPig.Annotations;
using PigDocument = UglyToad.PdfPig.PdfDocument;

namespace Lyo.Pdf.Tests;

public class PdfMarkupTests
{
    [Fact]
    public void Form_SetKnownField_ReadReturnsValue()
    {
        var pdf = Form("City");
        var filled = PdfMarkup.SetField(pdf, "City", "Ada");
        Assert.Equal("Ada", PdfMarkup.ReadField(filled, "City"));
    }

    [Fact]
    public void Form_UnknownField_LeavesBytesUnchanged()
    {
        var pdf = Form("City");
        var before = pdf.ToArray();
        var error = Record.Exception(() => PdfMarkup.SetField(pdf, "Missing", "Ada"));
        Assert.NotNull(error);
        Assert.Equal(before, pdf);
    }

    [Fact]
    public void Redact_ExtractNoLongerContainsRemovedText()
    {
        var pdf = PageWithText();
        var redacted = PdfMarkup.Redact(pdf, "SECRET");
        using var document = PigDocument.Open(redacted);
        var text = document.GetPage(1).Text;
        Assert.DoesNotContain("SECRET", text);
        Assert.Contains("KEEP", text);
    }

    [Fact]
    public void Annotate_HighlightAndStamp_RoundTripOnReopen()
    {
        var pdf = PageWithText();
        var marked = PdfMarkup.Annotate(pdf, 72, 680, 120, 18, "Approved");
        using var document = PigDocument.Open(marked);
        var annotations = document.GetPage(1).GetAnnotations().ToList();
        Assert.Contains(annotations, annotation => annotation.Type == AnnotationType.Highlight);
        Assert.Contains(annotations, annotation => annotation.Content == "Approved");
    }

    private static byte[] Form(string fieldName)
        => Document([
            "<< /Type /Catalog /Pages 2 0 R /AcroForm << /Fields [5 0 R] >> >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Annots [5 0 R] >>",
            "<< /Length 0 >>\nstream\nendstream",
            $"<< /Type /Annot /Subtype /Widget /FT /Tx /T ({fieldName}) /Rect [72 680 250 700] /P 3 0 R >>"
        ]);

    private static byte[] PageWithText()
    {
        const string stream = "BT /F1 18 Tf 72 700 Td (SECRET) Tj 0 -40 Td (KEEP) Tj ET";
        return Document([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            $"<< /Length {stream.Length} >>\nstream\n{stream}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        ]);
    }

    private static byte[] Document(IReadOnlyList<string> bodies)
    {
        var builder = new StringBuilder();
        builder.Append("%PDF-1.4\n");
        var offsets = new int[bodies.Count + 1];
        for (var i = 0; i < bodies.Count; i++) {
            offsets[i + 1] = Encoding.ASCII.GetByteCount(builder.ToString());
            builder.Append(i + 1).Append(" 0 obj\n").Append(bodies[i]).Append("\nendobj\n");
        }

        var startxref = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append("xref\n0 ").Append(bodies.Count + 1).Append('\n');
        builder.Append("0000000000 65535 f \n");
        for (var i = 1; i <= bodies.Count; i++)
            builder.Append(offsets[i].ToString("D10")).Append(" 00000 n \n");
        builder.Append("trailer\n<< /Size ").Append(bodies.Count + 1).Append(" /Root 1 0 R >>\nstartxref\n");
        builder.Append(startxref).Append("\n%%EOF\n");
        return Encoding.ASCII.GetBytes(builder.ToString());
    }
}
