namespace Lyo.Diagnostic.Classification;

public interface IExceptionClassifier
{
    /// <summary>Classifies a live exception.</summary>
    ClassifiedExceptionResult Classify(Exception exception);

    /// <summary>Classifies by exception type name (e.g. from a decoded trace message).</summary>
    ClassifiedExceptionResult ClassifyByTypeName(string exceptionTypeName);
}
