using Lyo.Pdf.Web.Components.PdfAnnotator;
using Microsoft.AspNetCore.Components;

namespace Lyo.Pdf.Web.Components;

public partial class PdfAnnotationWorkbench
{
    private List<LyoPdfAnnotationResult> _liveAnnotations = [];
    private List<LyoPdfAnnotationResult> _savedAnnotations = [];

    private Task OnAnnotationsChanged(IReadOnlyList<LyoPdfAnnotationResult> annotations)
    {
        _liveAnnotations = annotations.ToList();
        return Task.CompletedTask;
    }

    private Task OnAnnotationsSaved(IReadOnlyList<LyoPdfAnnotationResult> annotations)
    {
        _savedAnnotations = annotations.ToList();
        return Task.CompletedTask;
    }

    private static string FormatResult(LyoPdfAnnotationResult annotation)
    {
        if (!string.IsNullOrWhiteSpace(annotation.ErrorMessage))
            return annotation.ErrorMessage;

        if (annotation.KeyValuePairs is { Count: > 0 })
            return string.Join(Environment.NewLine, annotation.KeyValuePairs.Select(x => $"{x.Key}: {x.Value ?? "—"}"));

        if (annotation.TableRows is { Count: > 0 })
            return $"{annotation.TableRows.Count} row(s) extracted.";

        return string.IsNullOrWhiteSpace(annotation.ExtractedText) ? "—" : annotation.ExtractedText;
    }
}
