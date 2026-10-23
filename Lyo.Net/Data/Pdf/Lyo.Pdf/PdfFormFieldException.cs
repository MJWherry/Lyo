namespace Lyo.Pdf;

/// <summary>An AcroForm field name was not in the document. The file bytes are left unchanged.</summary>
public sealed class PdfFormFieldException(string message)
    : Exception(message);