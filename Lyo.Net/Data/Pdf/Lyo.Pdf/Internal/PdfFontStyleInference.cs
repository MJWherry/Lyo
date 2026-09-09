using Lyo.Common.Core.Extensions;
using Lyo.Pdf.Models;

namespace Lyo.Pdf.Internal;

/// <summary>
/// Infers bold/italic from PDF font names. PdfPig font-details flags often track stem weight rather than the PostScript name (e.g. SomeFont-Regular vs SomeFont-Bold), so
/// name tokens win when present.
/// </summary>
internal static class PdfFontStyleInference
{
    /// <summary>True when the word looks emphasized (bold or italic) under font-name-first rules.</summary>
    public static bool IsEmphasized(PdfWord w)
    {
        var f = w.Format;
        if (f == null)
            return false;

        return InferBold(f.FontName, f.FontBold) || InferItalic(f.FontName, f.FontItalic);
    }

    /// <summary>True when the word matches the requested emphasis (bold/italic vs vector underline).</summary>
    public static bool IsInferEmphasizedForFlags(PdfWord w, PdfInferFormattingFlags flags)
    {
        if (flags == PdfInferFormattingFlags.None)
            return false;

        var boldHit = (flags & PdfInferFormattingFlags.Bold) != 0 && IsEmphasized(w);
        var ulHit = (flags & PdfInferFormattingFlags.Underline) != 0 && w.Format?.FontUnderline == true;
        return boldHit || ulHit;
    }

    public static bool InferBold(string? fontName, bool pdfPigBold)
    {
        if (fontName.IsNullOrEmpty())
            return pdfPigBold;

        var n = fontName;
        if (NameImpliesRegularStyle(n))
            return false;

        if (NameImpliesBoldStyle(n))
            return true;

        return pdfPigBold;
    }

    public static bool InferItalic(string? fontName, bool pdfPigItalic)
    {
        if (fontName.IsNullOrEmpty())
            return pdfPigItalic;

        var n = fontName;
        if (NameImpliesUprightStyle(n))
            return false;

        if (NameImpliesItalicStyle(n))
            return true;

        return pdfPigItalic;
    }

    private static bool NameImpliesRegularStyle(string fontName)
    {
        // Typical PDF / TrueType tokens that mean "not bold"
        if (fontName.Contains("-Regular", StringComparison.OrdinalIgnoreCase))
            return true;

        if (fontName.Contains("Regular", StringComparison.OrdinalIgnoreCase) && !fontName.Contains("Bold", StringComparison.OrdinalIgnoreCase))
            return true;

        if (fontName.Contains("-Roman", StringComparison.OrdinalIgnoreCase) || fontName.Contains("-Book", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static bool NameImpliesBoldStyle(string fontName)
    {
        if (fontName.Contains("-Bold", StringComparison.OrdinalIgnoreCase))
            return true;

        if (fontName.Contains("Bold", StringComparison.OrdinalIgnoreCase))
            return true;

        if (fontName.Contains("-Black", StringComparison.OrdinalIgnoreCase) || fontName.Contains("Heavy", StringComparison.OrdinalIgnoreCase))
            return true;

        // SemiBold / DemiBold commonly mark subheads
        if (fontName.Contains("SemiBold", StringComparison.OrdinalIgnoreCase) || fontName.Contains("Demi", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static bool NameImpliesUprightStyle(string fontName)
    {
        if (fontName.Contains("-Regular", StringComparison.OrdinalIgnoreCase) && !NameImpliesItalicStyle(fontName))
            return true;

        return false;
    }

    private static bool NameImpliesItalicStyle(string fontName)
    {
        if (fontName.Contains("-Italic", StringComparison.OrdinalIgnoreCase) || fontName.Contains("Italic", StringComparison.OrdinalIgnoreCase))
            return true;

        if (fontName.Contains("-Oblique", StringComparison.OrdinalIgnoreCase) || fontName.Contains("Oblique", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}