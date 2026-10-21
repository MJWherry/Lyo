using Lyo.Diagnostic.Classification;

namespace Lyo.Diagnostic.Web.Components;

public partial class ExceptionTypeClassifierPanel
{
    private readonly IExceptionClassifier _classifier = new ExceptionClassifier();

    private ClassifiedExceptionResult? _result;

    private string _typeName = string.Empty;

    private void Classify()
    {
        var name = _typeName.Trim();
        _result = string.IsNullOrEmpty(name) ? null : _classifier.ClassifyByTypeName(name);
    }
}